using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

using Amazon.Auth.AccessControlPolicy;
using Amazon.Auth.AccessControlPolicy.ActionIdentifiers;
using Amazon.S3;
using Amazon.S3.Model;

using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;

using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.S3.View;

using Statement = Amazon.Auth.AccessControlPolicy.Statement;

namespace Amazon.AWSToolkit.S3.Controller
{
    public class AddEventConfigurationController
    {

        bool _success;
        IAmazonS3 _s3Client;
        string _bucketName;
        AddEventConfigurationControl _control;

        IAmazonIdentityManagementService _iamClient;
        IAmazonSimpleNotificationService _snsClient;
        IAmazonSQS _sqsClient;
        IAmazonLambda _lambdaClient;
        ToolkitRegion _region;

        public bool Execute(IAmazonS3 s3Client, string bucketName, ToolkitRegion region, AccountViewModel account)
        {
            this._s3Client = s3Client;
            this._bucketName = bucketName;
            this._region = region;

            this._iamClient = account.CreateServiceClient<AmazonIdentityManagementServiceClient>(_region);
            this._sqsClient = account.CreateServiceClient<AmazonSQSClient>(_region);
            this._snsClient = account.CreateServiceClient<AmazonSimpleNotificationServiceClient>(_region);
            this._lambdaClient = account.CreateServiceClient<AmazonLambdaClient>(_region);

            this._control = new AddEventConfigurationControl(this);
            ToolkitFactory.Instance.ShellProvider.ShowModal(this._control);
            return _success;
        }

        public void Persist()
        {
            var getResponse = this._s3Client.GetBucketNotification(this._bucketName);
            var putRequest = new PutBucketNotificationRequest
            {
                BucketName = this._bucketName,
                LambdaFunctionConfigurations = getResponse.LambdaFunctionConfigurations,
                QueueConfigurations = getResponse.QueueConfigurations,
                TopicConfigurations = getResponse.TopicConfigurations
            };

            S3KeyFilter filter = new S3KeyFilter();
            if(!string.IsNullOrEmpty(this._control.Prefix))
            {
                filter.FilterRules.Add(new FilterRule
                {
                    Name = S3Constants.NOTIFICATION_FILTER_KEY_PREFIX,
                    Value = this._control.Prefix
                });
            }
            if (!string.IsNullOrEmpty(this._control.Suffix))
            {
                filter.FilterRules.Add(new FilterRule
                {
                    Name = S3Constants.NOTIFICATION_FILTER_KEY_SUFFIX,
                    Value = this._control.Suffix
                });
            }

            switch (this._control.Service)
            {
                case AddEventConfigurationControl.ServiceType.SNS:
                    if (this._control.AddPermissions)
                    {
                        CreateS3PublishPermissionForSns(this._snsClient, this._control.ResourceArn, this._bucketName);
                    }
                    var topicConfig = new TopicConfiguration{Topic = this._control.ResourceArn};
                    topicConfig.Events.Add(this._control.Event);
                    if (filter.FilterRules.Count != 0)
                        topicConfig.Filter = new Filter { S3KeyFilter = filter };
                    putRequest.TopicConfigurations.Add(topicConfig);
                    break;
                case AddEventConfigurationControl.ServiceType.SQS:
                    var queueUrl = this._queueArnsToQueueURls[this._control.ResourceArn];
                    if (this._control.AddPermissions)
                    {
                        CreateS3PublishPermissionForSqs(this._sqsClient, queueUrl, this._bucketName);
                    }
                    var queueConfig = new QueueConfiguration{Queue = this._control.ResourceArn};
                    queueConfig.Events.Add(this._control.Event);
                    if (filter.FilterRules.Count != 0)
                        queueConfig.Filter = new Filter { S3KeyFilter = filter };
                    putRequest.QueueConfigurations.Add(queueConfig);
                    break;
                case AddEventConfigurationControl.ServiceType.Lambda:
                    AddLambdaConfiguration(putRequest, filter);
                    break;
            }

            this._s3Client.PutBucketNotification(putRequest);
            this._success = true;
        }

