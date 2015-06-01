using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using ThirdParty.Json.LitJson;
using log4net;
using Amazon.AWSToolkit.PluginServices.Deployment;

namespace Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating
{
    /// <summary>
    /// Pseudo-template (no content) used to represent to a Beanstalk endpoint
    /// </summary>
    public class BeanstalkTemplateWrapper : DeploymentTemplateWrapperBase
    {
        public override string ServiceOwner { get { return DeploymentServiceIdentifiers.BeanstalkServiceName; } }

        public override System.Windows.Media.ImageSource TemplateIcon
        {
            get
            {
                // for now...
                string iconPath = "beanstalk_deployment.png";
                var icon = IconHelper.GetIcon(this.GetType().Assembly, iconPath);
                return icon.Source;
            }
        }

        /// <summary>
        /// Null operation, as there is no content to parse (currently)
        /// </summary>
        public override void LoadAndParse(OnTemplateParseComplete completionCallback)
        {
            if (completionCallback != null)
                completionCallback(this);
        }

        public BeanstalkTemplateWrapper(string header, 
                                        string description, 
                                        string templateFilename, 
                                        Source templateSource, 
                                        string minToolkitVersion,
                                        IEnumerable<string> supportedFrameworkVersions)
            : this()
        {
            this.TemplateHeader = header;
            this.TemplateDescription = description;
            this.TemplateFilename = templateFilename;
            this.TemplateSource = templateSource;
            this.TemplateContent = string.Empty;
            this.MinToolkitVersion = minToolkitVersion;
            this.SupportedFrameworks = new List<string>(supportedFrameworkVersions == null 
                                                            ? DeploymentTemplateWrapperBase.ALL_FRAMEWORKS 
                                                            : supportedFrameworkVersions
                                                       );
        }

        BeanstalkTemplateWrapper()
        {
            LOGGER = LogManager.GetLogger(typeof(BeanstalkTemplateWrapper));
        }
    }
}
