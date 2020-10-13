﻿using Amazon.Lambda;
using Amazon.Lambda.Tools.Commands;

using log4net;
using System;
using System.Diagnostics;
using System.Threading;
using Amazon.AWSToolkit.Lambda.Controller;
using System.IO;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Lambda.Util;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.Common.DotNetCli.Tools;

namespace Amazon.AWSToolkit.Lambda.DeploymentWorkers
{
    public class UploadNETCoreWorker : BaseUploadWorker
    {
        ILog LOGGER = LogManager.GetLogger(typeof(UploadNETCoreWorker));

        private readonly ITelemetryLogger _telemetryLogger;

        public UploadNETCoreWorker(ILambdaFunctionUploadHelpers functionHandler, IAmazonLambda lambdaClient,
            ITelemetryLogger telemetryLogger)
            : base(functionHandler, lambdaClient)
        {
            _telemetryLogger = telemetryLogger;
        }

        static readonly TimeSpan SLEEP_TIME_FOR_ROLE_PROPOGATION = TimeSpan.FromSeconds(15);
        public override void UploadFunction(UploadFunctionController.UploadFunctionState uploadState)
        {
            var logger = new DeployToolLogger(this.FunctionUploader);
            var deploymentProperties = new LambdaTelemetryUtils.RecordLambdaDeployProperties();

            try
            {
                deploymentProperties.RegionId = uploadState.Region?.SystemName;
                deploymentProperties.Runtime = uploadState.Request?.Runtime;
                deploymentProperties.TargetFramework = uploadState.Framework;
                deploymentProperties.NewResource = IsNewResource(uploadState);

                var command = new DeployFunctionCommand(logger, uploadState.SourcePath, new string[0]);
                command.DisableInteractive = true;
                command.SkipHandlerValidation = true;
                command.LambdaClient = this.LambdaClient;
                command.PersistConfigFile = uploadState.SaveSettings;
                command.Profile = uploadState.Account.DisplayName;
                command.Region = uploadState.Region.SystemName;

                command.FunctionName = uploadState.Request.FunctionName;
                command.Description = uploadState.Request.Description;
                command.Handler = uploadState.Request.Handler;
                command.MemorySize = uploadState.Request.MemorySize;
                command.Timeout = uploadState.Request.Timeout;
                command.Configuration = uploadState.Configuration;
                command.TargetFramework = uploadState.Framework;
                command.Runtime = uploadState.Request.Runtime;
                command.EnvironmentVariables = uploadState.Request?.Environment?.Variables;
                command.KMSKeyArn = uploadState.Request?.KMSKeyArn;
                command.TracingMode = uploadState.Request?.TracingConfig?.Mode;
                command.DeadLetterTargetArn = uploadState.Request?.DeadLetterConfig?.TargetArn;

                if (uploadState.Request.VpcConfig != null)
                {
                    command.SubnetIds = uploadState.Request.VpcConfig.SubnetIds?.ToArray();
                    command.SecurityGroupIds = uploadState.Request.VpcConfig.SecurityGroupIds?.ToArray();
                }


                if (uploadState.SelectedRole != null)
                {
                    command.Role = uploadState.SelectedRole.Arn;
                }
                else if(uploadState.SelectedManagedPolicy != null)
                {
                    command.Role = this.CreateRole(uploadState);
                    logger.WriteLine(string.Format("Created IAM role {0} with managed policy {1}", command.Role,
                        uploadState.SelectedManagedPolicy.PolicyName));
                    logger.WriteLine("Waiting for new IAM Role to propagate to AWS regions");
                    Thread.Sleep(SLEEP_TIME_FOR_ROLE_PROPOGATION);
                }

                if (command.ExecuteAsync().Result)
                {
                    _telemetryLogger.RecordLambdaDeploy(Result.Succeeded, deploymentProperties);

                    this.FunctionUploader.UploadFunctionAsyncCompleteSuccess(uploadState);
                }
                else
                {
                    if (command.LastToolsException != null)
                    {
                        throw command.LastToolsException;
                    }
                    else
                    {
                        throw new ToolkitException("Failed to deploy Lambda Function", ToolkitException.CommonErrorCode.UnexpectedError);
                    }
                }
            }

            catch (ToolsException e)
            {
                _telemetryLogger.RecordLambdaDeploy(Result.Failed, deploymentProperties);
                this.FunctionUploader.UploadFunctionAsyncCompleteError("Error uploading function");
            }
            catch (ToolkitException e)
            {
                logger.WriteLine(e.Message);
                _telemetryLogger.RecordLambdaDeploy(Result.Failed, deploymentProperties);
                this.FunctionUploader.UploadFunctionAsyncCompleteError("Error uploading function");
            }
            catch (Exception e)
            {
                logger.WriteLine(e.Message);
                LOGGER.Error("Error uploading Lambda function.", e);
                _telemetryLogger.RecordLambdaDeploy(Result.Failed, deploymentProperties);
                this.FunctionUploader.UploadFunctionAsyncCompleteError("Error uploading function");
            }
        }

        private bool IsNewResource(UploadFunctionController.UploadFunctionState uploadState)
        {
            try
            {
                var existingConfiguration = this.FunctionUploader.GetExistingConfiguration(this.LambdaClient, uploadState.Request.FunctionName);
                return existingConfiguration == null;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error looking up lambda configuration", e);
                Debug.Assert(false, $"Error looking up lambda configuration. Function will be considered new for metrics purposes: {e.Message}");
                return true;
            }
        }

        internal class DeployToolLogger : IToolLogger
        {
            ILambdaFunctionUploadHelpers FunctionHandler { get; }

            internal DeployToolLogger(ILambdaFunctionUploadHelpers controller)
            {
                this.FunctionHandler = controller;
            }

            public void WriteLine(string message)
            {
                this.FunctionHandler.AppendUploadStatus(message);
            }

            public void WriteLine(string message, params object[] args)
            {
                WriteLine(string.Format(message, args));
            }
        }
    }
}
