using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Lambda.Controller;
using Amazon.AWSToolkit.Lambda.Util;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Telemetry;
using Amazon.Common.DotNetCli.Tools;
using Amazon.ECR;
using Amazon.IdentityManagement;
using Amazon.Lambda;
using Amazon.Lambda.Tools.Commands;
using Amazon.S3;
using Amazon.SecurityToken;

using log4net;

using static Amazon.AWSToolkit.Lambda.Controller.UploadFunctionController;
using static Amazon.AWSToolkit.Lambda.Util.LambdaTelemetryUtils;

namespace Amazon.AWSToolkit.Lambda.DeploymentWorkers
{
    public class UploadNETCoreWorker : BaseUploadWorker
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(UploadNETCoreWorker));

        private readonly ITelemetryLogger _telemetryLogger;
        private readonly IAmazonIdentityManagementService _iamClient;
        private readonly IAmazonS3 _s3Client;
        private readonly UploadOriginator _originator;

        public UploadNETCoreWorker(ILambdaFunctionUploadHelpers functionHandler,
            IAmazonSecurityTokenService stsClient,
            IAmazonLambda lambdaClient, IAmazonECR ecrClient,
            IAmazonIdentityManagementService iamClient, IAmazonS3 s3Client, UploadOriginator originator, ITelemetryLogger telemetryLogger)
            : base(functionHandler, stsClient, lambdaClient, ecrClient)
        {
            _iamClient = iamClient;
            _s3Client = s3Client;
            _telemetryLogger = telemetryLogger;
            _originator = originator;
        }

        static readonly TimeSpan SLEEP_TIME_FOR_ROLE_PROPOGATION = TimeSpan.FromSeconds(15);

        public override void UploadFunction(UploadFunctionState uploadState)
        {
            ActionResults results = null;
            var deploymentProperties = new RecordLambdaDeployProperties();

            void Invoke() => results = Upload(uploadState, out deploymentProperties);

            void Record(ITelemetryLogger telemetryLogger, double duration)
            {
                var metricSource = _originator.AsMetricSource();
                _telemetryLogger.RecordLambdaDeploy(results, duration, metricSource, deploymentProperties);
            }

            _telemetryLogger.TimeAndRecord(Invoke, Record);
        }

        private ActionResults Upload(UploadFunctionState uploadState, out RecordLambdaDeployProperties deploymentProperties)
        {
            var logger = new DeployToolLogger(this.FunctionUploader);
            deploymentProperties = new RecordLambdaDeployProperties();
            try
            {
                var architectureList = uploadState.GetRequestArchitectures();
                deploymentProperties.AccountId = uploadState.AccountId;
                deploymentProperties.RegionId = uploadState.Region?.Id;
                deploymentProperties.Runtime = uploadState.Request?.Runtime;
                deploymentProperties.TargetFramework = uploadState.Framework;
                deploymentProperties.NewResource = IsNewResource(uploadState);
                deploymentProperties.LambdaPackageType = uploadState.Request?.PackageType;
                var command = new DeployFunctionCommand(logger, uploadState.SourcePath, new string[0]);
                command.DisableInteractive = true;
                command.STSClient = this.StsClient;
                command.LambdaClient = this.LambdaClient;
                command.ECRClient = this.ECRClient;
                command.IAMClient = _iamClient;
                command.S3Client = _s3Client;
                command.PersistConfigFile = uploadState.SaveSettings;
                command.Profile = uploadState.Account.Identifier.ProfileName;
                command.Credentials = uploadState.Credentials;
                command.Region = uploadState.Region.Id;

                command.FunctionName = uploadState.Request.FunctionName;
                command.Description = uploadState.Request.Description;
                command.Handler = uploadState.Request.Handler;
                command.MemorySize = uploadState.Request.MemorySize;
                command.Timeout = uploadState.Request.Timeout;
                command.Configuration = uploadState.Configuration;
                command.PackageType = uploadState.Request?.PackageType;
                command.TargetFramework = uploadState.Framework;
                command.Runtime = uploadState.Request.Runtime;
                if (architectureList.Count == 1)
                {
                    deploymentProperties.LambdaArchitecture = architectureList.First();
                    command.Architecture = architectureList.First();
                }
                command.EnvironmentVariables = uploadState.Request?.Environment?.Variables;
                command.KMSKeyArn = uploadState.Request?.KMSKeyArn;
                command.TracingMode = uploadState.Request?.TracingConfig?.Mode;
                command.DeadLetterTargetArn = uploadState.Request?.DeadLetterConfig?.TargetArn;
                command.DockerFile = uploadState.Dockerfile;
                command.DockerImageTag = GetDockerImageTag(uploadState);
                command.ImageCommand = uploadState.Request?.ImageConfig?.Command?.ToArray();

                if (uploadState.Request.VpcConfig != null)
                {
                    command.SubnetIds = uploadState.Request.VpcConfig.SubnetIds?.ToArray();
                    command.SecurityGroupIds = uploadState.Request.VpcConfig.SecurityGroupIds?.ToArray();
                }


                if (uploadState.SelectedRole != null)
                {
                    command.Role = uploadState.SelectedRole.Arn;
                }
                else if (uploadState.SelectedManagedPolicy != null)
                {
                    command.Role = this.CreateRole(uploadState);
                    logger.WriteLine(string.Format("Created IAM role {0} with managed policy {1}", command.Role,
                        uploadState.SelectedManagedPolicy.PolicyName));
                    logger.WriteLine("Waiting for new IAM Role to propagate to AWS regions");
                    Thread.Sleep(SLEEP_TIME_FOR_ROLE_PROPOGATION);
                }

                if (command.ExecuteAsync().Result)
                {
                    deploymentProperties.Language = this.FunctionUploader.GetFunctionLanguage();
                    deploymentProperties.XRayEnabled = this.FunctionUploader.XRayEnabled();

                    FunctionUploader.UploadFunctionAsyncCompleteSuccess(uploadState);

                    return new ActionResults().WithSuccess(true);
                }
                else
                {
                    if (command.LastToolsException != null)
                    {
                        throw command.LastToolsException;
                    }
                    else
                    {
                        throw new ToolkitException("Failed to deploy Lambda Function",
                            ToolkitException.CommonErrorCode.UnexpectedError);
                    }
                }
            }

            catch (ToolsException ex)
            {
                FunctionUploader.UploadFunctionAsyncCompleteError("Error uploading function");
                return ActionResults.CreateFailed(ex);
            }
            catch (ToolkitException ex)
            {
                logger.WriteLine(ex.Message);
                FunctionUploader.UploadFunctionAsyncCompleteError("Error uploading function");
                return ActionResults.CreateFailed(ex);
            }
            catch (Exception ex)
            {
                logger.WriteLine(ex.Message);
                _logger.Error("Error uploading Lambda function.", ex);
                FunctionUploader.UploadFunctionAsyncCompleteError("Error uploading function");
                return ActionResults.CreateFailed(ex);
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
                _logger.Error("Error looking up lambda configuration", e);
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

        public static string GetDockerImageTag(UploadFunctionController.UploadFunctionState uploadState)
        {
            var dockerRepository = uploadState.ImageRepo;
            var dockerTag = uploadState.ImageTag;
            var dockerImageTag = string.IsNullOrEmpty(dockerRepository) ? "" : dockerRepository;
            if (!string.IsNullOrEmpty(dockerTag))
                dockerImageTag += ":" + dockerTag;

            return dockerImageTag;
        }
    }
}
