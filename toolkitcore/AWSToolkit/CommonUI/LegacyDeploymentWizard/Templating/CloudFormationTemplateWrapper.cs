using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

using ThirdParty.Json.LitJson;
using log4net;
using Amazon.AWSToolkit.PluginServices.Deployment;

namespace Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating
{
    /// <summary>
    /// Wraps the json document that describes a CloudFormation template,
    /// with helpers to assist in 'beautifying' the display of template
    /// data
    /// </summary>
    public class CloudFormationTemplateWrapper : DeploymentTemplateWrapperBase
    {
        public override string ServiceOwner => DeploymentServiceIdentifiers.CloudFormationServiceName;

        public override System.Windows.Media.ImageSource TemplateIcon
        {
            get 
            {
                // for now...
                string iconPath = "cloudformation_deployment.png";
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
        /// If true you must compute a change set before creating a stack with this template.
        /// </summary>
        /// <returns></returns>
        public bool MustCreateWithChangeSets()
        {
            return !string.IsNullOrEmpty(this.TemplateTransformation);
        }

        enum TemplateFormat { Json, Yaml }
        private static TemplateFormat DetermineTemplateFormat(string templateBody)
        {
            templateBody = templateBody.Trim();
            if (templateBody.Length > 0 && templateBody[0] == '{')
                return TemplateFormat.Json;

            return TemplateFormat.Yaml;
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
                if (DetermineTemplateFormat(this.TemplateContent) == TemplateFormat.Yaml)
                    ParseYamlFormat();
                else
                    ParseJsonFormat();

                if (completionCallback != null)
                    completionCallback(this);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error parsing template file: " + this.TemplateFilename, e);
                throw new ApplicationException("Error parsing template file: " + e.Message);
            }
        }

        private void ParseYamlFormat()
        {
            var data = new YamlDotNet.Serialization.Deserializer().Deserialize(new StringReader(this.TemplateContent)) as IDictionary<object, object>;

            if (data.ContainsKey("Description"))
                this.TemplateDescription = data["Description"].ToString();

            if (data.ContainsKey("Transform"))
                this.TemplateTransformation = data["Transform"].ToString();

            if (data.ContainsKey("Description"))
                this.TemplateDescription = data["Description"].ToString();


            this.Parameters = new Dictionary<string, TemplateParameter>();

            if (data.ContainsKey("Parameters"))
            {
                var parameters = data["Parameters"] as IDictionary<object, object>;

                foreach (var kvp in parameters)
                {
                    var parameter = kvp.Value as IDictionary<object, object>;
                    string description = null;
                    string type = null;
                    string defaultValue = null;
                    bool noEcho = false;
                    string[] allowedValues = null;

                    int? minLength = null;
                    int? maxLength = null;
                    double? minValue = null;
                    double? maxValue = null;
                    string allowedPattern = null;
                    string constraintDescription = null;

                    if (parameter.ContainsKey("Description"))
                        description = parameter["Description"].ToString();
                    if (parameter.ContainsKey("Type"))
                        type = parameter["Type"].ToString();
                    if (parameter.ContainsKey("Default"))
                        defaultValue = parameter["Default"].ToString();
                    if (parameter.ContainsKey("NoEcho"))
                        noEcho = string.Equals(parameter["NoEcho"].ToString(), "true", StringComparison.InvariantCultureIgnoreCase);
                    if (parameter.ContainsKey("AllowedValues") && parameter["AllowedValues"] is IEnumerable<object>)
                    {
                        var values = parameter["AllowedValues"] as IEnumerable<object>;
                        allowedValues = new string[values.Count()];
                        int i = 0;
                        foreach(var value in values)
                        {
                            allowedValues[i] = value.ToString();
                            i++;
                        }
                    }

                    if (parameter.ContainsKey("MinLength"))
                    {
                        int value;
                        if (int.TryParse(parameter["MinLength"].ToString(), out value))
                            minLength = value;
                    }
                    if (parameter.ContainsKey("MaxLength"))
                    {
                        int value;
                        if (int.TryParse(parameter["MaxLength"].ToString(), out value))
                            maxLength = value;
                    }
                    if (parameter.ContainsKey("MinValue"))
                    {
                        double value;
                        if (double.TryParse(parameter["MinValue"].ToString(), out value))
                            minValue = value;
                    }
                    if (parameter.ContainsKey("MaxValue"))
                    {
                        double value;
                        if (double.TryParse(parameter["MaxValue"].ToString(), out value))
                            maxValue = value;
                    }
                    if (parameter.ContainsKey("AllowedPattern"))
                        allowedPattern = parameter["AllowedPattern"].ToString();
                    if (parameter.ContainsKey("ConstraintDescription"))
                        constraintDescription = parameter["ConstraintDescription"].ToString();

                    this.Parameters.Add(kvp.Key.ToString(), new TemplateParameter(kvp.Key.ToString(), description, type, defaultValue, noEcho, allowedValues, minLength, maxLength, minValue, maxValue, allowedPattern, constraintDescription));
                }
            }
        }

        private void ParseJsonFormat()
        {
            var data = JsonMapper.ToObject(this.TemplateContent);

            var templateDescription = data["Description"];
            if (templateDescription != null && templateDescription.IsString)
                this.TemplateDescription = templateDescription.ToString();

            var templateTransform = data["Transform"];
            if (templateTransform != null && templateTransform.IsString)
                this.TemplateTransformation = templateTransform.ToString();

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
                    string[] allowedValues = null;

                    int? minLength = null;
                    int? maxLength = null;
                    double? minValue = null;
                    double? maxValue = null;
                    string allowedPattern = null;
                    string constraintDescription = null;

                    if (parameter["Description"] != null)
                        description = parameter["Description"].ToString();
                    if (parameter["Type"] != null)
                        type = parameter["Type"].ToString();
                    if (parameter["Default"] != null)
                        defaultValue = parameter["Default"].ToString();
                    if (parameter["NoEcho"] != null)
                        noEcho = string.Equals(parameter["NoEcho"].ToString(), "true", StringComparison.InvariantCultureIgnoreCase);
                    if (parameter["AllowedValues"] != null && parameter["AllowedValues"].IsArray)
                    {
                        allowedValues = new string[parameter["AllowedValues"].Count];
                        for (int i = 0; i < allowedValues.Length; i++)
                        {
                            allowedValues[i] = parameter["AllowedValues"][i].ToString();
                        }
                    }

                    if (parameter["MinLength"] != null)
                    {
                        int value;
                        if (int.TryParse(parameter["MinLength"].ToString(), out value))
                            minLength = value;
                    }
                    if (parameter["MaxLength"] != null)
                    {
                        int value;
                        if (int.TryParse(parameter["MaxLength"].ToString(), out value))
                            maxLength = value;
                    }
                    if (parameter["MinValue"] != null)
                    {
                        double value;
                        if (double.TryParse(parameter["MinValue"].ToString(), out value))
                            minValue = value;
                    }
                    if (parameter["MaxValue"] != null)
                    {
                        double value;
                        if (double.TryParse(parameter["MaxValue"].ToString(), out value))
                            maxValue = value;
                    }
                    if (parameter["AllowedPattern"] != null)
                        allowedPattern = parameter["AllowedPattern"].ToString();
                    if (parameter["ConstraintDescription"] != null)
                        constraintDescription = parameter["ConstraintDescription"].ToString();

                    this.Parameters.Add(kvp.Key, new TemplateParameter(kvp.Key, description, type, defaultValue, noEcho, allowedValues, minLength, maxLength, minValue, maxValue, allowedPattern, constraintDescription));
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
        
        public CloudFormationTemplateWrapper(string header, 
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

        CloudFormationTemplateWrapper() 
        { 
            LOGGER = LogManager.GetLogger(typeof(CloudFormationTemplateWrapper));
        }

        public static CloudFormationTemplateWrapper FromPublicS3Location(string url)
        {
            return new CloudFormationTemplateWrapper(null, null, url, Source.Url, null, null);
        }

        public static CloudFormationTemplateWrapper FromLocalFile(string templateFilename)
        {
            return new CloudFormationTemplateWrapper(null, null, templateFilename, Source.Local, null, null);
        }

        public static CloudFormationTemplateWrapper FromString(string templateContent)
        {
            return new CloudFormationTemplateWrapper(null, null, templateContent, Source.String, null, null);
        }

        public class TemplateParameter : BaseModel
        {
            const string Hidden_Prefix = "[Hidden]";

            internal TemplateParameter(string name, string description, string type, string defaultValue, bool noEcho, string[] allowedValues, 
                int? minLength, int? maxLength, double? minValue, double? maxValue, string allowedPattern, string constraintDescription)
            {
                this.Name = name;
                this.Description = description;
                this.Type = type;
                this.DefaultValue = defaultValue;
                this.NoEcho = noEcho;
                this.OverrideValue = this.DefaultValue;
                this.AllowedValues = allowedValues;

                this.MinLength = minLength;
                this.MaxLength = maxLength;
                this.MinValue = minValue;
                this.MaxValue = maxValue;
                this.AllowedPattern = allowedPattern;
                this.ConstraintDescription = constraintDescription;
            }

            public string Name
            {
                get;
            }

            string _description;
            public string Description
            {
                get => _description;
                private set
                {
                    _description = value;
                    if (value != null)
                        Hidden = value.StartsWith(Hidden_Prefix, StringComparison.InvariantCulture);
                }
            }

            public string Type
            {
                get;
            }

            public string DefaultValue
            {
                get;
            }

            public bool NoEcho
            {
                get;
            }

            public bool Hidden { get; set; }

            string _overrideValue;
            public string OverrideValue
            {
                get => this._overrideValue;
                set
                {
                    this._overrideValue = value;
                    base.NotifyPropertyChanged("OverrideValue");
                }
            }

            public string[] AllowedValues
            {
                get;
            }

            public int? MinLength
            {
                get;
            }

            public int? MaxLength
            {
                get;
            }

            public double? MinValue
            {
                get;
            }

            public double? MaxValue
            {
                get;
            }

            public string AllowedPattern
            {
                get;
            }

            public string ConstraintDescription
            {
                get;
            }

            public bool IsValid(out string errorMessage)
            {                
                // The parameter is optional
                if(this.MinLength == 0 && string.IsNullOrEmpty(this.OverrideValue))
                {
                    errorMessage = null;
                    return true;
                }


                if (string.IsNullOrEmpty(this.OverrideValue) && !string.Equals(this.DefaultValue, string.Empty))
                {
                    errorMessage = "Parameter must have value";
                    return false;
                }
                if (this.Type == "Numeric")
                {
                    double value;
                    if (!double.TryParse(this.OverrideValue, out value))
                    {
                        errorMessage = "Parameter must be a numeric";
                        return false;
                    }
                }
                if (this.MinLength.HasValue && this.OverrideValue.Length < this.MinLength.Value)
                {
                    errorMessage = string.Format("Parameter must have a length of at least {0} characters", this.MinLength.Value);
                    return false;
                }
                if (this.MaxLength.HasValue && this.MaxLength.Value < this.OverrideValue.Length)
                {
                    errorMessage = string.Format("Parameter must have a length no greater than {0} characters", this.MaxLength.Value);
                    return false;
                }
                if (this.MinValue.HasValue)
                {
                    double value;
                    if (double.TryParse(this.OverrideValue, out value))
                    {
                        if (value < this.MinValue.Value)
                        {
                            errorMessage = string.Format("Parameter's value must be at least {0}", this.MinValue.Value);
                            return false;
                        }
                    }
                    else
                    {
                        errorMessage = "Parameter must be a numeric";
                        return false;
                    }
                }
                if (this.MaxValue.HasValue)
                {
                    double value;
                    if (double.TryParse(this.OverrideValue, out value))
                    {
                        if (this.MaxValue.Value < value)
                        {
                            errorMessage = string.Format("Parameter's value must be no greater than {0}", this.MaxValue.Value);
                            return false;
                        }
                    }
                    else
                    {
                        errorMessage = "Parameter must be a numeric";
                        return false;
                    }
                }
                if (!string.IsNullOrEmpty(this.AllowedPattern))
                {
                    var match = Regex.Match(this.OverrideValue, this.AllowedPattern);
                    if (!match.Success || match.Length != this.OverrideValue.Length)
                    {
                        errorMessage = string.Format("Parameter does not match the specified pattern in template: {0}", this.AllowedPattern);
                        return false;
                    }
                }

                errorMessage = null;
                return true;
            }
        }
    }
}
