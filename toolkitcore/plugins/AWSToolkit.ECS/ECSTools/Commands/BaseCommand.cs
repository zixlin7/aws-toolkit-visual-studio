﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;

using Amazon.ECR;
using Amazon.ECS;

using Amazon.ECS.Tools.Options;
using System.Reflection;

namespace Amazon.ECS.Tools.Commands
{

    public abstract class BaseCommand : ICommand
    {
        /// <summary>
        /// The common options used by every command
        /// </summary>
        protected static readonly IList<CommandOption> CommonOptions = new List<CommandOption>
        {
            DefinedCommandOptions.ARGUMENT_DISABLE_INTERACTIVE,
            DefinedCommandOptions.ARGUMENT_AWS_REGION,
            DefinedCommandOptions.ARGUMENT_AWS_PROFILE,
            DefinedCommandOptions.ARGUMENT_AWS_PROFILE_LOCATION,
            DefinedCommandOptions.ARGUMENT_PROJECT_LOCATION,
            DefinedCommandOptions.ARGUMENT_CONFIG_FILE
        };

        public abstract Task<bool> ExecuteAsync();

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



        public IToolLogger Logger { get; protected set; }
        public string WorkingDirectory { get; set; }

        public DockerToolsException LastToolsException { get; protected set; }

        public string [] OriginalCommandLineArguments { get; private set; }


        DockerToolsDefaults _defaultConfig;
        public DockerToolsDefaults DefaultConfig
        {
            get
            {
                if (this._defaultConfig == null)
                {
                    this._defaultConfig = DockerToolsDefaultsReader.LoadDefaults(Utilities.DetermineProjectLocation(this.WorkingDirectory, this.ProjectLocation), this.ConfigFile);
                }
                return this._defaultConfig;
            }
        }

        private static void SetUserAgentString()
        {
            string version = typeof(BaseCommand).GetTypeInfo().Assembly.GetName().Version.ToString();
            Util.Internal.InternalSDKUtils.SetUserAgent("AWSDockerToolsDotnet",
                                          version);
        }


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

        /// <summary>
        /// Parse the CommandOptions into the Properties on the command.
        /// </summary>
        /// <param name="values"></param>
        protected virtual void ParseCommandArguments(CommandOptions values)
        {
            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_DISABLE_INTERACTIVE.Switch)) != null)
                this.DisableInteractive = tuple.Item2.BoolValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_AWS_PROFILE.Switch)) != null)
                this.Profile = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_AWS_PROFILE_LOCATION.Switch)) != null)
                this.ProfileLocation = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_AWS_REGION.Switch)) != null)
                this.Region = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_PROJECT_LOCATION.Switch)) != null)
                this.ProjectLocation = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_CONFIG_FILE.Switch)) != null)
                this.ConfigFile = tuple.Item2.StringValue;

            if (string.IsNullOrEmpty(this.ConfigFile))
                this.ConfigFile = DockerToolsDefaultsReader.DEFAULT_FILE_NAME;
        }


        IAmazonECR _ecrClient;
        public IAmazonECR ECRClient
        {
            get
            {
                if (this._ecrClient == null)
                {
                    SetUserAgentString();

                    var config = new AmazonECRConfig();
                    config.RegionEndpoint = DetermineAWSRegion();

                    this._ecrClient = new AmazonECRClient(DetermineAWSCredentials(), config);
                }
                return this._ecrClient;
            }
            set { this._ecrClient = value; }
        }

        IAmazonECS _ecsClient;
        public IAmazonECS ECSClient
        {
            get
            {
                if (this._ecsClient == null)
                {
                    SetUserAgentString();

                    var config = new AmazonECSConfig();
                    config.RegionEndpoint = DetermineAWSRegion();

                    this._ecsClient = new AmazonECSClient(DetermineAWSCredentials(), config);
                }
                return this._ecsClient;
            }
            set { this._ecsClient = value; }
        }


        private AWSCredentials DetermineAWSCredentials()
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
                    profile = DefaultConfig[DefinedCommandOptions.ARGUMENT_AWS_PROFILE.Switch] as string;
                }

                if (!string.IsNullOrEmpty(profile))
                {
                    var chain = new CredentialProfileStoreChain(this.ProfileLocation);
                    if (!chain.TryGetAWSCredentials(profile, out credentials))
                    {
                        throw new DockerToolsException($"Credentials for profile {profile} cannot be found", DockerToolsException.ErrorCode.ProfileNotFound);
                    }
                }
                else
                {
                    credentials = FallbackCredentialsFactory.GetCredentials();
                }
            }

            return credentials;
        }

        private RegionEndpoint DetermineAWSRegion()
        {
            // See if a region has been set but don't prompt if not set.
            var regionName = this.GetStringValueOrDefault(this.Region, DefinedCommandOptions.ARGUMENT_AWS_REGION, false);
            if(!string.IsNullOrWhiteSpace(regionName))
            {
                return RegionEndpoint.GetBySystemName(regionName);
            }

            // See if we can find a region using the region fallback logic.
            if(string.IsNullOrWhiteSpace(regionName))
            {
                var region = FallbackRegionFactory.GetRegionEndpoint(true);
                if (region != null)
                {
                    return region;
                }
            }

            // If we still don't have a region prompt the user for a region.
            regionName = this.GetStringValueOrDefault(this.Region, DefinedCommandOptions.ARGUMENT_AWS_REGION, true);
            if (!string.IsNullOrWhiteSpace(regionName))
            {
                return RegionEndpoint.GetBySystemName(regionName);
            }

            throw new DockerToolsException("Can not determine AWS region. Either configure a default region or use the --region option.", DockerToolsException.ErrorCode.RegionNotConfigured);
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

            if(required)
            {
                throw new DockerToolsException($"Missing required parameter: {option.Switch}", DockerToolsException.ErrorCode.MissingRequiredParameter);
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
                throw new DockerToolsException($"Missing required parameter: {option.Switch}", DockerToolsException.ErrorCode.MissingRequiredParameter);
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
                throw new DockerToolsException($"Missing required parameter: {option.Switch}", DockerToolsException.ErrorCode.MissingRequiredParameter);
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
                    throw new DockerToolsException($"{userValue} cannot be parsed into an integer for {option.Name}", DockerToolsException.ErrorCode.CommandLineParseError);
                }
                return i;
            }
            else if (_cachedRequestedValues.ContainsKey(option))
            {
                var cachedValue = _cachedRequestedValues[option];
                int i;
                if(int.TryParse(cachedValue, out i))
                {
                    return i;
                }
            }

            if (required)
            {
                throw new DockerToolsException($"Missing required parameter: {option.Switch}", DockerToolsException.ErrorCode.MissingRequiredParameter);
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
                    throw new DockerToolsException($"{userValue} cannot be parsed into a boolean for {option.Name}", DockerToolsException.ErrorCode.CommandLineParseError);
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
                throw new DockerToolsException($"Missing required parameter: {option.Switch}", DockerToolsException.ErrorCode.MissingRequiredParameter);
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
    }
}
