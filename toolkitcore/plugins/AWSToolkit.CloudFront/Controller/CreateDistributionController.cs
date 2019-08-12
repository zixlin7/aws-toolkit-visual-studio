using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.CloudFront.Nodes;
using Amazon.AWSToolkit.CloudFront.DistributionWizard.PageController;
using Amazon.AWSToolkit.CloudFront.Model;
using Amazon.CloudFront.Model;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

namespace Amazon.AWSToolkit.CloudFront.Controller
{
    public class CreateDistributionController : BaseDistributionConfigEditorController
    {
        CreateDistributionModel _model;
        CloudFrontRootViewModel _rootModel;
        ActionResults _results;

        public override ActionResults Execute(IViewModel model)
        {
            this._rootModel = model as CloudFrontRootViewModel;
            if (this._rootModel == null)
                return new ActionResults().WithSuccess(false);

            this._cfClient = this._rootModel.CFClient;
            this._model = new CreateDistributionModel();

            base.Initialize(this._cfClient, this._rootModel.AccountViewModel, this._model, true);


            Dictionary<string, object> seedProperties = new Dictionary<string, object>();

            IAWSWizard wizard = AWSWizardFactory.CreateStandardWizard("Amazon.AWSToolkit.CloudFront.View.CreateCloudFrontDistribution", seedProperties);
            wizard.Title = "Create CloudFront Distribution";

            IAWSWizardPageController[] defaultPages = new IAWSWizardPageController[]
            {
                new DistributionOriginPageController(this),
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
                    var newView = this._rootModel.AddDistribution(distribution) as CloudFrontDistributionViewModel;
                    EditDistributionConfigController editController = new EditDistributionConfigController();
                    editController.Execute(newView);
                }
            }

            if (this._results == null)
                return new ActionResults().WithSuccess(false);
            return this._results;
        }

        public CreateDistributionModel Model => this._model;

        public Distribution Persist()
        {
            try
            {
                var config = this._model.ConvertToDistribtionConfig();
                var request = new CreateDistributionRequest() { DistributionConfig = config };
                var response = this._cfClient.CreateDistribution(request);

                this._results = new ActionResults()
                    .WithSuccess(true)
                    .WithParameter(CloudFrontActionResultsContants.PARAM_CLOUDFRONT_DISTRIBUTION, response.Distribution);

                return response.Distribution;
            }
            catch (Exception e)
            {
                this._model.CallerReference = null;
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating distribution: " + e.Message);
                this._results = new ActionResults().WithSuccess(false);
                return null;
            }
        }

    }
}
