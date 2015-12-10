using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;

using Amazon.Auth.AccessControlPolicy;

using Amazon.S3;
using Amazon.S3.Model;

using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SimpleNotificationService.Util;

using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.SQS.Util;

using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;

using Amazon.Lambda;
using Amazon.Lambda.Model;

using Amazon.AWSToolkit;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.S3.View;

namespace Amazon.AWSToolkit.S3.Controller
{
    public class AddEventConfigurationController
    {

        bool _success;
        string _region;
        IAmazonS3 _s3Client;
        string _bucketName;
        AddEventConfigurationControl _control;

        IAmazonIdentityManagementService _iamClient;
        IAmazonSimpleNotificationService _snsClient;
        IAmazonSQS _sqsClient;
        IAmazonLambda _lambdaClient;

        public bool Execute(IAmazonS3 s3Client, string bucketName, string region, AccountViewModel account)
        {
            this._s3Client = s3Client;
            this._bucketName = bucketName;
            this._region = region;
            RegionEndPointsManager.RegionEndPoints endPoints = RegionEndPointsManager.Instance.GetRegion(region);

            var iamConfig = new AmazonIdentityManagementServiceConfig();
            iamConfig.ServiceURL = endPoints.GetEndpoint(RegionEndPointsManager.IAM_SERVICE_NAME).Url;
            this._iamClient = new AmazonIdentityManagementServiceClient(account.Credentials, iamConfig);

            var sqsConfig = new AmazonSQSConfig();
            sqsConfig.ServiceURL = endPoints.GetEndpoint(RegionEndPointsManager.SQS_SERVICE_NAME).Url;
            this._sqsClient = new AmazonSQSClient(account.Credentials, sqsConfig);

            var snsConfig = new AmazonSimpleNotificationServiceConfig();
            snsConfig.ServiceURL = endPoints.GetEndpoint(RegionEndPointsManager.SNS_SERVICE_NAME).Url;
            this._snsClient = new AmazonSimpleNotificationServiceClient(account.Credentials, snsConfig);

            var lambdaConfig = new AmazonLambdaConfig();
            lambdaConfig.ServiceURL = endPoints.GetEndpoint(RegionEndPointsManager.LAMBDA_SERVICE_NAME).Url;
            this._lambdaClient = new AmazonLambdaClient(account.Credentials, lambdaConfig);

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

            switch (this._control.Service)
            {
                case AddEventConfigurationControl.ServiceType.SNS:
                    if (this._control.AddPermissions)
                    {
                        this._snsClient.AuthorizeS3ToPublish(this._control.ResourceArn, this._bucketName);
                    }
                    var topicConfig = new TopicConfiguration{Topic = this._control.ResourceArn};
                    topicConfig.Events.Add(this._control.Event);
                    putRequest.TopicConfigurations.Add(topicConfig);
                    break;
                case AddEventConfigurationControl.ServiceType.SQS:
                    var queueUrl = this._queueArnsToQueueURls[this._control.ResourceArn];
                    if (this._control.AddPermissions)
                    {
                        this._sqsClient.AuthorizeS3ToSendMessage(queueUrl, this._bucketName);
                    }
                    var queueConfig = new QueueConfiguration{Queue = this._control.ResourceArn};
                    queueConfig.Events.Add(this._control.Event);
                    putRequest.QueueConfigurations.Add(queueConfig);
                    break;
                case AddEventConfigurationControl.ServiceType.Lambda:
                    AddLambdaConfiguration(putRequest);
                    break;
            }

            this._s3Client.PutBucketNotification(putRequest);
            this._success = true;
        }

        private void AddLambdaConfiguration(PutBucketNotificationRequest putRequest)
        {
            if (this._control.AddPermissions)
            {
                this._lambdaClient.AddPermission(new Amazon.Lambda.Model.AddPermissionRequest
                {
                    FunctionName = this._control.ResourceArn,
                    Action = "lambda:InvokeFunction",
                    Principal = "s3.amazonaws.com",
                    SourceArn = string.Format("arn:aws:s3:::{0}", this._bucketName),
                    StatementId = Guid.NewGuid().ToString() + "-vstoolkit"
                });
            }

            var lambdaConfig = new LambdaFunctionConfiguration
            {
                FunctionArn = this._control.ResourceArn
            };
            lambdaConfig.Events.Add(this._control.Event);
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
                var arn = string.Format("arn:aws:sqs:{0}:{1}:{2}", this._region, tokens[tokens.Length - 2], tokens[tokens.Length - 1]);
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
