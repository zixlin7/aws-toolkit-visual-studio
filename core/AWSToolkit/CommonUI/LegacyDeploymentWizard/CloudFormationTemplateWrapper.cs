using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using ThirdParty.Json.LitJson;
using log4net;

namespace Amazon.AWSToolkit.CommonUI.DeploymentWizard
{
    /// <summary>
    /// Wraps the json document that describes a CloudFormation template,
    /// with helpers to assist in 'beautifying' the display of template
    /// data
    /// </summary>
    public class CloudFormationTemplateWrapper : DeploymentTemplateWrapperBase
    {
        public override string ServiceOwner { get { return "CloudFormation"; } }

        public override System.Windows.Media.ImageSource TemplateIcon
        {
            get 
            {
                // for now...
                string iconPath = "Amazon.AWSToolkit.CloudFormation.Resources.EmbeddedImages.windows-logo.png";
                var icon = IconHelper.GetIcon(this.GetType().Assembly, iconPath);
                return icon.Source;
            }
        }

        public Dictionary<string, TemplateParameter> Parameters { get; private set; }

        public HashSet<string> OutputParameterNames { get; private set; }

        /// <summary>
        /// True if at least one of the parameters in the template is not marked as hidden
        /// </summary>
        public bool ContainsUserVisibleParameters
        {
            get
            {
                LoadAndParse();
                foreach (TemplateParameter tp in Parameters.Values)
                {
                    if (!tp.Hidden)
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Downloads and parses the referenced template file, invoking the callback on
        /// completion.
        /// </summary>
        public override void LoadAndParse(OnTemplateParseComplete completionCallback)
        {
            if (!string.IsNullOrEmpty(TemplateContent))
                return;

            try
            {
                TextReader stream;
                if (this.TemplateSource == Source.ToolkitDistribution)
                    stream = new StreamReader(S3FileFetcher.Instance.OpenFileStream(this.TemplateFilename.TrimStart('/'),
                                              S3FileFetcher.CacheMode.PerInstance));
                else if (this.TemplateSource == Source.Url)
                {
                    HttpWebRequest httpRequest = WebRequest.Create(this.TemplateFilename) as HttpWebRequest;
                    HttpWebResponse response = httpRequest.GetResponse() as HttpWebResponse;
                    stream = new StreamReader(response.GetResponseStream());
                }
                else if (this.TemplateSource == Source.String)
                    stream = new StringReader(this.TemplateFilename);
                else
                    stream = new StreamReader(TemplateFilename);

                this.TemplateContent = stream.ReadToEnd();
                stream.Close();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error opening stream to template: " + this.TemplateFilename, e);
                throw new ApplicationException("Failed to load template: " + e.Message);
            }

            try
            {
                var data = JsonMapper.ToObject(this.TemplateContent);

                var templateDescription = data["Description"];
                if (templateDescription != null && templateDescription.IsString)
                    this.TemplateDescription = templateDescription.ToString();

                var parameters = data["Parameters"];
                this.Parameters = new Dictionary<string, TemplateParameter>();
                if (parameters != null)
                {
                    foreach (KeyValuePair<string, JsonData> kvp in parameters)
                    {
                        var parameter = kvp.Value;
                        string description = null;
                        string type = null;
                        string defaultValue = null;
                        bool noEcho = false;

                        if (parameter["Description"] != null)
                            description = parameter["Description"].ToString();
                        if (parameter["Type"] != null)
                            type = parameter["Type"].ToString();
                        if (parameter["Default"] != null)
                            defaultValue = parameter["Default"].ToString();
                        if (parameter["NoEcho"] != null)
                            noEcho = Convert.ToBoolean(parameter["NoEcho"].ToString());

                        this.Parameters.Add(kvp.Key, new TemplateParameter(kvp.Key, description, type, defaultValue, noEcho));
                    }
                }

                this.OutputParameterNames = new HashSet<string>();
                var outputs = data["Outputs"];
                if (outputs != null)
                {
                    foreach (KeyValuePair<string, JsonData> kvp in outputs)
                    {
                        if (!this.OutputParameterNames.Contains(kvp.Key))
                            this.OutputParameterNames.Add(kvp.Key);
                    }
                }

                checkIfVSDeployed(data);

                if (completionCallback != null)
                    completionCallback(this);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error parsing json document: " + this.TemplateFilename, e);
                throw new ApplicationException("Error parsing template file: " + e.Message);
            }
        }

        private void checkIfVSDeployed(JsonData data)
        {
            var outputs = data["Outputs"];
            if (outputs != null)
            {
                var vsToolkitDeployed = outputs["VSToolkitDeployed"];
                if (vsToolkitDeployed != null)
                {
                    var value = vsToolkitDeployed["Value"];
                    bool parsed;
                    if (value != null && value.IsString && bool.TryParse(value.ToString(), out parsed))
                    {
                        this.IsVSToolkitDeployed = parsed;
                    }
                }
            }
        }
        
        public CloudFormationTemplateWrapper(string header, string description, string templateFilename, Source templateSource, string minToolkitVersion)
            : this()
        {
            this.TemplateHeader = header;
            this.TemplateDescription = description;
            this.TemplateFilename = templateFilename;
            this.TemplateSource = templateSource;
            this.TemplateContent = string.Empty;
            this.MinToolkitVersion = minToolkitVersion;
        }

        CloudFormationTemplateWrapper() 
        { 
            LOGGER = LogManager.GetLogger(typeof(CloudFormationTemplateWrapper));
        }

        public class TemplateParameter : BaseModel
        {
            const string Hidden_Prefix = "[Hidden]";

            internal TemplateParameter(string name, string description, string type, string defaultValue, bool noEcho)
            {
                this.Name = name;
                this.Description = description;
                this.Type = type;
                this.DefaultValue = defaultValue;
                this.OverrideValue = this.DefaultValue;
            }

            public string Name
            {
                get;
                private set;
            }

            string _description;
            public string Description
            {
                get { return _description; }
                private set
                {
                    _description = value;
                    Hidden = value.StartsWith(Hidden_Prefix, StringComparison.InvariantCulture);
                }
            }

            public string Type
            {
                get;
                private set;
            }

            public string DefaultValue
            {
                get;
                private set;
            }

            public bool NoEcho
            {
                get;
                private set;
            }

            public bool Hidden { get; set; }

            string _overrideValue;
            public string OverrideValue
            {
                get { return this._overrideValue; }
                set
                {
                    this._overrideValue = value;
                    base.NotifyPropertyChanged("OverrideValue");
                }
            }

        }
    }
}