        private static void CreateS3PublishPermissionForSqs(IAmazonSQS sqsClient, string queueUrl, string bucket)
        {
            var queueAttributes = sqsClient.GetQueueAttributes(new GetQueueAttributesRequest()
            {
                QueueUrl = queueUrl,
                AttributeNames = new List<string>() { "All" }
            });
           var policy = string.IsNullOrEmpty(queueAttributes.Policy) ? new Policy() : Policy.FromJson(queueAttributes.Policy);
           var policyJson = CreatePolicyStatement(policy, "sqs:SendMessage", queueAttributes.QueueARN ,bucket);
           if (!string.IsNullOrWhiteSpace(policyJson))
           {
               sqsClient.SetQueueAttributes(new SetQueueAttributesRequest()
               {
                   QueueUrl = queueUrl,
                   Attributes = new Dictionary<string, string>()
                   {
                       {
                           "Policy",
                           policyJson
                       }
                   }
               });
            }
        }

        private static void CreateS3PublishPermissionForSns(IAmazonSimpleNotificationService snsClient, string topicArn, string bucket)
        {
            var attributes = snsClient.GetTopicAttributes(new GetTopicAttributesRequest
            {
                TopicArn = topicArn
            }).Attributes;

            Policy policy;
            if (attributes.ContainsKey("Policy") && !string.IsNullOrEmpty(attributes["Policy"]))
            {
                policy = Policy.FromJson(attributes["Policy"]);
            }
            else
            {
                policy = new Policy();
            }
            var policyJson = CreatePolicyStatement(policy, "sns:Publish", topicArn, bucket);
            if (!string.IsNullOrWhiteSpace(policyJson))
            {
                snsClient.SetTopicAttributes(new SetTopicAttributesRequest
                {
                    TopicArn = topicArn,
                    AttributeName = "Policy",
                    AttributeValue = policyJson
                });
            }
        }

        private static string CreatePolicyStatement(Policy policy, string actionName, string resourceArn,
            string bucket)
        {
            if (string.IsNullOrWhiteSpace(resourceArn))
            {
                return null;
            }
            var tokens = resourceArn.Split(':');
            if (tokens.Length < 6)
            {
                return null;
            }

            var accountNumber = tokens[4];
            string arnPattern = string.Format((IFormatProvider) CultureInfo.InvariantCulture, "arn:aws:s3:*:*:{0}",
                (object) bucket);
            var statement = new Statement(Statement.StatementEffect.Allow);
            statement.Actions.Add(new ActionIdentifier(actionName));
            statement.Resources.Add(new Resource(resourceArn));
            statement.Conditions.Add(ConditionFactory.NewSourceArnCondition(arnPattern));
            statement.Principals.Add(new Principal("*"));
            statement.Conditions.Add(ConditionFactory.NewCondition(ConditionFactory.StringComparisonType.StringEquals,
                "aws:SourceAccount", accountNumber));


            if (!policy.CheckIfStatementExists(statement))
            {
                policy.Statements.Add(statement);
                string json = policy.ToJson();
                return json;
            }

            return null;
        }


        private void AddLambdaConfiguration(PutBucketNotificationRequest putRequest, S3KeyFilter filter)
        {
            var tokens = this._control.ResourceArn.Split(':');
            var accountNumber = tokens[4];
            if (this._control.AddPermissions)
            {
                this._lambdaClient.AddPermission(new Amazon.Lambda.Model.AddPermissionRequest
                {
                    FunctionName = this._control.ResourceArn,
                    Action = "lambda:InvokeFunction",
                    Principal = "s3.amazonaws.com",
                    SourceArn = string.Format("arn:aws:s3:::{0}", this._bucketName),
                    SourceAccount = accountNumber,
                    StatementId = Guid.NewGuid().ToString() + "-vstoolkit"
                });
            }

            var lambdaConfig = new LambdaFunctionConfiguration
            {
                FunctionArn = this._control.ResourceArn
            };
            lambdaConfig.Events.Add(this._control.Event);
            if (filter.FilterRules.Count != 0)
                lambdaConfig.Filter = new Filter { S3KeyFilter = filter };
            putRequest.LambdaFunctionConfigurations.Add(lambdaConfig);

            this._s3Client.PutBucketNotification(putRequest);
        }

