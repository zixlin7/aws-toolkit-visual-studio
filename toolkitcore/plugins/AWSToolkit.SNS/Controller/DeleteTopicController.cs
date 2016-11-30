using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit;
using Amazon.AWSToolkit.SNS.Nodes;

using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace Amazon.AWSToolkit.SNS.Controller
{
    public class DeleteTopicController : BaseContextCommand
    {
        public override ActionResults Execute(IViewModel model)
        {
            SNSTopicViewModel topicModel = model as SNSTopicViewModel;
            if (topicModel == null)
                return new ActionResults().WithSuccess(false);

            string msg = string.Format("Are you sure you want to delete the {0} topic?", model.Name);
            if (ToolkitFactory.Instance.ShellProvider.Confirm("Delete Topic", msg))
            {
                try
                {
                    DeleteTopicRequest request = new DeleteTopicRequest() { TopicArn = topicModel.TopicArn };
                    topicModel.SNSClient.DeleteTopic(request);
                }
                catch (Exception e)
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Error deleting topic: " + e.Message);
                    return new ActionResults().WithSuccess(false);
                }
                return new ActionResults()
                    .WithSuccess(true)
                    .WithFocalname(model.Name)
                    .WithShouldRefresh(true);
            }

            return new ActionResults().WithSuccess(false);
        }
    }
}
