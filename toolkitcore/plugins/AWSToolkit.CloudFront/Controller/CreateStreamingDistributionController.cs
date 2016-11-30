using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.CloudFront.Nodes;
using Amazon.AWSToolkit.CloudFront.DistributionWizard.PageController;
using Amazon.AWSToolkit.CloudFront.View;
using Amazon.AWSToolkit.CloudFront.Model;
using Amazon.AWSToolkit;

using Amazon.CloudFront;
using Amazon.CloudFront.Model;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

namespace Amazon.AWSToolkit.CloudFront.Controller
{
    public class CreateStreamingDistributionController : BaseDistributionConfigEditorController
    {
        CreateStreamingDistributionModel _model;
        CloudFrontRootViewModel _rootModel;
        ActionResults _results;

        public override ActionResults Execute(IViewModel model)
        {
            this._rootModel = model as CloudFrontRootViewModel;
            if (this._rootModel == null)
                return new ActionResults().WithSuccess(false);

            this._cfClient = this._rootModel.CFClient;
            this._model = new CreateStreamingDistributionModel();
            base.Initialize(this._cfClient, this._rootModel.AccountViewModel, this._model, true);

            Dictionary<string, object> seedProperties = new Dictionary<string, object>();

            IAWSWizard wizard = AWSWizardFactory.CreateStandardWizard("Amazon.AWSToolkit.CloudFront.View.CreateCloudFrontDistribution", seedProperties);
            wizard.Title = "Create CloudFront Distribution";

            IAWSWizardPageController[] defaultPages = new IAWSWizardPageController[]
            {
                new StreamingDistributionOriginPageController(this),
                new LoggingPageController(this),
                new CNAMEPageController(this),
                new PrivateSettingsPageController(this),
                new ReviewPageController(this)
            };

            wizard.RegisterPageControllers(defaultPages, 0);
            if (wizard.Run() == true)
            {
                var distribution = this.Persist();
                if (distribution != null)
                {
                    var newView = this._rootModel.AddDistribution(distribution) as CloudFrontStreamingDistributionViewModel;
                    var editController = new EditStreamingDistributionConfigController();
                    editController.Execute(newView);
                }
            } 
            
            if (this._results == null)
                return new ActionResults().WithSuccess(false);
            return this._results;
        }

        public CreateStreamingDistributionModel Model
        {
            get { return this._model; }
        }

        public StreamingDistribution Persist()
        {
            try
            {
                var config = this._model.ConvertToStreamingDistribtionConfig();
                var request = new CreateStreamingDistributionRequest() { StreamingDistributionConfig = config };

                var response = this._cfClient.CreateStreamingDistribution(request);

                this._results = new ActionResults()
                    .WithSuccess(true)
                    .WithParameter(CloudFrontActionResultsContants.PARAM_CLOUDFRONT_DISTRIBUTION, response.StreamingDistribution);

                return response.StreamingDistribution;
            }
            catch (Exception e)
            {
                this._model.CallerReference = null;
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating streaming distribution: " + e.Message);
                this._results = new ActionResults().WithSuccess(false);
                return null;
            }
        }
    }
}