        public IList<string> GetTopicArns()
        {
            var topics = new List<string>();
            if (this._snsClient == null)
                return topics;

            var request = new ListTopicsRequest();
            ListTopicsResponse response = null;

            do {
                if (response != null)
                    request.NextToken = response.NextToken;

                response = this._snsClient.ListTopics(request);

                foreach (var topic in response.Topics)
                    topics.Add(topic.TopicArn);
            }while(!string.IsNullOrEmpty(response.NextToken));

            topics = new List<string>(topics.OrderBy(x => x));
            return topics;
        }

        Dictionary<string, string> _queueArnsToQueueURls = new Dictionary<string, string>();
        public IList<string> GetQueueArns()
        {
            this._queueArnsToQueueURls.Clear();
            var queues = new List<string>();
            if (this._sqsClient == null)
                return queues;

            ListQueuesResponse response = this._sqsClient.ListQueues(new ListQueuesRequest());


            foreach (var queue in response.QueueUrls)
            {
                var tokens = queue.Split('/');
                var arn = string.Format("arn:aws:sqs:{0}:{1}:{2}", this._region.Id, tokens[tokens.Length - 2], tokens[tokens.Length - 1]);
                queues.Add(arn);

                this._queueArnsToQueueURls[arn] = queue;
            }

            queues = new List<string>(queues.OrderBy(x => x));
            return queues;
        }

        public IList<string> GetLambdaArns()
        {
            var lambdas = new List<string>();
            if (this._lambdaClient == null)
                return lambdas;

            ListFunctionsRequest request = new ListFunctionsRequest();
            ListFunctionsResponse response = null;

            do
            {
                if (response != null)
                    request.Marker = response.NextMarker;

                response = this._lambdaClient.ListFunctions(request);

                foreach (var function in response.Functions)
                    lambdas.Add(function.FunctionArn);
            } while (!string.IsNullOrEmpty(response.NextMarker));

            lambdas = new List<string>(lambdas.OrderBy(x => x));
            return lambdas;
        }

        public IList<string> GetLambdaInvokeRoles()
        {
            var roles = new List<string>();
            if (this._iamClient == null)
                return roles;

            var request = new ListRolesRequest();
            ListRolesResponse response = null;
            do
            {
                if (response != null)
                    request.Marker = response.Marker;

                response = this._iamClient.ListRoles(request);

                foreach (var role in response.Roles)
                {
                    if(CheckForS3TrustedEntity(role))
                        roles.Add(role.Arn);
                }
            } while (response.IsTruncated);


            roles = new List<string>(roles.OrderBy(x => x));
            roles.Insert(0, "<New Invocation Role for S3>");
            return roles;
        }

        private bool CheckForS3TrustedEntity(Role role)
        {
            if (string.IsNullOrEmpty(role.AssumeRolePolicyDocument))
                return false;

            var policy = Policy.FromJson(HttpUtility.UrlDecode(role.AssumeRolePolicyDocument));
            foreach (var statement in policy.Statements)
            {
                if (statement.Actions.FirstOrDefault(x => string.Equals(x.ActionName, "sts:AssumeRole", StringComparison.InvariantCultureIgnoreCase)) != null &&
                    statement.Principals.FirstOrDefault(x => string.Equals(x.Id, "s3.amazonaws.com", StringComparison.InvariantCultureIgnoreCase)) != null)
                {
                    return true;
                }
            }

            return false;
        }

    }
}
