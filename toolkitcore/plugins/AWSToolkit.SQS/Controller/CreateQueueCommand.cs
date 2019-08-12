using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.SQS.View;
using Amazon.AWSToolkit.SQS.Model;
using Amazon.AWSToolkit.SQS.Nodes;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace Amazon.AWSToolkit.SQS.Controller
{
    public class CreateQueueCommand : BaseContextCommand
    {

        public override ActionResults Execute(IViewModel model)
        {
            var queueModel = model as SQSRootViewModel;
            if (queueModel == null)
                return new ActionResults().WithSuccess(false);

            var control = new CreateQueueControl(new CreateQueueControlModel { SQSClient = queueModel.SQSClient });
            if (ToolkitFactory.Instance.ShellProvider.ShowModal(control))
            {
                CreateQueueResponse response = null;
                try
                {
                    var request = new CreateQueueRequest()
                    {
                        QueueName = control.Model.QueueName,
                        Attributes = new Dictionary<string, string>() { { "VisibilityTimeout", control.Model.DefaultVisiblityTimeout.ToString() } }
                    };

                    if (control.Model.DefaultDelaySeconds >= 0)
                        request.Attributes.Add("DelaySeconds", control.Model.DefaultDelaySeconds.ToString());

                    if (control.Model.UseRedrivePolicy)
                    {
                        var queueArn = queueModel.SQSClient.GetQueueARN(queueModel.CurrentEndPoint.RegionSystemName, control.Model.DeadLetterQueueUrl);
                        var redrivePolicy = string.Format("{{\"maxReceiveCount\":\"{0}\", \"deadLetterTargetArn\":\"{1}\"}}",
                                                         control.Model.MaxReceives,
                                                         queueArn);
                        request.Attributes.Add("RedrivePolicy", redrivePolicy);
                    }

                    response = queueModel.SQSClient.CreateQueue(request);
                }
                catch (Exception e)
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Error creating queue: " + e.Message);
                    return new ActionResults().WithSuccess(false);
                }
                return new ActionResults()
                    .WithSuccess(true)
                    .WithFocalname(control.Model.QueueName)
                    .WithShouldRefresh(true)
                    .WithParameters(new KeyValuePair<string, object>(SQSActionResultsConstants.PARAM_QUEUE_URL, response.QueueUrl));
            }
            else
            {
                return new ActionResults()
                    .WithSuccess(false);
            }    
        }
    }
}
