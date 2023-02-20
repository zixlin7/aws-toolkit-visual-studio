using Amazon.AWSToolkit.CloudFormation.Nodes;
using Amazon.AWSToolkit.CloudFormation.WizardPages.PageControllers;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.PluginServices.Deployment;
using Amazon.AWSToolkit.Telemetry.Model;

using System.Collections.Generic;

using Amazon.AWSToolkit.CloudFormation.Util;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Telemetry;

namespace Amazon.AWSToolkit.CloudFormation.Controllers
{
    public class CreateStackController : BaseStackController
    {
        private static readonly BaseMetricSource _deployMetricSource = CommonMetricSources.AwsExplorerMetricSource.ServiceNode;
        private CloudFormationRootViewModel _rootModel;

        public CreateStackController(ToolkitContext toolkitContext) : base(toolkitContext, _deployMetricSource)
        {
        }

        public override ActionResults Execute(IViewModel model)
        { 
            ActionResults result = null;
            IAWSWizard wizard = null;

            void Invoke() => result = TryCreateWizard(model, out wizard);

            void Record(ITelemetryLogger telemetryLogger, double duration)
            {
                RecordWizardMetric(result, duration);
            }

            _toolkitContext.TelemetryLogger.TimeAndRecord(Invoke, Record);

            //if user cancelled from the wizard return it's result, else perform create stack operation
            if (!result.Success)
            {
                return result;
            }
            return CreateStack(wizard);
        }


        private ActionResults TryCreateWizard(IViewModel model, out IAWSWizard wizard)
        {
            wizard = null;
            _rootModel = model as CloudFormationRootViewModel;
            if (_rootModel == null)
            {
                return ActionResults.CreateFailed(new ToolkitException("Unable to find CloudFormation stack data",
                    ToolkitException.CommonErrorCode.InternalMissingServiceState));
            }

            var seedProperties = new Dictionary<string, object>();
            seedProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_TemplateServiceOwner] = DeploymentServiceIdentifiers.CloudFormationServiceName;

            wizard = AWSWizardFactory.CreateStandardWizard("Amazon.AWSToolkit.CloudFormation.View.CreateStack", seedProperties);
            wizard.Title = "Create Stack";

            var defaultPages = new IAWSWizardPageController[]
            {
                new SelectTemplateController(_rootModel),
                new TemplateParametersController(),
                new CreateStackReviewPageController()
            };

            wizard.RegisterPageControllers(defaultPages, 0);
            if (!wizard.Run())
            {
                return ActionResults.CreateCancelled();
            }

            return new ActionResults().WithSuccess(true);
        }

        private ActionResults CreateStack(IAWSWizard wizard)
        {
            var properties = wizard.CollectedProperties;
            var wrapper = properties[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplate] as CloudFormationTemplateWrapper;
            if (wrapper.TemplateSource == DeploymentTemplateWrapperBase.Source.Local || wrapper.TemplateSource == DeploymentTemplateWrapperBase.Source.String)
            {
                properties[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplateName] =
                    properties[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] as string;
            }
            return base.CreateStack(_rootModel.AccountViewModel, _rootModel.Region, properties);
        }

        private void RecordWizardMetric(ActionResults result, double duration)
        {
            var connectionSettings = _rootModel?.AwsConnectionSettings;
            _toolkitContext.RecordCloudFormationPublishWizard(result, duration, connectionSettings, _deployMetricSource);
        }
    }
}
