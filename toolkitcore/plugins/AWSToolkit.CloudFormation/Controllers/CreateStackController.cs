using System.Collections.Generic;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.CloudFormation.Nodes;
using Amazon.AWSToolkit.CloudFormation.WizardPages.PageControllers;
using Amazon.AWSToolkit.PluginServices.Deployment;

namespace Amazon.AWSToolkit.CloudFormation.Controllers
{
    public class CreateStackController : BaseStackController
    {
        CloudFormationRootViewModel _rootModel;
        public override ActionResults Execute(IViewModel model)
        {
            this._rootModel = model as CloudFormationRootViewModel;
            if (this._rootModel == null)
                return new ActionResults().WithSuccess(false);

            Dictionary<string, object> seedProperties = new Dictionary<string, object>();
            seedProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_TemplateServiceOwner] = DeploymentServiceIdentifiers.CloudFormationServiceName;

            IAWSWizard wizard = AWSWizardFactory.CreateStandardWizard("Amazon.AWSToolkit.CloudFormation.View.CreateStack", seedProperties);
            wizard.Title = "Create Stack";

            IAWSWizardPageController[] defaultPages = new IAWSWizardPageController[]
            {
                new SelectTemplateController(this._rootModel),
                new TemplateParametersController(),
                new CreateStackReviewPageController()
            };

            wizard.RegisterPageControllers(defaultPages, 0);
            if (wizard.Run() == true)
            {
                var properties = wizard.CollectedProperties;
                var wrapper = properties[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplate] as CloudFormationTemplateWrapper;
                if (wrapper.TemplateSource == DeploymentTemplateWrapperBase.Source.Local || wrapper.TemplateSource == DeploymentTemplateWrapperBase.Source.String)
                {
                    properties[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplateName] =
                        properties[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] as string;
                }

                var region = RegionEndPointsManager.GetInstance().GetRegion(this._rootModel.CurrentEndPoint.RegionSystemName);
                return base.CreateStack(this._rootModel.AccountViewModel, region, properties);
            }

            return new ActionResults().WithSuccess(false);
        }
    }
}
