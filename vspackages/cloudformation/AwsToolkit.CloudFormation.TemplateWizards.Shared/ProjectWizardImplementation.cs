using System;
using System.Collections.Generic;
using System.IO;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Microsoft.VisualStudio.TemplateWizard;
using Amazon.AWSToolkit.CloudFormation.TemplateWizards.WizardPages.PageControllers;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;

namespace Amazon.AWSToolkit.CloudFormation.TemplateWizards
{
    public class ProjectWizardImplementation : AbstractTemplateWizard
    {
        public override void RunStarted(object automationObject,
            Dictionary<string, string> replacementsDictionary,
            WizardRunKind runKind, object[] customParams)
        {
            var result = Result.Failed;
            string blueprintName = null;

            try
            {
                IAWSWizard wizard = AWSWizardFactory.CreateStandardWizard(
                    "Amazon.AWSToolkit.CloudFormation.View.NewAWSCloudFormationProject",
                    new Dictionary<string, object>());
                wizard.Title = "New AWS CloudFormation Project";

                IAWSWizardPageController[] defaultPages = new IAWSWizardPageController[]
                {
                    new ProjectTypeController("Select Project Source",
                        "Choose the source for the template created with the new project.")
                };

                wizard.RegisterPageControllers(defaultPages, 0);
                if (wizard.Run() != true)
                {
                    throw new WizardCancelledException();
                }

                var creationMode =
                    (ProjectTypeController.CreationMode) wizard.CollectedProperties[
                        WizardPages.WizardPropertyNameConstants.propKey_CreationMode];

                blueprintName = (creationMode == ProjectTypeController.CreationMode.FromSample)
                    ? $"{creationMode}/{wizard.CollectedProperties[WizardPages.WizardPropertyNameConstants.propKey_SampleTemplateURL] as string}"
                    : creationMode.ToString();

                var initialTemplate = GetInitialTemplate(wizard.CollectedProperties);

                if (!string.IsNullOrEmpty(initialTemplate))
                {
                    var projectFolder = replacementsDictionary["$destinationdirectory$"] as string;
                    var filepath = Path.Combine(projectFolder, "cloudformation.template");
                    File.WriteAllText(filepath, initialTemplate);
                }

                result = Result.Succeeded;
            }
            catch (WizardCancelledException)
            {
                result = Result.Cancelled;
                throw;
            }
            catch (Exception ex)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError(ex.ToString());
            }
            finally
            {
                RecordCloudFormationCreateProjectMetric(result, blueprintName);
            }
        }

        private static void RecordCloudFormationCreateProjectMetric(Result result, string blueprintName)
        {
            ToolkitFactory.Instance.TelemetryLogger.RecordCloudformationCreateProject(new CloudformationCreateProject
            {
                AwsAccount = ToolkitFactory.Instance.AwsConnectionManager?.ActiveAccountId ?? MetadataValue.NotSet,
                AwsRegion = ToolkitFactory.Instance.AwsConnectionManager?.ActiveRegion?.Id ?? MetadataValue.NotSet,
                Result = result,
                TemplateName = blueprintName
            });
        }
    }
}
