using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using log4net;

namespace Amazon.AWSToolkit.CommonUI.DeploymentWizard
{
    /// <summary>
    /// Base of the CloudFormation/Beanstalk deployment 'template' hierarchy
    /// </summary>
    public abstract class DeploymentTemplateWrapperBase
    {
        // file contains references to individual 'system' template files for use
        // with toolkit deployment features; this holds all deployment services' files,
        // not just CloudFormation
        public const string TEMPLATEMANIFEST_FILE = "CloudFormationTemplates/TemplatesManifest.xml";

        public enum Source { Local, String, ToolkitDistribution, Url }

        // callback on completion of parse to allow additional parameter manipulation
        public delegate void OnTemplateParseComplete(DeploymentTemplateWrapperBase template);

        protected ILog LOGGER;

        /// <summary>
        /// Declares the logical service owner of the wrapped template
        /// </summary>
        public abstract string ServiceOwner { get; }

        public abstract System.Windows.Media.ImageSource TemplateIcon { get; }

        /// <summary>
        /// Flag indicating this template is for VSToolkit deployment
        /// </summary>
        public bool IsVSToolkitDeployed { get; protected set; }

        /// <summary>
        /// 'One liner' header describing the template
        /// </summary>
        public string TemplateHeader { get; protected set; }

        /// <summary>
        /// Detailed description of the template
        /// </summary>
        public string TemplateDescription { get; protected set; }

        /// <summary>
        /// Local filename or S3 file reference
        /// </summary>
        public string TemplateFilename { get; protected set; }

        /// <summary>
        /// The content of the template; not set until LoadAndParse() is called
        /// </summary>
        public string TemplateContent { get; protected set; }

        string _minToolkitVersion;
        public string MinToolkitVersion
        {
            get
            {
                if (this._minToolkitVersion == null)
                    return Constants.VERSION_NUMBER;
                return this._minToolkitVersion;
            }
            protected set
            {
                this._minToolkitVersion = value;
            }
        }

        /// <summary>
        /// True if the underlying template is a 'system default' downloaded from toolkit store, false
        /// if the file came from user's local file system
        /// </summary>
        public Source TemplateSource { get; protected set; }

        public void LoadAndParse()
        {
            LoadAndParse(null);
        }

        public abstract void LoadAndParse(OnTemplateParseComplete completionCallback);

        public static DeploymentTemplateWrapperBase FromToolkitFile(string serviceOwner, string header, string description, string templateFilename, string minToolkitVersion)
        {
            return new CloudFormationTemplateWrapper(header, description, templateFilename, Source.ToolkitDistribution, minToolkitVersion);
        }

        public static DeploymentTemplateWrapperBase FromPublicS3Location(string serviceOwner, string url)
        {
            return new CloudFormationTemplateWrapper(null, null, url, Source.Url, null);
        }

        public static DeploymentTemplateWrapperBase FromLocalFile(string serviceOwner, string templateFilename)
        {
            return new CloudFormationTemplateWrapper(null, null, templateFilename, Source.Local, null);
        }

        public static DeploymentTemplateWrapperBase FromString(string serviceOwner, string templateContent)
        {
            return new CloudFormationTemplateWrapper(null, null, templateContent, Source.String, null);
        }
    }
}
