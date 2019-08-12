using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.SQS.Nodes;
using Amazon.AWSToolkit.PolicyEditor;
using Amazon.AWSToolkit.PolicyEditor.Model;

using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.SQS.Util;

namespace Amazon.AWSToolkit.SQS.Controller
{
    public class SQSPolicyEditorController : BaseContextCommand, IStandalonePolicyEditorController
    {
        StandalonePolicyEditor _control;
        SQSQueueViewModel _rootModel;

        public override ActionResults Execute(IViewModel model)
        {
            this._rootModel = model as SQSQueueViewModel;
            if (this._rootModel == null)
                return new ActionResults().WithSuccess(false);

            try
            {
                this._control = new StandalonePolicyEditor(this);
                ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);

                return new ActionResults().WithSuccess(true);
            }
            catch(Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error display policy editor: " + e.Message);
                return new ActionResults().WithSuccess(false);
            }
        }

        public string Title => string.Format("SQS Policy: {0}", this._rootModel.Name);

        public PolicyModel.PolicyModelMode PolicyMode => PolicyModel.PolicyModelMode.SQS;

        public string GetPolicyDocument()
        {
            var request = new GetQueueAttributesRequest()
            {
                QueueUrl = this._rootModel.QueueUrl,
                AttributeNames = new List<string>() { SQSConstants.ATTRIBUTE_POLICY }
            };
            var response = this._rootModel.SQSClient.GetQueueAttributes(request);

            if (response == null)
                return string.Empty;

            return response.Policy;
        }

        public void SavePolicyDocument(string document)
        {
            this._rootModel.SQSClient.SetQueueAttribute(
                this._rootModel.QueueUrl, SQSConstants.ATTRIBUTE_POLICY, document ?? string.Empty);
        }
    }
}
