using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CloudFormation.WizardPages.PageControllers;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.PluginServices.Deployment;
using System;
using System.Collections.Generic;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Telemetry.Model;

using Amazon.AWSToolkit.CloudFormation.Model;
using Amazon.AWSToolkit.CloudFormation.Util;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Telemetry;
using log4net;

namespace Amazon.AWSToolkit.CloudFormation.Controllers
{
    public class DeployTemplateController : BaseStackController
    {
        private static readonly BaseMetricSource _deployMetricSource = MetricSources.CloudFormationMetricSource.Project;
        private static readonly ILog _logger = LogManager.GetLogger(typeof(DeployTemplateController));

        public DeployTemplateController(ToolkitContext toolkitContext) : base(toolkitContext, _deployMetricSource)
        {
        }


        public DeployedTemplateData Execute(string templatePath, IDictionary<string, object> seedProperties,
            string templateName)
        {
            ActionResults result = null;
            IAWSWizard wizard = null;

            void Invoke() => result = TryCreateWizard(templatePath, seedProperties, templateName, out wizard);

            void Record(ITelemetryLogger telemetryLogger, double duration)
            {
                RecordWizardMetric(result, duration, wizard);
            }

            _toolkitContext.TelemetryLogger.TimeAndRecord(Invoke, Record);

            //if user cancelled from the wizard return null, else deploy template
            if (!result.Success)
            {
                return null;
            }

            return DeployTemplate(wizard);
        }


        private ActionResults TryCreateWizard(string templatePath, IDictionary<string, object> seedProperties, string templateName, out IAWSWizard wizard)
        {
            seedProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_TemplateServiceOwner] = DeploymentServiceIdentifiers.CloudFormationServiceName;
            seedProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplate] = CloudFormationTemplateWrapper.FromLocalFile(templatePath);
            seedProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplateName] = templateName;

            try
            {
                ValidateTemplate(templatePath);
            }
            catch (Exception ex)
            {
                wizard = null;
                return ActionResults.CreateFailed(ex);
            }

            wizard = AWSWizardFactory.CreateStandardWizard("Amazon.AWSToolkit.CloudFormation.View.DeployTemplate", seedProperties);
            wizard.Title = "Deploy Template";

            var defaultPages = new IAWSWizardPageController[]
            {
                new SelectStackPageController(_toolkitContext),
                new TemplateParametersController(),
                new CreateStackReviewPageController()
            };

            wizard.RegisterPageControllers(defaultPages, 0);

            var success = wizard.Run();
            return success ? new ActionResults().WithSuccess(true) : ActionResults.CreateCancelled();
        }

        private void ValidateTemplate(string templatePath)
        {
            try
            {
                var wrapper = CloudFormationTemplateWrapper.FromLocalFile(templatePath);
                wrapper.LoadAndParse();
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to validate CloudFormation Template", ex);
                _toolkitContext.ToolkitHost.ShowError("Error validating CloudFormation template", ex.Message);
                throw new TemplateToolkitException(ex.Message, TemplateToolkitException.TemplateErrorCode.InvalidFormat,
                    ex);
            }
        }

        private DeployedTemplateData DeployTemplate(IAWSWizard wizard)
        {
            DeployedTemplateData persistableData = null;
            var account =
                wizard.CollectedProperties[
                        CloudFormationDeploymentWizardProperties.SelectStackProperties.propkey_SelectedAccount] as
                    AccountViewModel;
            var region =
                wizard.CollectedProperties[
                        CloudFormationDeploymentWizardProperties.SelectStackProperties.propkey_SelectedRegion] as
                    ToolkitRegion;

            var createMode =
                Convert.ToBoolean(wizard.CollectedProperties[
                    CloudFormationDeploymentWizardProperties.SelectStackProperties.propkey_CreateStackMode]);

            ActionResults results;
            if (createMode)
            {
                results = base.CreateStack(account, region, wizard.CollectedProperties);
            }
            else
            {
                results = base.UpdateStack(account, region, wizard.CollectedProperties);
            }

            if (results.Success)
            {
                var stackName =
                    wizard.CollectedProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] as
                        string;
                Util.CloudFormationUtil.OpenStack(account, region, stackName);
                persistableData = GatherPersistableDeploymentData(account,
                    region,
                    createMode
                        ? DeployedTemplateData.DeploymentType.newStack
                        : DeployedTemplateData.DeploymentType.updateStack,
                    wizard.CollectedProperties);
            }

            return persistableData;
        }

        private void RecordWizardMetric(ActionResults result, double duration, IAWSWizard wizard)
        {
            var account = GetAccount(wizard);
            var region = GetRegion(wizard);

            var connectionSettings = new AwsConnectionSettings(account?.Identifier, region);
            _toolkitContext.RecordCloudFormationPublishWizard(result, duration, connectionSettings, _deployMetricSource);
        }

        private static ToolkitRegion GetRegion(IAWSWizard wizard)
        {
            return wizard?.CollectedProperties[
                CloudFormationDeploymentWizardProperties.SelectStackProperties.propkey_SelectedRegion] as ToolkitRegion;
        }

        private static AccountViewModel GetAccount(IAWSWizard wizard)
        {
            return wizard?.CollectedProperties[
                CloudFormationDeploymentWizardProperties.SelectStackProperties.propkey_SelectedAccount] as AccountViewModel;
        }
    }
}
