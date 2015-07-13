using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Microsoft.VisualStudio.TemplateWizard;
using EnvDTE;

using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CloudFormation.TemplateWizards.WizardPages;
using Amazon.AWSToolkit.CloudFormation.TemplateWizards.WizardPages.PageControllers;


namespace Amazon.AWSToolkit.CloudFormation.TemplateWizards
{
    public class NewTemplateWizardImplementation : AbstractTemplateWizard
    {
        string _initialTemplate;
        public override void RunStarted(object automationObject,
            Dictionary<string, string> replacementsDictionary,
            WizardRunKind runKind, object[] customParams)
        {
            try
            {
                IAWSWizard wizard = AWSWizardFactory.CreateStandardWizard("Amazon.AWSToolkit.CloudFormation.View.NewAWSCloudFormationTemplate", new Dictionary<string, object>());
                wizard.Title = "New AWS CloudFormation Template";

                IAWSWizardPageController[] defaultPages = new IAWSWizardPageController[]
                {
                    new ProjectTypeController("Select Template Source", "Choose the source for the template.")
                };

                wizard.RegisterPageControllers(defaultPages, 0);
                if (wizard.Run() != true)
                {
                    throw new WizardCancelledException();
                }


                this._initialTemplate = GetInitialTemplate(wizard.CollectedProperties);
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

        public override void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
            if (string.IsNullOrEmpty(this._initialTemplate))
                return;

            var property = projectItem.Properties.Item("FullPath");
            if (property == null)
                return;

            var filepath = property.Value;
            File.WriteAllText(filepath, this._initialTemplate);
        }
    }
}
