using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.SQS.View;
using Amazon.AWSToolkit.SQS.Model;
using Amazon.AWSToolkit.SQS.Nodes;

using Amazon.SQS;
using Amazon.SQS.Model;

namespace Amazon.AWSToolkit.SQS.Controller
{
    public class PermissionsCommand : BaseContextCommand
    {
        public override ActionResults Execute(IViewModel model)
        {
            SQSQueueViewModel queueModel = model as SQSQueueViewModel;
            if (queueModel == null)
                return new ActionResults().WithSuccess(false);

            var response = queueModel.SQSClient.GetQueueAttributes(new GetQueueAttributesRequest()
            {
                AttributeNames = new List<string>() { "Policy" },
                QueueUrl = queueModel.QueueUrl
            });

            QueuePermissionsModel qpm;
            if (response == null || response.Attributes.Count == 0)
                qpm = new QueuePermissionsModel();
            else
            {
                string policy = getAttributeValue(response, "Policy");
                qpm = new QueuePermissionsModel(policy);
            }

            var control = new QueuePermissionsControl(qpm);
            if (ToolkitFactory.Instance.ShellProvider.ShowModal(control))
            {
                processActions(queueModel, qpm);
                return new ActionResults()
                    .WithSuccess(true);
            }

            return new ActionResults()
                .WithSuccess(false);
        }

        private void processActions(SQSQueueViewModel queueModel, QueuePermissionsModel model)
        {
            IAmazonSQS sqsClient = queueModel.SQSClient;
            List<QueuePermissionsModel.PersistAction> persistActions = model.GetPersistActions();

            foreach (QueuePermissionsModel.PersistAction action in persistActions)
            {
                try
                {
                    QueuePermissionsModel.PermissionRecord record = action.Record;
                    switch (action.ActionToTake)
                    {
                        case QueuePermissionsModel.PersistAction.Action.Add:
                            sqsClient.AddPermission(new AddPermissionRequest()
                            {
                                Actions = new List<string>() { record.Action },
                                AWSAccountIds = new List<string>() { record.AWSAccountId },
                                Label = record.Label,
                                QueueUrl = queueModel.QueueUrl
                            });
                            break;
                        case QueuePermissionsModel.PersistAction.Action.Modify:
                            sqsClient.RemovePermission(new RemovePermissionRequest()
                            {
                                Label = record.Label,
                                QueueUrl = queueModel.QueueUrl
                            });

                            sqsClient.AddPermission(new AddPermissionRequest()
                            {
                                Actions = new List<string>() { record.Action },
                                AWSAccountIds = new List<string>() { record.AWSAccountId },
                                Label = record.Label,
                                QueueUrl = queueModel.QueueUrl
                            });
                            break;
                        case QueuePermissionsModel.PersistAction.Action.Delete:
                            sqsClient.RemovePermission(new RemovePermissionRequest()
                            {
                                Label = record.Label,
                                QueueUrl = queueModel.QueueUrl
                            });
                            break;
                    }
                }
                catch (Exception e)
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Error persisting permissions: " + e.Message);
                }
            }
        }

        private string getAttributeValue(GetQueueAttributesResponse response, string field)
        {
            string value;
            response.Attributes.TryGetValue(field, out value);
            return value;
        }
    }
}
