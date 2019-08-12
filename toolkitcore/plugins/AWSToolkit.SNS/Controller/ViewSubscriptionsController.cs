using System;
using System.Collections.Generic;
using System.Threading;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.SNS.Nodes;
using Amazon.AWSToolkit.SNS.View;
using Amazon.AWSToolkit.SNS.Model;
using Amazon.AWSToolkit.SQS.Nodes;
using Amazon.AWSToolkit.Lambda.Nodes;
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

        public ViewSubscriptionsModel Model => this._model;

        public override ActionResults Execute(IViewModel model)
        {
            this._snsRootModel = model as SNSRootViewModel;
            if (this._snsRootModel == null)
                return new ActionResults().WithSuccess(false);

            return Execute(this._snsRootModel.SNSClient, string.Format("{0}: Subscriptions", this._snsRootModel.AccountDisplayName));
        }

        public ActionResults Execute(IAmazonSimpleNotificationService snsClient, string title)
        {
            this._snsClient = snsClient;
            this._model = new ViewSubscriptionsModel();

            ViewSubscriptionsControl control = new ViewSubscriptionsControl(this, this._model);
            control.SetTitle(title);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(control);

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
                CreateSubscriptionModel model = new CreateSubscriptionModel(this._snsRootModel.CurrentEndPoint.RegionSystemName);

                if (!string.IsNullOrEmpty(this._model.OwningTopicARN))
                {
                    model.TopicArn = this._model.OwningTopicARN;
                    model.IsTopicARNReadOnly = true;
                }

                if (!string.IsNullOrEmpty(queueARN))
                {
                    model.Protocol = SubscriptionProtocol.SQS;
                    model.Endpoint = queueARN;
                }

                if (this._snsRootModel != null)
                {
                    foreach (IViewModel viewModel in this._snsRootModel.Children)
                    {
                        SNSTopicViewModel topicViewModel = viewModel as SNSTopicViewModel;
                        if(topicViewModel != null)
                            model.PossibleTopicArns.Add(topicViewModel.TopicArn);
                    }

                    AccountViewModel accountViewModel = this._snsRootModel.AccountViewModel;
                    if (accountViewModel != null)
                    {
                        ISQSRootViewModel sqsRootViewModel = accountViewModel.FindSingleChild<ISQSRootViewModel>(false);
                        foreach (IViewModel viewModel in sqsRootViewModel.Children)
                        {
                            ISQSQueueViewModel queueViewModel = viewModel as ISQSQueueViewModel;
                            if (queueViewModel == null)
                                continue;

                            string arn = queueViewModel.QueueARN;
                            model.PossibleSQSEndpoints.Add(arn);
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

                CreateSubscriptionController controller = new CreateSubscriptionController(this._snsRootModel, model);
                if (controller.Execute())
                {
                    Thread.Sleep(SLEEP_TIME_FOR_REFRESH);
                    this.Refresh();
                }
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating subscription: " + e.Message);
            }
        }

        public bool DeleteSubscriptions(List<SubscriptionEntry> entries)
        {
            bool deletedItems = false;
            try
            {
                bool pendingSubscriptions = false;
                foreach (SubscriptionEntry entry in entries)
                {
                    if (!entry.SubscriptionId.StartsWith("arn:"))
                    {
                        pendingSubscriptions = true;                        
                        continue;
                    }

                    this._snsClient.Unsubscribe(new UnsubscribeRequest() { SubscriptionArn = entry.SubscriptionId });

                    deletedItems = true;
                }

                if (pendingSubscriptions)
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Pending subscriptions can not be unsubscribed.");
                }
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Deleting Subscriptions: " + e.Message);
            }
            finally
            {
                if (deletedItems)
                {
                    Thread.Sleep(SLEEP_TIME_FOR_REFRESH);
                    this.Refresh();
                }
            }

            return deletedItems;
        }

        public void Refresh()
        {
            try
            {
                ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
                {
                    LoadModel();
                }));
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing list of subscriptions: " + e.Message);
            }
        }

        public void LoadModel()
        {
            if (string.IsNullOrEmpty(this._model.OwningTopicARN))
                loadAllSubscriptionsEntries();
            else
                loadSubscriptionsEntriesByTopic();
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
