﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.DynamoDBv2;
using Amazon.IdentityManagement;
using Amazon.Kinesis;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.CloudWatchEvents;
using Amazon.CloudWatchEvents.Model;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Lambda.Util;
using Amazon.AWSToolkit.Lambda.View;
using Amazon.AWSToolkit.Regions;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using AddPermissionRequest = Amazon.Lambda.Model.AddPermissionRequest;

namespace Amazon.AWSToolkit.Lambda.Controller
{
    public class AddEventSourceController
    {
        public const string TRUSTENTITY_LAMBDA = "lambda.amazonaws.com";
        public const string TRUSTENTITY_S3 = "s3.amazonaws.com";

        IAmazonLambda _lambdaClient;
        IAmazonDynamoDB _dynamoDBClient;
        IAmazonDynamoDBStreams _dynamoDBStreamsClient;
        IAmazonIdentityManagementService _iamClient;
        IAmazonKinesis _kinesisClient;
        IAmazonS3 _s3Client;
        IAmazonSQS _sqsClient;
        IAmazonSimpleNotificationService _snsClient;
        IAmazonCloudWatchEvents _cloudWatchEventsClient;

        bool success;
        AddEventSourceControl _control;
        string _accountNumber;

        string _functionARN;
        string _functionName;
        string _role;
        ToolkitRegion _region;
        AccountViewModel _account;
        private readonly ToolkitContext _toolkitContext;

        public AddEventSourceController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public bool Execute(IAmazonLambda lambdaClient, AccountViewModel account, ToolkitRegion region, string functionARN, string role)
        {
            this._account = account;
            this._region = region;
            this._functionARN = functionARN;
            this._role = role;

            var tokens = this._functionARN.Split(':');
            this._accountNumber = tokens[4];
            this._functionName = tokens[6];

            this._lambdaClient = lambdaClient;

            this._dynamoDBClient = _account.CreateServiceClient<AmazonDynamoDBClient>(_region);
            this._dynamoDBStreamsClient = _account.CreateServiceClient<AmazonDynamoDBStreamsClient>(_region);
            this._iamClient = _account.CreateServiceClient<AmazonIdentityManagementServiceClient>(_region);
            this._kinesisClient = _account.CreateServiceClient<AmazonKinesisClient>(_region);
            this._s3Client = _account.CreateServiceClient<AmazonS3Client>(_region);
            this._sqsClient = _account.CreateServiceClient<AmazonSQSClient>(_region);
            this._snsClient = _account.CreateServiceClient<AmazonSimpleNotificationServiceClient>(_region);
            this._cloudWatchEventsClient = _account.CreateServiceClient<AmazonCloudWatchEventsClient>(_region);

            this._control = new AddEventSourceControl(this);
            ToolkitFactory.Instance.ShellProvider.ShowModal(this._control);
            return success;
        }

        public void Persist()
        {
            try
            {
                switch (this._control.EventSourceType)
                {
                    case AddEventSourceControl.SourceType.S3:
                        success = SaveS3EventConfiguration();
                        break;
                    case AddEventSourceControl.SourceType.SNS:
                        success = SaveSNSSubscription();
                        break;
                    case AddEventSourceControl.SourceType.CloudWatchEventsSchedule:
                        success = SaveCloudWatchEventsSchedule();
                        break;
                    default:
                        success = SaveEventSource();
                        break;
                }
            }
            finally
            {
                _toolkitContext.TelemetryLogger.RecordLambdaAddEvent(
                    success ? Result.Succeeded : Result.Failed,
                    this._control.EventSourceType.ToString(),
                    this._account?.GetAccountId(this._region),
                    this._region?.Id);
            }
        }

