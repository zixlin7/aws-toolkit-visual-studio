using System;
using System.Collections.Generic;
using System.Threading;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.SNS.Nodes;
using Amazon.AWSToolkit.SNS.View;
using Amazon.AWSToolkit.SNS.Model;
using Amazon.AWSToolkit.SQS.Nodes;
using Amazon.AWSToolkit.Lambda.Nodes;
using Amazon.AWSToolkit.SNS.Util;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace Amazon.AWSToolkit.SNS.Controller
{
    public class ViewSubscriptionsController : BaseContextCommand
    {
        private const int SLEEP_TIME_FOR_REFRESH = 500;

        IAmazonSimpleNotificationService _snsClient;
        ViewSubscriptionsModel _model;
        SNSRootViewModel _snsRootModel;

        public ViewSubscriptionsController(ToolkitContext toolkitContext)
        {
            ToolkitContext = toolkitContext;
        }

        public ToolkitContext ToolkitContext { get; }

        public ViewSubscriptionsModel Model => this._model;

        public override ActionResults Execute(IViewModel model)
        {
            this._snsRootModel = model as SNSRootViewModel;
            if (this._snsRootModel == null)
            {
                return new ActionResults().WithSuccess(false);
            }

            return Execute(this._snsRootModel.SNSClient, string.Format("{0}: Subscriptions", this._snsRootModel.AccountDisplayName));
        }

        public ActionResults Execute(IAmazonSimpleNotificationService snsClient, string title)
        {
            this._snsClient = snsClient;
            this._model = new ViewSubscriptionsModel();

            ViewSubscriptionsControl control = new ViewSubscriptionsControl(this, this._model);
            control.SetTitle(title);
            ToolkitContext.ToolkitHost.OpenInEditor(control);

            return new ActionResults()
                    .WithSuccess(true);
        }

        public ViewSubscriptionsControl CreateSubscriptionEntriesControl(SNSRootViewModel snsRootModel, ViewSubscriptionsModel model, string topicARN)
        {
            this._snsRootModel = snsRootModel;
            this._snsClient = snsRootModel.SNSClient;
            this._model = model;
            
            ViewSubscriptionsControl control = new ViewSubscriptionsControl(this, model);
            control.ExecuteBackGroundLoadDataLoad();

            return control;
        }

        public void CreateSubscription(string topicARN, string queueARN)
        {
            try
            {
                var model = new CreateSubscriptionModel(_snsRootModel.Region.Id);

                if (!string.IsNullOrEmpty(this._model.OwningTopicARN))
                {
                    model.TopicArn = _model.OwningTopicARN;
                    model.IsTopicARNReadOnly = true;
                }

                if (!string.IsNullOrEmpty(queueARN))
                {
                    model.Protocol = SubscriptionProtocol.SQS;
                    model.Endpoint = queueARN;
                }

                if (_snsRootModel != null)
                {
                    foreach (IViewModel viewModel in _snsRootModel.Children)
                    {
                        SNSTopicViewModel topicViewModel = viewModel as SNSTopicViewModel;
                        if(topicViewModel != null)
                            model.PossibleTopicArns.Add(topicViewModel.TopicArn);
                    }

                    AccountViewModel accountViewModel = _snsRootModel.AccountViewModel;
                    if (accountViewModel != null)
                    {
                        ISQSRootViewModel sqsRootViewModel = accountViewModel.FindSingleChild<ISQSRootViewModel>(false);
                        foreach (IViewModel viewModel in sqsRootViewModel.Children)
                        {
                            ISQSQueueViewModel queueViewModel = viewModel as ISQSQueueViewModel;
                            if (queueViewModel == null)
                                continue;

                            var url = queueViewModel.QueueUrl;
                            string arn = queueViewModel.QueueARN;
                            model.AddSqsEndpoint(url, arn);
                        }

                        var lambdaRootViewModel = accountViewModel.FindSingleChild<ILambdaRootViewModel>(false);
                        if (lambdaRootViewModel != null)
                        {
                            foreach (IViewModel viewModel in lambdaRootViewModel.Children)
                            {
                                ILambdaFunctionViewModel lambdaViewModel = viewModel as ILambdaFunctionViewModel;
                                if (lambdaViewModel == null)
                                    continue;

                                model.PossibleLambdaEndpoints.Add(lambdaViewModel.FunctionArn);
                            }
                        }
                    }
                }

                var controller = new CreateSubscriptionController(ToolkitContext, _snsRootModel, model);
                var result = controller.Execute();

                RecordCreateSubscription(result);
                if (result.Success)
                {
                    Thread.Sleep(SLEEP_TIME_FOR_REFRESH);
                    Refresh();
                }

            }
            catch (Exception e)
            {
               ToolkitContext.ToolkitHost.ShowError("Error creating subscription: " + e.Message);
               RecordCreateSubscription(ActionResults.CreateFailed(e));
            }
        }

        public ActionResults DeleteSubscriptions(List<SubscriptionEntry> entries)
        {
            var result = Delete(entries);
            if (result.Success)
            {
                Thread.Sleep(SLEEP_TIME_FOR_REFRESH);
                Refresh();
            }

            return result;
        }

        private ActionResults Delete(List<SubscriptionEntry> entries)
        {
            var pendingSubscriptionCount = 0;
            var failureCount = 0;

            foreach (var entry in entries)
            {
                if (!entry.SubscriptionId.StartsWith("arn:"))
                {
                    pendingSubscriptionCount++;
                    continue;
                }

                try
                {
                    _snsClient.Unsubscribe(new UnsubscribeRequest()
                    {
                        SubscriptionArn = entry.SubscriptionId
                    });
                }
                catch (Exception e)
                {
                    var errMsg = $"An error occurred attempting to delete the subscription {entry.TopicArn}: {e.Message}";
                    ToolkitContext.ToolkitHost.ShowError("Subscription Delete Failed", errMsg);
                    failureCount++;
                }
            }

            if (pendingSubscriptionCount > 0)
            {
                ToolkitContext.ToolkitHost.ShowError("Pending subscriptions can not be unsubscribed.");
            }

            // if total failure and pending count equals all entries to be deleted, report failure result
            if (failureCount > 0 && pendingSubscriptionCount > 0 && (failureCount+pendingSubscriptionCount == entries.Count))
            {
                return ActionResults.CreateFailed();
            }

            return new ActionResults().WithSuccess(true);
        }

        public void Refresh()
        {
            try
            {
                ToolkitContext.ToolkitHost.ExecuteOnUIThread((Action) (() =>
                {
                    LoadModel();
                }));
            }
            catch (Exception e)
            {
                ToolkitContext.ToolkitHost.ShowError("Error refreshing list of subscriptions: " + e.Message);
            }
        }

        public void LoadModel()
        {
            if (string.IsNullOrEmpty(this._model.OwningTopicARN))
                loadAllSubscriptionsEntries();
            else
                loadSubscriptionsEntriesByTopic();
        }

        public void RecordDeleteSubscription(ActionResults result, int count)
        {
            var connectionSettings = _snsRootModel?.AwsConnectionSettings;
            ToolkitContext.RecordSnsDeleteSubscription(result, count, connectionSettings);
        }

        private void RecordCreateSubscription(ActionResults result)
        {
            var connectionSettings = _snsRootModel?.AwsConnectionSettings;
            ToolkitContext.RecordSnsCreateSubscription(result, connectionSettings);
        }

        private void loadSubscriptionsEntriesByTopic()
        {
            List<SubscriptionEntry> items = new List<SubscriptionEntry>();
            var response = new ListSubscriptionsByTopicResponse();
            do
            {
                response = this._snsClient.ListSubscriptionsByTopic(
                    new ListSubscriptionsByTopicRequest()
                    {
                        TopicArn = this._model.OwningTopicARN,
                        NextToken = response.NextToken
                    });

                addSubscriptions(items, response.Subscriptions);

            } while (!string.IsNullOrEmpty(response.NextToken));

            updateChildItemCollection(items);
        }

        private void loadAllSubscriptionsEntries()
        {
            List<SubscriptionEntry> items = new List<SubscriptionEntry>();
            ListSubscriptionsResponse response = new ListSubscriptionsResponse();
            do
            {
                response = this._snsClient.ListSubscriptions(
                    new ListSubscriptionsRequest() { NextToken = response.NextToken });

                addSubscriptions(items, response.Subscriptions);

            } while (!string.IsNullOrEmpty(response.NextToken));

            updateChildItemCollection(items);
        }

        private void updateChildItemCollection(List<SubscriptionEntry> items)
        {
            this._model.SubscriptionEntries.Clear();

            foreach (var item in items)
                this._model.SubscriptionEntries.Add(item);
        }

        private void addSubscriptions(List<SubscriptionEntry> entries, List<Subscription> subscriptions)
        {
            foreach (var sub in subscriptions)
            {
                SubscriptionEntry entry = new SubscriptionEntry()
                {
                    TopicArn = sub.TopicArn,
                    Protocol = sub.Protocol,
                    EndPoint = sub.Endpoint,
                    SubscriptionId = sub.SubscriptionArn
                };

                entries.Add(entry);
            }
        }
    }
}
