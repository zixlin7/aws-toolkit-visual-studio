using System;
using System.Collections.Generic;
using System.Linq;

using Amazon.Auth.AccessControlPolicy;
using Amazon.Auth.AccessControlPolicy.ActionIdentifiers;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Lambda.Nodes;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.SNS.Model;
using Amazon.AWSToolkit.SNS.Nodes;
using Amazon.AWSToolkit.SNS.Util;
using Amazon.AWSToolkit.SNS.View;
using Amazon.AWSToolkit.SQS.Nodes;
using Amazon.Lambda;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;

using log4net;

namespace Amazon.AWSToolkit.SNS.Controller
{
    public class CreateSubscriptionController
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(CreateSubscriptionController));

        IAmazonSimpleNotificationService _snsClient;
        IAmazonSQS _sqsClient;
        IAmazonLambda _lambdaClient;
        SNSRootViewModel _snsRootViewModel;
        ISQSRootViewModel _sqsRootViewModel;
        CreateSubscriptionModel _model;

        public CreateSubscriptionController(ToolkitContext toolkitContext, SNSRootViewModel snsRootViewModel, CreateSubscriptionModel model)
        {
            ToolkitContext = toolkitContext;
            _snsRootViewModel = snsRootViewModel;
            _sqsRootViewModel = snsRootViewModel.AccountViewModel.FindSingleChild<ISQSRootViewModel>(false);
            _snsClient = _snsRootViewModel.SNSClient;
            _sqsClient = _sqsRootViewModel.SQSClient;

            var lambdaRootViewModel = snsRootViewModel.AccountViewModel.FindSingleChild<ILambdaRootViewModel>(false);
            if (lambdaRootViewModel != null)
            {
                _lambdaClient = lambdaRootViewModel.LambdaClient;
            }

            _model = model;
        }

        public ToolkitContext ToolkitContext { get; }

        public CreateSubscriptionModel Model => _model;

        public ActionResults Execute()
        {
            var control = new CreateSubscriptionControl(this);
            var result = ToolkitContext.ToolkitHost.ShowModal(control);

            return result ? new ActionResults().WithSuccess(true) : ActionResults.CreateCancelled();
        }

        public void Persist()
        {
            _snsClient.Subscribe(new SubscribeRequest()
            {
                TopicArn = _model.TopicArn,
                Protocol = _model.Protocol.SystemName,
                Endpoint = _model.FormattedEndpoint
            });

            if ("SQS".Equals(_model.Protocol.SystemName.ToUpper()))
            {
                AttemptedToGivePermissionOnQueue();
            }
            else if ("LAMBDA".Equals(_model.Protocol.SystemName.ToUpper()))
            {
                AddLambdaEventSource();
            }
        }

        internal void RecordMetric(ActionResults result)
        {
            var connectionSettings = _snsRootViewModel?.AwsConnectionSettings;
            ToolkitContext.RecordSnsCreateSubscription(result, connectionSettings);
        }

        void AddLambdaEventSource()
        {
            if (_lambdaClient == null)
            {
                return;
            }
            
            _lambdaClient.AddPermission(new Amazon.Lambda.Model.AddPermissionRequest
            {
                FunctionName = _model.Endpoint,
                Action = "lambda:InvokeFunction",
                Principal = "sns.amazonaws.com",
                SourceArn = _model.TopicArn,
                StatementId = Guid.NewGuid().ToString() + "-vstoolkit"
            });
        }

        void AttemptedToGivePermissionOnQueue()
        {
            if (_sqsClient == null || string.IsNullOrWhiteSpace(_model.EndpointSqsUrl))
            {
                return;
            }

            try
            {
                var queryUrl = _model.EndpointSqsUrl;

                var getRequest = _sqsClient.GetQueueAttributes(new GetQueueAttributesRequest()
                {
                    AttributeNames = new List<string>() { "All" },
                    QueueUrl = queryUrl
                });


                Policy policy;
                string policyStr = getRequest.Policy;
                if (string.IsNullOrEmpty(policyStr))
                {
                    policy = new Policy();
                }
                else
                {
                    policy = Policy.FromJson(policyStr);
                }

                if (_model.IsAddSQSPermission && !hasSQSPermission(policy))
                {
                    addSQSPermission(policy);
                    _sqsClient.SetQueueAttributes(new SetQueueAttributesRequest()
                    {
                        QueueUrl = queryUrl,
                        Attributes = new Dictionary<string, string>() { { "Policy", policy.ToJson() } }
                    });
                }
            }
            catch (Exception e)
            {
                _logger.Error("Error editing policy for queue", e);
            }
        }

        void addSQSPermission(Policy policy)
        {
            Statement statement = new Statement(Statement.StatementEffect.Allow);
#pragma warning disable CS0618 // Type or member is obsolete (SQSActionIdentifiers)
            statement.Actions.Add(SQSActionIdentifiers.SendMessage);
#pragma warning restore CS0618 // Type or member is obsolete
            statement.Resources.Add(new Resource(_model.Endpoint));
            statement.Conditions.Add(ConditionFactory.NewSourceArnCondition(_model.TopicArn));
            statement.Principals.Add(new Principal("*"));
            policy.Statements.Add(statement);
        }

        bool hasSQSPermission(Policy policy)
        {
            foreach (Statement statement in policy.Statements)
            {
                bool containsResource = false;
                foreach (var resource in statement.Resources)
                {
                    if (resource.Id.Equals(_model.Endpoint))
                    {
                        containsResource = true;
                        break;
                    }
                }

                if (containsResource)
                {
                    foreach (var condition in statement.Conditions)
                    {
                        if ((condition.Type.ToLower().Equals(ConditionFactory.StringComparisonType.StringLike.ToString().ToLower()) ||
                                condition.Type.ToLower().Equals(ConditionFactory.StringComparisonType.StringEquals.ToString().ToLower()) ||
                                condition.Type.ToLower().Equals(ConditionFactory.ArnComparisonType.ArnEquals.ToString().ToLower()) ||
                                condition.Type.ToLower().Equals(ConditionFactory.ArnComparisonType.ArnLike.ToString().ToLower())) &&
                            condition.ConditionKey.ToLower().Equals(ConditionFactory.SOURCE_ARN_CONDITION_KEY.ToLower()) &&
                            condition.Values.Contains<string>(_model.TopicArn))
                            return true;
                    }
                }
            }

            return false;
        }
    }
}
