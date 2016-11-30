using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.SNS.Nodes;
using Amazon.AWSToolkit.PolicyEditor;
using Amazon.AWSToolkit.PolicyEditor.Model;
using Amazon.AWSToolkit;

using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace Amazon.AWSToolkit.SNS.Controller
{
    public class SNSPolicyEditorController : BaseContextCommand, IStandalonePolicyEditorController
    {
        StandalonePolicyEditor _control;
        SNSTopicViewModel _rootModel;

        public override ActionResults Execute(IViewModel model)
        {
            this._rootModel = model as SNSTopicViewModel;
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

        public string Title
        {
            get { return string.Format("SNS Policy: {0}", this._rootModel.Name); }
        }

        public PolicyModel.PolicyModelMode PolicyMode
        {
            get { return PolicyModel.PolicyModelMode.SNS; }
        }

        public string GetPolicyDocument()
        {
            var request = new GetTopicAttributesRequest()
            {
                TopicArn = this._rootModel.TopicArn
            };
            var response = this._rootModel.SNSClient.GetTopicAttributes(request);

            return response.GetPolicy();
        }

        public void SavePolicyDocument(string document)
        {
            this._rootModel.SNSClient.SetTopicAttribute(
                this._rootModel.TopicArn, GetTopicAttributesResponseExt.POLICY, document);
        }
    }
}
