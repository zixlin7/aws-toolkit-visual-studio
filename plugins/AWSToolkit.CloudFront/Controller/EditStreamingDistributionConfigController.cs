using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.CloudFront.Nodes;
using Amazon.AWSToolkit.CloudFront.View;
using Amazon.AWSToolkit.CloudFront.Model;
using Amazon.AWSToolkit.S3.Nodes;
using Amazon.AWSToolkit;

using Amazon.CloudFront;
using Amazon.CloudFront.Model;


namespace Amazon.AWSToolkit.CloudFront.Controller
{
    public class EditStreamingDistributionConfigController : BaseDistributionConfigEditorController
    {
        EditStreamingDistributionConfigControl _control;
        EditStreamingDistributionConfigModel _model;
        CloudFrontStreamingDistributionViewModel _rootModel;

        public override ActionResults Execute(IViewModel model)
        {
            this._rootModel = model as CloudFrontStreamingDistributionViewModel;
            if (this._rootModel == null)
                return new ActionResults().WithSuccess(false);

            this._cfClient = this._rootModel.CFClient;
            this._model = new EditStreamingDistributionConfigModel();
            this._control = new EditStreamingDistributionConfigControl(this);

            base.Initialize(this._cfClient, this._rootModel.AccountViewModel, this._model, false);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);

            return new ActionResults().WithSuccess(true);

        }

        public string Title
        {
            get { return this._rootModel.Name; }
        }

        public string UniqueId
        {
            get { return "StreamingDistribtion: " + this._rootModel.DistributionId; }
        }

        public override void LoadModel()
        {
            base.LoadModel();
            Refresh();
        }

        public EditStreamingDistributionConfigModel Model
        {
            get { return this._model; }
        }

        public void Refresh()
        {
            var response = this._cfClient.GetStreamingDistribution(
                new GetStreamingDistributionRequest() { Id = this._rootModel.DistributionId });

            this._model.LoadStreamingDistributionConfig(response.StreamingDistribution.StreamingDistributionConfig);

            this._model.Id = response.StreamingDistribution.Id;
            this._model.ETag = response.ETag;
            this._model.DomainName = response.StreamingDistribution.DomainName;
            this._model.Status = response.StreamingDistribution.Status;
            this._model.LastModifedDate = response.StreamingDistribution.LastModifiedTime.ToLocalTime().ToString();
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

        public bool Persist()
        {
            try
            {
                var config = this._model.ConvertToStreamingDistribtionConfig();
                var request = new UpdateStreamingDistributionRequest()
                {
                    IfMatch = this._model.ETag,
                    StreamingDistributionConfig = config,
                    Id = this._model.Id
                };

                var response = this._cfClient.UpdateStreamingDistribution(request);
                this.Refresh();

                return true;
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error saving streaming distribution: " + e.Message);
                return false;
            }
        }
    }
}
