using System.Collections.Generic;
using System.IO;
using System.Net;
using Microsoft.VisualStudio.TemplateWizard;
using EnvDTE;

using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CloudFormation.TemplateWizards.WizardPages;
using Amazon.AWSToolkit.CloudFormation.TemplateWizards.WizardPages.PageControllers;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.CloudFormation.TemplateWizards
{
    public abstract class AbstractTemplateWizard : IWizard
    {
        // This method is called before opening any item that
        // has the OpenInEditor attribute.
        public virtual void BeforeOpeningFile(ProjectItem projectItem)
        {
        }

        public virtual void ProjectFinishedGenerating(Project project)
        {
        }

        // This method is only called for item templates,
        // not for project templates.
        public virtual void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
        }

        // This method is called after the project is created.
        public virtual void RunFinished()
        {
        }

        // This method is only called for item templates,
        // not for project templates.
        public virtual bool ShouldAddProjectItem(string filePath)
        {
            return true;
        }

        protected string GetInitialTemplate(Dictionary<string, object> wizardProperties)
        {
            var creationMode = (ProjectTypeController.CreationMode)wizardProperties[WizardPropertyNameConstants.propKey_CreationMode];

            string initialTemplate = null;
            switch (creationMode)
            {
                case ProjectTypeController.CreationMode.ExistingStack:
                    initialTemplate = GetExistingStacksTemplate(wizardProperties);
                    break;
                case ProjectTypeController.CreationMode.FromSample:
                    initialTemplate = GetSampleTemplate(wizardProperties);
                    break;
            }

            return initialTemplate;
        }

        protected string GetExistingStacksTemplate(Dictionary<string, object> wizardProperties)
        {
            var account = wizardProperties[WizardPropertyNameConstants.propKey_SelectedAccount] as AccountViewModel;
            var region = wizardProperties[WizardPropertyNameConstants.propKey_SelectedRegion] as ToolkitRegion;
            var existingStackName = wizardProperties[WizardPropertyNameConstants.propKey_ExistingStackName] as string;

            var client = account.CreateServiceClient<AmazonCloudFormationClient>(region);

            var response = client.GetTemplate(new GetTemplateRequest() { StackName = existingStackName });

            var templateBody = response.TemplateBody;

            // For no newlines in the existing stack which means it is an unformatted mess so lets do an initial formatting
            if (templateBody.Trim().IndexOf('\n') == -1)
                templateBody = CloudFormation.Parser.ParserUtil.PrettyFormat(templateBody);

            return templateBody;
        }

        protected string GetSampleTemplate(Dictionary<string, object> wizardProperties)
        {
            var url = wizardProperties[WizardPropertyNameConstants.propKey_SampleTemplateURL] as string;

            WebClient client = new WebClient();
            string content = null;
            using (StreamReader reader = new StreamReader(client.OpenRead(url)))
            {
                content = reader.ReadToEnd();
            }

            return content;
        }

        public abstract void RunStarted(object automationObject,
            Dictionary<string, string> replacementsDictionary,
            WizardRunKind runKind, object[] customParams);
    }
}
