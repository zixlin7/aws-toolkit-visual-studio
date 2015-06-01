using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.SNS.View;
using Amazon.AWSToolkit.SNS.Model;
using Amazon.AWSToolkit.SNS.Nodes;
using Amazon.AWSToolkit.SQS.Nodes;
using Amazon.AWSToolkit.Lambda.Nodes;
using Amazon.AWSToolkit;

using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

using Amazon.Auth.AccessControlPolicy;
using Amazon.Auth.AccessControlPolicy.ActionIdentifiers;
using Amazon.SQS;
using Amazon.SQS.Model;

using Amazon.Lambda;
using Amazon.Lambda.Model;

using log4net;

namespace Amazon.AWSToolkit.SNS.Controller
{
    public class CreateSubscriptionController
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(CreateSubscriptionController));

        IAmazonSimpleNotificationService _snsClient;
        IAmazonSQS _sqsClient;
        IAmazonLambda _lambdaClient;
        SNSRootViewModel _snsRootViewModel;
        ISQSRootViewModel _sqsRootViewModel;
        CreateSubscriptionModel _model;

        public CreateSubscriptionController(SNSRootViewModel snsRootViewModel, string topicArn)
            : this(snsRootViewModel, new CreateSubscriptionModel(snsRootViewModel.CurrentEndPoint.RegionSystemName))
        {
            this._model.TopicArn = topicArn;
        }

        public CreateSubscriptionController(SNSRootViewModel snsRootViewModel, CreateSubscriptionModel model)
        {
            this._snsRootViewModel = snsRootViewModel;
            this._sqsRootViewModel = snsRootViewModel.AccountViewModel.FindSingleChild<ISQSRootViewModel>(false);
            this._snsClient = this._snsRootViewModel.SNSClient;
            this._sqsClient = this._sqsRootViewModel.SQSClient;

            var lambdaRootViewModel = snsRootViewModel.AccountViewModel.FindSingleChild<ILambdaRootViewModel>(false);
            if (lambdaRootViewModel != null)
                this._lambdaClient = lambdaRootViewModel.LambdaClient;

            this._model = model;
        }

        public CreateSubscriptionModel Model
        {
           get{return this._model; }
        }

       public bool Execute()
       {
           CreateSubscriptionControl control = new CreateSubscriptionControl(this);
           return ToolkitFactory.Instance.ShellProvider.ShowModal(control);
       }

       public void Persist()
       {
           this._snsClient.Subscribe(new SubscribeRequest()
           {
               TopicArn = this._model.TopicArn,
               Protocol = this._model.Protocol.SystemName,
               Endpoint = this._model.FormattedEndpoint
           });

           if ("SQS".Equals(this._model.Protocol.SystemName.ToUpper()))
           {
               this.AttemptedToGivePermissionOnQueue();
           }
           else if ("LAMBDA".Equals(this._model.Protocol.SystemName.ToUpper()))
           {
               this.AddLambdaEventSource();
           }
       }

       void AddLambdaEventSource()
       {
           if (this._lambdaClient == null)
               return;

           this._lambdaClient.AddPermission(new Amazon.Lambda.Model.AddPermissionRequest
           {
               FunctionName = this._model.Endpoint,
               Action = "lambda:InvokeFunction",
               Principal = "sns.amazonaws.com",
               SourceArn = this._model.TopicArn,
               StatementId = Guid.NewGuid().ToString() + "-vstoolkit"
           });
       }

        void AttemptedToGivePermissionOnQueue()
        {
           if (this._sqsClient == null)
               return;

           try
           {
               string[] tokens = this._model.Endpoint.Split(':');
               string queryUrl = this._sqsRootViewModel.CurrentEndPoint.Url;
               if (!queryUrl.EndsWith("/"))
                   queryUrl += "/";
               queryUrl += string.Format("{0}/{1}", tokens[4], tokens[5]);

               var getRequest = this._sqsClient.GetQueueAttributes(new GetQueueAttributesRequest()
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

               if (this._model.IsAddSQSPermission && !hasSQSPermission(policy))
               {
                   addSQSPermission(policy);
                   this._sqsClient.SetQueueAttributes(new SetQueueAttributesRequest()
                   {
                       QueueUrl = queryUrl,
                       Attributes = new Dictionary<string, string>() { { "Policy", policy.ToJson() } }
                   });
               }
           }
           catch (Exception e)
           {
               LOGGER.Error("Error editing policy for queue", e);
           }
        }

        void addSQSPermission(Policy policy)
        {
            Statement statement = new Statement(Statement.StatementEffect.Allow);
            statement.Actions.Add(SQSActionIdentifiers.SendMessage);
            statement.Resources.Add(new Resource(this._model.Endpoint));
            statement.Conditions.Add(ConditionFactory.NewSourceArnCondition(this._model.TopicArn));
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
                    if (resource.Id.Equals(this._model.Endpoint))
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
                            condition.Values.Contains<string>(this._model.TopicArn))
                            return true;
                    }
                }
            }

            return false;
        }
    }
}
