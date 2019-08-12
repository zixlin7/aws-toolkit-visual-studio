using System;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.CloudFront.Nodes;
using Amazon.AWSToolkit.CloudFront.View;
using Amazon.AWSToolkit.CloudFront.Model;
using Amazon.CloudFront.Model;

namespace Amazon.AWSToolkit.CloudFront.Controller
{
    public class EditDistributionConfigController : BaseDistributionConfigEditorController
    {
        EditDistributionConfigControl _control;
        EditDistributionConfigModel _model;
        CloudFrontDistributionViewModel _rootModel;

        public override ActionResults Execute(IViewModel model)
        {
            this._rootModel = model as CloudFrontDistributionViewModel;
            if (this._rootModel == null)
                return new ActionResults().WithSuccess(false);

            this._cfClient = this._rootModel.CFClient;
            this._model = new EditDistributionConfigModel();
            this._control = new EditDistributionConfigControl(this);

            base.Initialize(this._cfClient, this._rootModel.AccountViewModel, this._model, false);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);

            return new ActionResults().WithSuccess(true);
        }

        public string Title => this._rootModel.Name;

        public string UniqueId => "Distribtion: " + this._rootModel.DistributionId;

        public override void LoadModel()
        {
            base.LoadModel();
            Refresh();

        }

        public void Refresh()
        {
            var response = this._cfClient.GetDistribution(
                new GetDistributionRequest() { Id = this._rootModel.DistributionId });

            this._model.LoadDistributionConfig(response.Distribution.DistributionConfig);

            this._model.Id = response.Distribution.Id;
            this._model.ETag = response.ETag;
            this._model.DomainName = response.Distribution.DomainName;
            this._model.Status = response.Distribution.Status;
            this._model.LastModifedDate = response.Distribution.LastModifiedTime.ToLocalTime().ToString();
            this._model.IsDirty = false;

            foreach (var cname in this._model.CNAMEs)
            {
                cname.PropertyChanged += this._model.OnPropertyChanged;
            }

            foreach (var signers in this._model.TrustedSignerAWSAccountIds)
            {
                signers.PropertyChanged += this._model.OnPropertyChanged;
            }
        }

        public EditDistributionConfigModel Model => this._model;

        public bool Persist()
        {
            try
            {
                var config = this._model.ConvertToDistribtionConfig();

                var request = new UpdateDistributionRequest()
                {
                    DistributionConfig = config,
                    IfMatch = this._model.ETag,
                    Id = this._model.Id
                };

                this._cfClient.UpdateDistribution(request);
                this.Refresh();
                return true;
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error saving distribution: " + e.Message);
                return false;
            }
        }
    }
}
