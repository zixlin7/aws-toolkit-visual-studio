﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Options;
using Amazon.Lambda.Model;

using ThirdParty.Json.LitJson;

namespace Amazon.Lambda.Tools.Commands
{
    /// <summary>
    /// Get the current configuration for a deployed function
    /// </summary>
    public class GetFunctionConfigCommand : LambdaBaseCommand
    {
        public const string COMMAND_NAME = "get-function-config";
        public const string COMMAND_DESCRIPTION = "Command to get the current runtime configuration for a Lambda function";
        public const string COMMAND_ARGUMENTS = "<FUNCTION-NAME> The name of the function to get the configuration for";

        public static readonly IList<CommandOption> GetConfigCommandOptions = BuildLineOptions(new List<CommandOption>
        {
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME
        });

        public string FunctionName { get; set; }

        public GetFunctionConfigCommand(IToolLogger logger, string workingDirectory, string[] args)
            : base(logger, workingDirectory, GetConfigCommandOptions, args)
        {
        }

        /// <summary>
        /// Parse the CommandOptions into the Properties on the command.
        /// </summary>
        /// <param name="values"></param>
        protected override void ParseCommandArguments(CommandOptions values)
        {
            base.ParseCommandArguments(values);

            if (values.Arguments.Count > 0)
            {
                this.FunctionName = values.Arguments[0];
            }

            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME.Switch)) != null)
                this.FunctionName = tuple.Item2.StringValue;
        }

        protected override async Task<bool> PerformActionAsync()
        {
            GetFunctionConfigurationResponse response;

            try
            {
                response = await this.LambdaClient.GetFunctionConfigurationAsync(this.GetStringValueOrDefault(this.FunctionName, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME, true));
            }
            catch (Exception e)
            {
                throw new LambdaToolsException("Error getting configuration for Lambda function: " + e.Message, LambdaToolsException.LambdaErrorCode.LambdaGetConfiguration, e);
            }

            const int PAD_SIZE = 20;
            this.Logger.WriteLine("Name:".PadRight(PAD_SIZE) + response.FunctionName);
            this.Logger.WriteLine("Arn:".PadRight(PAD_SIZE) + response.FunctionArn);
            if(!string.IsNullOrEmpty(response.Description))
                this.Logger.WriteLine("Description:".PadRight(PAD_SIZE) + response.Description);
            this.Logger.WriteLine("Handler:".PadRight(PAD_SIZE) + response.Handler);
            this.Logger.WriteLine("Last Modified:".PadRight(PAD_SIZE) + response.LastModified);
            this.Logger.WriteLine("Memory Size:".PadRight(PAD_SIZE) + response.MemorySize);
            this.Logger.WriteLine("Role:".PadRight(PAD_SIZE) + response.Role);
            this.Logger.WriteLine("Timeout:".PadRight(PAD_SIZE) + response.Timeout);
            this.Logger.WriteLine("Version:".PadRight(PAD_SIZE) + response.Version);

            if(!string.IsNullOrEmpty(response.KMSKeyArn))
                this.Logger.WriteLine("KMS Key ARN:".PadRight(PAD_SIZE) + response.KMSKeyArn);
            else
                this.Logger.WriteLine("KMS Key ARN:".PadRight(PAD_SIZE) + "(default) aws/lambda");

            if(!string.IsNullOrEmpty(response.DeadLetterConfig?.TargetArn))
            {
                this.Logger.WriteLine("Dead Letter Target:".PadRight(PAD_SIZE) + response.DeadLetterConfig.TargetArn);
            }


            if (response.Environment?.Variables?.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach(var kvp in response.Environment.Variables)
                {
                    if (sb.Length > 0)
                        sb.Append(";");
                    sb.Append($"{kvp.Key}={kvp.Value}");
                }
                this.Logger.WriteLine("Environment Vars:".PadRight(PAD_SIZE) + sb);
            }


            if (response.VpcConfig != null && !string.IsNullOrEmpty(response.VpcConfig.VpcId))
            {
                this.Logger.WriteLine("VPC Config");
                this.Logger.WriteLine("   VPC: ".PadRight(22) + response.VpcConfig.VpcId);
                this.Logger.WriteLine("   Security Groups: ".PadRight(22) + string.Join(",", response.VpcConfig?.SecurityGroupIds));
                this.Logger.WriteLine("   Subnets: ".PadRight(22) + string.Join(",", response.VpcConfig?.SubnetIds));
            }


            return true;
        }
        
        protected override void SaveConfigFile(JsonData data)
        {
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME.ConfigFileKey, this.GetStringValueOrDefault(this.FunctionName, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME, false));    
        }

    }
}