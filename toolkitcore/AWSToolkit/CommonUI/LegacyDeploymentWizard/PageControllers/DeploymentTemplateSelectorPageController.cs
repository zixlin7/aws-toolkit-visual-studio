using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.PluginServices.Deployment;

namespace Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.PageControllers
{
    public class DeploymentTemplateSelectorPageController
    {
        public static DeploymentTemplateWrapperBase ConvertToDeploymentTemplate(XElement template)
        {
            string serviceOwnerName = DeploymentServiceIdentifiers.CloudFormationServiceName;
            XAttribute serviceOwner = template.Attribute("serviceOwner");
            if (serviceOwner != null)
                serviceOwnerName = serviceOwner.Value;

            string header = template.Elements("header").ElementAt<XElement>(0).Value;
            string description = template.Elements("description").ElementAt<XElement>(0).Value;
            string file = template.Elements("templatefile").ElementAt<XElement>(0).Value;

            string minToolkitVersion = null;
            var element = template.Elements("min-toolkit-version");
            if (element != null && element.Count() > 0)
                minToolkitVersion = element.ElementAt<XElement>(0).Value;

            IEnumerable<string> supportedFrameworkVersions = null;
            element = template.Elements("frameworks");
            if (element != null && element.Count() > 0)
            {
                var versionsAttr = element.ElementAt<XElement>(0).Attribute("supportedVersions");
                if (versionsAttr != null)
                {
                    string fxVersions = versionsAttr.Value;
                    supportedFrameworkVersions = fxVersions.Split('|');
                }
            }

            var deploymentTemplate = DeploymentTemplateWrapperBase.FromToolkitFile(serviceOwnerName, header,
                description, file, minToolkitVersion, supportedFrameworkVersions);

            return deploymentTemplate;
        }
    }
}
