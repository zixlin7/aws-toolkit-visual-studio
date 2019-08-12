using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.CloudFormation.WizardPages.PageControllers;

using Amazon.CloudFormation;
using Amazon.AWSToolkit.PluginServices.Deployment;

namespace Amazon.AWSToolkit.CloudFormation.Controllers
{
    public class DeployTemplateController : BaseStackController
    {
        public DeployedTemplateData Execute(string templatePath, IDictionary<string, object> seedProperties, string templateName)
        {
            DeployedTemplateData persistableData = null;

            seedProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_TemplateServiceOwner] = DeploymentServiceIdentifiers.CloudFormationServiceName;
            seedProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplate] = CloudFormationTemplateWrapper.FromLocalFile(templatePath);
            seedProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplateName] = templateName;

            IAWSWizard wizard = AWSWizardFactory.CreateStandardWizard("Amazon.AWSToolkit.CloudFormation.View.DeployTemplate", seedProperties);
            wizard.Title = "Deploy Template";

            IAWSWizardPageController[] defaultPages = new IAWSWizardPageController[]
            {
                new SelectStackPageController(),
                new TemplateParametersController(),
                new CreateStackReviewPageController()
            };

            wizard.RegisterPageControllers(defaultPages, 0);
            if (wizard.Run() == true)
            {
                var account = wizard.CollectedProperties[CloudFormationDeploymentWizardProperties.SelectStackProperties.propkey_SelectedAccount] as AccountViewModel;
                var region = wizard.CollectedProperties[CloudFormationDeploymentWizardProperties.SelectStackProperties.propkey_SelectedRegion] as RegionEndPointsManager.RegionEndPoints;

                var createMode = Convert.ToBoolean(wizard.CollectedProperties[CloudFormationDeploymentWizardProperties.SelectStackProperties.propkey_CreateStackMode]);
                var client = account.CreateServiceClient<AmazonCloudFormationClient>(region);

                ActionResults results;
                if (createMode)
                    results = base.CreateStack(account, region, wizard.CollectedProperties);
                else
                    results = base.UpdateStack(account, region, wizard.CollectedProperties);

                if (results.Success)
                {
                    var stackName = wizard.CollectedProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] as string;
                    Util.CloudFormationUtil.OpenStack(account, region, stackName);
                    persistableData = GatherPersistableDeploymentData(account, 
                                                                      region, 
                                                                      createMode ? DeployedTemplateData.DeploymentType.newStack
                                                                                 : DeployedTemplateData.DeploymentType.updateStack,
                                                                      wizard.CollectedProperties);
                }
            }

            return persistableData;
        }
    }
}
