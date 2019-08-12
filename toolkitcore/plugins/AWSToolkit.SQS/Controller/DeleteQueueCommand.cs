using System;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.SQS.Nodes;
using Amazon.SQS.Model;

namespace Amazon.AWSToolkit.SQS.Controller
{
    public class DeleteQueueCommand : BaseContextCommand
    {

        public override ActionResults Execute(IViewModel model)
        {
            SQSQueueViewModel queueModel = model as SQSQueueViewModel;
            if (queueModel != null)
            {
                string msg = string.Format("Are you sure you want to delete the {0} queue?", model.Name);
                if (ToolkitFactory.Instance.ShellProvider.Confirm("Confirm Delete", msg))
                {
                    try
                    {
                        queueModel.SQSClient.DeleteQueue(new DeleteQueueRequest() { QueueUrl = queueModel.QueueUrl });
                        return new ActionResults().WithSuccess(true);
                    }
                    catch (Exception e)
                    {
                        ToolkitFactory.Instance.ShellProvider.ShowError(string.Format("Error deleting queue {0}: ", model.Name, e.Message));
                    }
                }
            }

            return new ActionResults().WithSuccess(false);        }
     }
}