        private bool SaveS3EventConfiguration()
        {
            IAmazonS3 bucketS3Client;
            string bucketRegion = null;
            Amazon.AWSToolkit.S3.S3Utils.BuildS3ClientForBucket(this._account, this._s3Client, this._control.Resource, _toolkitContext.RegionProvider, out bucketS3Client, ref bucketRegion);

            if (!string.Equals(this._region.Id, bucketRegion, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ApplicationException("The S3 bucket is in a different region then the Lambda function. Notifications require that the bucket and function be in the same region.");
            }

            string sourceArn = string.Format("arn:aws:s3:::{0}", this._control.Resource);

            this._lambdaClient.AddPermission(CreateAddEventSourcePermissionRequest(sourceArn, "s3.amazonaws.com", this._functionARN, this._accountNumber));

            var getResponse = bucketS3Client.GetBucketNotification(this._control.Resource);

            var requestPutNotification = new PutBucketNotificationRequest
            {
                BucketName = this._control.Resource,
                LambdaFunctionConfigurations = getResponse.LambdaFunctionConfigurations,
                QueueConfigurations = getResponse.QueueConfigurations,
                TopicConfigurations = getResponse.TopicConfigurations
            };

            var lambdaConfiguration = new LambdaFunctionConfiguration
            {
                Events = new List<EventType> {EventType.ObjectCreatedAll },
                FunctionArn = this._functionARN,
                Id = string.Format("VSToolkitQuickCreate_{0}", this._functionName)
            };

            if(!string.IsNullOrEmpty(this._control.Prefix) || !string.IsNullOrEmpty(this._control.Suffix))
            {
                var s3KeyFilter = new S3KeyFilter();
                lambdaConfiguration.Filter = new Filter { S3KeyFilter = s3KeyFilter };

                if(!string.IsNullOrEmpty(this._control.Prefix))
                {
                    s3KeyFilter.FilterRules.Add(new FilterRule
                    {
                        Name = "prefix",
                        Value = this._control.Prefix
                    });
                }
                if (!string.IsNullOrEmpty(this._control.Suffix))
                {
                    s3KeyFilter.FilterRules.Add(new FilterRule
                    {
                        Name = "suffix",
                        Value = this._control.Suffix
                    });
                }
            }

            requestPutNotification.LambdaFunctionConfigurations.Add(lambdaConfiguration);

            // Get client for selected bucket.
            bucketS3Client.PutBucketNotification(requestPutNotification);
            return true;
        }

        private bool SaveSNSSubscription()
        {
            this._snsClient.Subscribe(new SubscribeRequest()
            {
                TopicArn = this._control.Resource,
                Protocol = "lambda",
                Endpoint = this._functionARN
            });

            this._lambdaClient.AddPermission(CreateAddEventSourcePermissionRequest(this._control.Resource, "sns.amazonaws.com", this._functionARN, this._accountNumber));
            return true;
        }

        private bool SaveCloudWatchEventsSchedule()
        {
            var putResponse = this._cloudWatchEventsClient.PutRule(new PutRuleRequest
            {
                Name = this._control.ScheduleRuleName,
                Description = this._control.ScheduleRuleDescription,
                ScheduleExpression = this._control.ScheduleExpression,
                State = RuleState.ENABLED
            });

            this._cloudWatchEventsClient.PutTargets(new PutTargetsRequest
            {
                Rule = this._control.ScheduleRuleName,
                Targets = new List<Target>
                {
                    new Target
                    {
                        Arn = this._functionARN,
                        Id = Guid.NewGuid().ToString()
                    }
                }
            });

            this._lambdaClient.AddPermission(CreateAddEventSourcePermissionRequest(putResponse.RuleArn, "events.amazonaws.com", this._functionARN, this._accountNumber));
            return true;
        }

        private bool SaveEventSource()
        {
            var request = new CreateEventSourceMappingRequest
            {
                FunctionName = this._functionName
            };

            switch (this._control.EventSourceType)
            {
                case AddEventSourceControl.SourceType.DynamoDBStream:
                    request.BatchSize = this._control.BatchSize;
                    request.StartingPosition = this._control.StartPosition;
                    var tokens = this._control.Resource.Split('/');
                    request.EventSourceArn = string.Format("arn:aws:dynamodb:{0}:{1}:table/{2}/stream/{3}", this._region.Id, this._accountNumber, tokens[0], tokens[1]);
                    break;
                case AddEventSourceControl.SourceType.Kinesis:
                    request.BatchSize = this._control.BatchSize;
                    request.StartingPosition = this._control.StartPosition;
                    request.EventSourceArn = string.Format("arn:aws:kinesis:{0}:{1}:stream/{2}", this._region.Id, this._accountNumber, this._control.Resource);
                    break;
                case AddEventSourceControl.SourceType.SQS:
                    request.BatchSize = this._control.SQSBatchSize;
                    request.EventSourceArn = string.Format("arn:aws:sqs:{0}:{1}:{2}", this._region.Id, this._accountNumber, this._control.Resource);
                    break;
            }

            if (request.EventSourceArn == null)
                return false;

            try
            {
                this._lambdaClient.CreateEventSourceMapping(request);
            }
            catch (AmazonLambdaException e)
            {
                if (e.Message.StartsWith("Cannot access stream arn:aws:kinesis"))
                {
                    if (ToolkitFactory.Instance.ShellProvider.Confirm("Add Policy",
                        "The IAM role executing the Lambda function does not have permission to read from the Kinesis stream. " +
                        "Do you wish to apply a read only policy for Kinesis to the role and continue adding the event source?"))
                    {
                        ApplyRoleTypeAndCreateEventSourceWithRetry(LambdaUtilities.RoleType.Kinesis, request);
                    }
                    else
                        return false;
                }
                else if(e.Message.StartsWith("The provided execution role does not have permissions to call ReceiveMessage on SQS"))
                {
                    if (ToolkitFactory.Instance.ShellProvider.Confirm("Add Policy",
                        "The IAM role executing the Lambda function does not have permission to read messages from the SQS queue. " +
                        "Do you wish to apply a read only policy for SQS to the role and continue adding the event source?"))
                    {
                        ApplyRoleTypeAndCreateEventSourceWithRetry(LambdaUtilities.RoleType.SQS, request);
                    }
                    else
                        return false;

                }
                else if (e.Message.StartsWith("Cannot access stream arn:aws:dynamodb"))
                {
                    if (ToolkitFactory.Instance.ShellProvider.Confirm("Add Policy",
                        "The IAM role executing the Lambda function does not have permission to read from the DynamoDB stream. " +
                        "Do you wish to apply a read only policy for DynamoDB to the role and continue adding the event source?"))
                    {
                        ApplyRoleTypeAndCreateEventSourceWithRetry(LambdaUtilities.RoleType.DynamoDBStream, request);
                    }
                    else
                        return false;
                }
                else
                    throw;
            }

            return true;
        }

        private void ApplyRoleTypeAndCreateEventSourceWithRetry(LambdaUtilities.RoleType roleType, CreateEventSourceMappingRequest request)
        {
            LambdaUtilities.ApplyPolicyToRole(this._iamClient, roleType, this._role);

            const int MAX_RETRIES = 10;
            for (int i = 0; true; i++)
            {
                try
                {
                    this._lambdaClient.CreateEventSourceMapping(request);
                    break;
                }
                catch (AmazonLambdaException e)
                {
                    if (e.Message.StartsWith("Cannot access stream arn:aws:kinesis"))
                    {
                        if (i == MAX_RETRIES)
                            throw;

                        Thread.Sleep(1000);
                    }
                }
            }
        }

        public List<string> GetS3Buckets()
        {
            var buckets = new List<string>();
            if (this._s3Client == null)
                return buckets;

            var response = this._s3Client.ListBuckets();
            foreach (var bucket in response.Buckets.OrderBy(x => x.BucketName))
            {
                buckets.Add(bucket.BucketName);
            }

            return buckets;
        }

        public List<string> GetSNSTopics()
        {
            var topics = new List<string>();
            if (this._snsClient == null)
                return topics;

            var response = this._snsClient.ListTopics();
            foreach (var topic in response.Topics.OrderBy(x => x.TopicArn))
            {
                topics.Add(topic.TopicArn);
            }

            return topics;
        }

        public List<string> GetSQSTopics()
        {
            var queues = new List<string>();
            if (this._sqsClient == null)
                return queues;

            var response = this._sqsClient.ListQueues(new ListQueuesRequest());
            foreach (var queueUrl in response.QueueUrls.OrderBy(x => x))
            {
                queues.Add(queueUrl.Substring(queueUrl.LastIndexOf("/") + 1));
            }

            return queues;
        }

        public List<string> GetDynamoDBStreams()
        {
            var streams = new List<string>();
            if (this._dynamoDBStreamsClient == null)
                return streams;

            var request = new Amazon.DynamoDBv2.Model.ListStreamsRequest();
            Amazon.DynamoDBv2.Model.ListStreamsResponse response = null;

            do
            {
                if (response != null)
                    request.ExclusiveStartStreamArn = response.LastEvaluatedStreamArn;

                response = this._dynamoDBStreamsClient.ListStreams(request);

                foreach (var stream in response.Streams)
                {
                    var tokens = stream.StreamArn.Split('/');
                    var streamName = string.Format("{0}/{1}", tokens[1], tokens[tokens.Length - 1]);
                    streams.Add(streamName);
                }
            } while (!string.IsNullOrEmpty(response.LastEvaluatedStreamArn));

            streams = new List<string>(streams.OrderBy(x => x));
            return streams;
        }

        public List<string> GetKinesisStreams()
        {
            var streams = new List<string>();
            if (this._kinesisClient == null)
                return streams;

            var request = new Amazon.Kinesis.Model.ListStreamsRequest();
            Amazon.Kinesis.Model.ListStreamsResponse response = null;

            do{
                if (response != null)
                    request.ExclusiveStartStreamName = response.StreamNames[response.StreamNames.Count - 1];

                response = this._kinesisClient.ListStreams(request);

                streams.AddRange(response.StreamNames);
            }while(response.HasMoreStreams);

            streams = new List<string>(streams.OrderBy(x => x));
            return streams;
        }

        /// <summary>
        /// Create an add permission request for the specified event source 
        /// </summary>
        public static AddPermissionRequest CreateAddEventSourcePermissionRequest(string sourceArn, string principal, string functionArn, string accountNumber)
        {
            var request = new AddPermissionRequest
            {
                FunctionName = functionArn,
                Action = "lambda:InvokeFunction",
                Principal = principal,
                SourceArn = sourceArn,
                StatementId = Guid.NewGuid().ToString() + "-vstoolkit"
            };
            if (principal.StartsWith("s3", StringComparison.OrdinalIgnoreCase))
            {
                request.SourceAccount = accountNumber;
            }
            return request;
        }
    }
}
