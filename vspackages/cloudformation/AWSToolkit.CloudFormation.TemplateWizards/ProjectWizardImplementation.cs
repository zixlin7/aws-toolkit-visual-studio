using System;
using System.Collections.Generic;
using System.IO;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Microsoft.VisualStudio.TemplateWizard;
using Amazon.AWSToolkit.CloudFormation.TemplateWizards.WizardPages.PageControllers;
using Amazon.AWSToolkit.MobileAnalytics;

namespace Amazon.AWSToolkit.CloudFormation.TemplateWizards
{
    public class ProjectWizardImplementation : AbstractTemplateWizard
    {


        public override void RunStarted(object automationObject,
            Dictionary<string, string> replacementsDictionary,
            WizardRunKind runKind, object[] customParams)
        {
            try
            {
                IAWSWizard wizard = AWSWizardFactory.CreateStandardWizard("Amazon.AWSToolkit.CloudFormation.View.NewAWSCloudFormationProject", new Dictionary<string, object>());
                wizard.Title = "New AWS CloudFormation Project";

                IAWSWizardPageController[] defaultPages = new IAWSWizardPageController[]
                {
                    new ProjectTypeController("Select Project Source", "Choose the source for the template created with the new project.")
                };

                wizard.RegisterPageControllers(defaultPages, 0);
                if (wizard.Run() != true)
                {
                    throw new WizardCancelledException();
                }

                ToolkitEvent evnt = new ToolkitEvent();
                var creationMode = (ProjectTypeController.CreationMode)wizard.CollectedProperties[WizardPages.WizardPropertyNameConstants.propKey_CreationMode];
                if (creationMode == ProjectTypeController.CreationMode.FromSample)
                {
                    evnt.AddProperty(AttributeKeys.CloudFormationNewProject, 
                        string.Format("{0}/{1}", creationMode.ToString(), 
                            wizard.CollectedProperties[WizardPages.WizardPropertyNameConstants.propKey_SampleTemplateURL] as string));
                }
                else
                {
                    evnt.AddProperty(AttributeKeys.CloudFormationNewProject, creationMode.ToString());
                }
                SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);

                string initialTemplate = GetInitialTemplate(wizard.CollectedProperties);

                if (!string.IsNullOrEmpty(initialTemplate))
                {
                    var projectFolder = replacementsDictionary["$destinationdirectory$"] as string;
                    var filepath = Path.Combine(projectFolder, "cloudformation.template");
                    File.WriteAllText(filepath, initialTemplate);
                }
            }
            catch (WizardCancelledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError(ex.ToString());
            }
        }
    }
}
