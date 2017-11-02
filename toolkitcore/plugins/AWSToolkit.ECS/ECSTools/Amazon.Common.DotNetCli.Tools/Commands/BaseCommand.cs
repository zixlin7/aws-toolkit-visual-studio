using Amazon.Common.DotNetCli.Tools.Options;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ThirdParty.Json.LitJson;

using Amazon.IdentityManagement;
using Amazon.S3;

namespace Amazon.Common.DotNetCli.Tools.Commands
{
    public abstract class BaseCommand<TDefaultConfig> : ICommand
        where TDefaultConfig : DefaultConfigFile, new()
    {
        public ToolsException LastToolsException { get; protected set; }

        public string[] OriginalCommandLineArguments { get; private set; }

        public BaseCommand(IToolLogger logger, string workingDirectory)
        {
            this.Logger = logger;
            this.WorkingDirectory = workingDirectory;
        }

        public BaseCommand(IToolLogger logger, string workingDirectory, IList<CommandOption> possibleOptions, string[] args)
            : this(logger, workingDirectory)
        {
            this.OriginalCommandLineArguments = args ?? new string[0];
            var values = CommandLineParser.ParseArguments(possibleOptions, args);
            ParseCommandArguments(values);
        }

        public abstract Task<bool> ExecuteAsync();

        protected abstract string ToolName
        {
            get;
        }

        /// <summary>
        /// The common options used by every command
        /// </summary>
        protected static readonly IList<CommandOption> CommonOptions = new List<CommandOption>
        {
            CommonDefinedCommandOptions.ARGUMENT_DISABLE_INTERACTIVE,
            CommonDefinedCommandOptions.ARGUMENT_AWS_REGION,
            CommonDefinedCommandOptions.ARGUMENT_AWS_PROFILE,
            CommonDefinedCommandOptions.ARGUMENT_AWS_PROFILE_LOCATION,
            CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION,
            CommonDefinedCommandOptions.ARGUMENT_CONFIG_FILE
        };

        /// <summary>
        /// Used to combine the command specific command options with the common options.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        protected static IList<CommandOption> BuildLineOptions(List<CommandOption> options)
        {
            var list = new List<CommandOption>();
            list.AddRange(CommonOptions);
            list.AddRange(options);

            return list;
        }

        /// <summary>
        /// Parse the CommandOptions into the Properties on the command.
        /// </summary>
        /// <param name="values"></param>
        protected virtual void ParseCommandArguments(CommandOptions values)
        {
            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_DISABLE_INTERACTIVE.Switch)) != null)
                this.DisableInteractive = tuple.Item2.BoolValue;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_AWS_PROFILE.Switch)) != null)
                this.Profile = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_AWS_PROFILE_LOCATION.Switch)) != null)
                this.ProfileLocation = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_AWS_REGION.Switch)) != null)
                this.Region = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION.Switch)) != null)
                this.ProjectLocation = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_CONFIG_FILE.Switch)) != null)
                this.ConfigFile = tuple.Item2.StringValue;

            if (string.IsNullOrEmpty(this.ConfigFile))
                this.ConfigFile = new TDefaultConfig().DefaultConfigFileName;
        }




        TDefaultConfig _defaultConfig;
        public TDefaultConfig DefaultConfig
        {
            get
            {
                if (this._defaultConfig == null)
                {
                    var config = new TDefaultConfig();
                    config.LoadDefaults(Utilities.DetermineProjectLocation(this.WorkingDirectory, this.ProjectLocation), this.ConfigFile);
                    this._defaultConfig = config;
                }
                return this._defaultConfig;
            }
        }



        public string Region { get; set; }
        public string Profile { get; set; }
        public string ProfileLocation { get; set; }
        public AWSCredentials Credentials { get; set; }
        public string ProjectLocation { get; set; }
        public string ConfigFile { get; set; }

        /// <summary>
        /// Disable all Console.Read operations to make sure the command is never blocked waiting for input. This is 
        /// used by the AWS Visual Studio Toolkit to make sure it never gets blocked.
        /// </summary>
        public bool DisableInteractive { get; set; } = false;

        protected AWSCredentials DetermineAWSCredentials()
        {
            AWSCredentials credentials;
            if (this.Credentials != null)
            {
                credentials = this.Credentials;
            }
            else
            {
                var profile = this.Profile;
                if (string.IsNullOrEmpty(profile))
                {
                    profile = DefaultConfig[CommonDefinedCommandOptions.ARGUMENT_AWS_PROFILE.Switch] as string;
                }

                if (!string.IsNullOrEmpty(profile))
                {
                    var chain = new CredentialProfileStoreChain(this.ProfileLocation);
                    if (!chain.TryGetAWSCredentials(profile, out credentials))
                    {
                        throw new ToolsException($"Credentials for profile {profile} cannot be found", ToolsException.CommonErrorCode.ProfileNotFound);
                    }
                }
                else
                {
                    credentials = FallbackCredentialsFactory.GetCredentials();
                }
            }

            return credentials;
        }

        protected RegionEndpoint DetermineAWSRegion()
        {
            // See if a region has been set but don't prompt if not set.
            var regionName = this.GetStringValueOrDefault(this.Region, CommonDefinedCommandOptions.ARGUMENT_AWS_REGION, false);
            if (!string.IsNullOrWhiteSpace(regionName))
            {
                return RegionEndpoint.GetBySystemName(regionName);
            }

            // See if we can find a region using the region fallback logic.
            if (string.IsNullOrWhiteSpace(regionName))
            {
                var region = FallbackRegionFactory.GetRegionEndpoint(true);
                if (region != null)
                {
                    return region;
                }
            }

            // If we still don't have a region prompt the user for a region.
            regionName = this.GetStringValueOrDefault(this.Region, CommonDefinedCommandOptions.ARGUMENT_AWS_REGION, true);
            if (!string.IsNullOrWhiteSpace(regionName))
            {
                return RegionEndpoint.GetBySystemName(regionName);
            }

            throw new ToolsException("Can not determine AWS region. Either configure a default region or use the --region option.", ToolsException.CommonErrorCode.RegionNotConfigured);
        }

        /// <summary>
        /// Gets the value for the CommandOption either through the property value which means the 
        /// user explicity set the value or through defaults for the project. 
        /// 
        /// If no value is found in either the property value or the defaults and the value
        /// is required the user will be prompted for the value if we are running in interactive
        /// mode.
        /// </summary>
        /// <param name="propertyValue"></param>
        /// <param name="option"></param>
        /// <param name="required"></param>
        /// <returns></returns>
        public string GetStringValueOrDefault(string propertyValue, CommandOption option, bool required)
        {
            if (!string.IsNullOrEmpty(propertyValue))
            {
                return propertyValue;
            }
            else if (!string.IsNullOrEmpty(DefaultConfig[option.Switch] as string))
            {
                var configDefault = DefaultConfig[option.Switch] as string;
                return configDefault;
            }
            else if (required && !this.DisableInteractive)
            {
                return PromptForValue(option);
            }
            else if (_cachedRequestedValues.ContainsKey(option))
            {
                var cachedValue = _cachedRequestedValues[option];
                return cachedValue;
            }

            if (required)
            {
                throw new ToolsException($"Missing required parameter: {option.Switch}", ToolsException.CommonErrorCode.MissingRequiredParameter);
            }

            return null;
        }

        /// <summary>
        /// String[] version of GetStringValueOrDefault
        /// </summary>
        /// <param name="propertyValue"></param>
        /// <param name="option"></param>
        /// <param name="required"></param>
        /// <returns></returns>
        public string[] GetStringValuesOrDefault(string[] propertyValue, CommandOption option, bool required)
        {
            if (propertyValue != null)
            {
                return propertyValue;
            }
            else if (!string.IsNullOrEmpty(DefaultConfig[option.Switch] as string))
            {
                var configDefault = DefaultConfig[option.Switch] as string;
                if (string.IsNullOrEmpty(configDefault))
                    return null;

                return configDefault.SplitByComma();
            }
            else if (required && !this.DisableInteractive)
            {
                var response = PromptForValue(option);
                if (string.IsNullOrEmpty(response))
                    return null;

                return response.SplitByComma();
            }
            else if (_cachedRequestedValues.ContainsKey(option))
            {
                var cachedValue = _cachedRequestedValues[option];
                return cachedValue?.SplitByComma();
            }

            if (required)
            {
                throw new ToolsException($"Missing required parameter: {option.Switch}", ToolsException.CommonErrorCode.MissingRequiredParameter);
            }

            return null;
        }

        public Dictionary<string, string> GetKeyValuePairOrDefault(Dictionary<string, string> propertyValue, CommandOption option, bool required)
        {
            if (propertyValue != null)
            {
                return propertyValue;
            }
            else if (!string.IsNullOrEmpty(DefaultConfig[option.Switch] as string))
            {
                var configDefault = DefaultConfig[option.Switch] as string;
                if (string.IsNullOrEmpty(configDefault))
                    return null;

                return Utilities.ParseKeyValueOption(configDefault);
            }
            else if (required && !this.DisableInteractive)
            {
                var response = PromptForValue(option);
                if (string.IsNullOrEmpty(response))
                    return null;

                return Utilities.ParseKeyValueOption(response);
            }
            else if (_cachedRequestedValues.ContainsKey(option))
            {
                var cachedValue = _cachedRequestedValues[option];
                return cachedValue == null ? null : Utilities.ParseKeyValueOption(cachedValue);
            }

            if (required)
            {
                throw new ToolsException($"Missing required parameter: {option.Switch}", ToolsException.CommonErrorCode.MissingRequiredParameter);
            }

            return null;
        }

        /// <summary>
        /// Int version of GetStringValueOrDefault
        /// </summary>
        /// <param name="propertyValue"></param>
        /// <param name="option"></param>
        /// <param name="required"></param>
        /// <returns></returns>
        public int? GetIntValueOrDefault(int? propertyValue, CommandOption option, bool required)
        {
            if (propertyValue.HasValue)
            {
                return propertyValue.Value;
            }
            else if (DefaultConfig[option.Switch] is int)
            {
                var configDefault = (int)DefaultConfig[option.Switch];
                return configDefault;
            }
            else if (required && !this.DisableInteractive)
            {
                var userValue = PromptForValue(option);
                if (string.IsNullOrWhiteSpace(userValue))
                    return null;

                int i;
                if (!int.TryParse(userValue, out i))
                {
                    throw new ToolsException($"{userValue} cannot be parsed into an integer for {option.Name}", ToolsException.CommonErrorCode.CommandLineParseError);
                }
                return i;
            }
            else if (_cachedRequestedValues.ContainsKey(option))
            {
                var cachedValue = _cachedRequestedValues[option];
                int i;
                if (int.TryParse(cachedValue, out i))
                {
                    return i;
                }
            }

            if (required)
            {
                throw new ToolsException($"Missing required parameter: {option.Switch}", ToolsException.CommonErrorCode.MissingRequiredParameter);
            }

            return null;
        }

        /// <summary>
        /// bool version of GetStringValueOrDefault
        /// </summary>
        /// <param name="propertyValue"></param>
        /// <param name="option"></param>
        /// <param name="required"></param>
        /// <returns></returns>
        public bool? GetBoolValueOrDefault(bool? propertyValue, CommandOption option, bool required)
        {
            if (propertyValue.HasValue)
            {
                return propertyValue.Value;
            }
            else if (DefaultConfig[option.Switch] is bool)
            {
                var configDefault = (bool)DefaultConfig[option.Switch];
                return configDefault;
            }
            else if (required && !this.DisableInteractive)
            {
                var userValue = PromptForValue(option);
                if (string.IsNullOrWhiteSpace(userValue))
                    return null;

                bool i;
                if (bool.TryParse(userValue, out i))
                {
                    throw new ToolsException($"{userValue} cannot be parsed into a boolean for {option.Name}", ToolsException.CommonErrorCode.CommandLineParseError);
                }
                return i;
            }
            else if (_cachedRequestedValues.ContainsKey(option))
            {
                var cachedValue = _cachedRequestedValues[option];
                bool i;
                if (bool.TryParse(cachedValue, out i))
                {
                    return i;
                }
            }

            if (required)
            {
                throw new ToolsException($"Missing required parameter: {option.Switch}", ToolsException.CommonErrorCode.MissingRequiredParameter);
            }

            return null;
        }

        // Cache all prompted values so the user is never prompted for the same CommandOption later.
        Dictionary<CommandOption, string> _cachedRequestedValues = new Dictionary<CommandOption, string>();
        protected string PromptForValue(CommandOption option)
        {
            if (_cachedRequestedValues.ContainsKey(option))
            {
                var cachedValue = _cachedRequestedValues[option];
                return cachedValue;
            }

            string input = null;


            Console.Out.WriteLine($"Enter {option.Name}: ({option.Description})");
            Console.Out.Flush();
            input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
                return null;
            input = input.Trim();

            _cachedRequestedValues[option] = input;
            return input;
        }





        public IToolLogger Logger { get; protected set; }
        public string WorkingDirectory { get; set; }

        protected void SetUserAgentString()
        {
            string version = this.GetType().GetTypeInfo().Assembly.GetName().Version.ToString();
            Util.Internal.InternalSDKUtils.SetUserAgent(this.ToolName,
                                          version);
        }

        IAmazonIdentityManagementService _iamClient;
        public IAmazonIdentityManagementService IAMClient
        {
            get
            {
                if (this._iamClient == null)
                {
                    SetUserAgentString();

                    var config = new AmazonIdentityManagementServiceConfig();
                    config.RegionEndpoint = DetermineAWSRegion();

                    this._iamClient = new AmazonIdentityManagementServiceClient(DetermineAWSCredentials(), config);
                }
                return this._iamClient;
            }
            set { this._iamClient = value; }
        }

        IAmazonS3 _s3Client;
        public IAmazonS3 S3Client
        {
            get
            {
                if (this._s3Client == null)
                {
                    SetUserAgentString();

                    var config = new AmazonS3Config();
                    config.RegionEndpoint = DetermineAWSRegion();

                    this._s3Client = new AmazonS3Client(DetermineAWSCredentials(), config);
                }
                return this._s3Client;
            }
            set { this._s3Client = value; }
        }

        protected void SaveConfigFile()
        {
            try
            {
                JsonData data;
                if (File.Exists(this.DefaultConfig.SourceFile))
                {
                    data = JsonMapper.ToObject(File.ReadAllText(this.DefaultConfig.SourceFile));
                }
                else
                {
                    data = new JsonData();
                }

                data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_AWS_REGION.ConfigFileKey, this.GetStringValueOrDefault(this.Region, CommonDefinedCommandOptions.ARGUMENT_AWS_REGION, false));
                data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_AWS_PROFILE.ConfigFileKey, this.GetStringValueOrDefault(this.Profile, CommonDefinedCommandOptions.ARGUMENT_AWS_PROFILE, false));
                data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_AWS_PROFILE_LOCATION.ConfigFileKey, this.GetStringValueOrDefault(this.ProfileLocation, CommonDefinedCommandOptions.ARGUMENT_AWS_PROFILE_LOCATION, false));


                SaveConfigFile(data);

                StringBuilder sb = new StringBuilder();
                JsonWriter writer = new JsonWriter(sb);
                writer.PrettyPrint = true;
                JsonMapper.ToJson(data, writer);

                var json = sb.ToString();
                File.WriteAllText(this.DefaultConfig.SourceFile, json);
                this.Logger?.WriteLine($"Config settings saved to {this.DefaultConfig.SourceFile}");
            }
            catch (Exception e)
            {
                throw new ToolsException("Error persisting configuration file: " + e.Message, ToolsException.CommonErrorCode.PersistConfigError);
            }
        }

        protected abstract void SaveConfigFile(JsonData data);
    }
}
