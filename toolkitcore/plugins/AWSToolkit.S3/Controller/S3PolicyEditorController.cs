using System;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.S3.Nodes;
using Amazon.AWSToolkit.PolicyEditor;
using Amazon.AWSToolkit.PolicyEditor.Model;
using Amazon.S3.Model;

namespace Amazon.AWSToolkit.S3.Controller
{
    public class S3PolicyEditorController : BaseContextCommand, IStandalonePolicyEditorController
    {
        StandalonePolicyEditor _control;
        S3BucketViewModel _rootModel;

        public override ActionResults Execute(IViewModel model)
        {
            this._rootModel = model as S3BucketViewModel;
            if (this._rootModel == null)
                return new ActionResults().WithSuccess(false);

            try
            {
                this._control = new StandalonePolicyEditor(this);
                ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);

                return new ActionResults().WithSuccess(true);
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error display policy editor: " + e.Message);
                return new ActionResults().WithSuccess(false);
            }
        }

        public string Title => string.Format("S3 Policy: {0}", this._rootModel.Name);

        public PolicyModel.PolicyModelMode PolicyMode => PolicyModel.PolicyModelMode.S3;

        public string GetPolicyDocument()
        {
            var request = new GetBucketPolicyRequest() { BucketName = this._rootModel.Name };
            var response = this._rootModel.S3Client.GetBucketPolicy(request);

            return response.Policy;
        }

        public void SavePolicyDocument(string document)
        {
            if (string.IsNullOrEmpty(document))
            {
                var request = new DeleteBucketPolicyRequest() { BucketName = this._rootModel.Name };
                this._rootModel.S3Client.DeleteBucketPolicy(request);
            }
            else
            {
                var request = new PutBucketPolicyRequest() { BucketName = this._rootModel.Name, Policy = document };
                this._rootModel.S3Client.PutBucketPolicy(request);
            }            
        }
    }
}
