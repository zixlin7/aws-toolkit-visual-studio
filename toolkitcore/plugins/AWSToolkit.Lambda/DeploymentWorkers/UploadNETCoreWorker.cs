using Amazon.Lambda;
using Amazon.Lambda.Tools.Commands;

using log4net;
using System;
using System.Threading;
using Amazon.AWSToolkit.Lambda.Controller;
using System.IO;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Lambda.Util;
using Amazon.Common.DotNetCli.Tools;

namespace Amazon.AWSToolkit.Lambda.DeploymentWorkers
{
    public class UploadNETCoreWorker : BaseUploadWorker
    {
        ILog LOGGER = LogManager.GetLogger(typeof(UploadGenericWorker));

        public UploadNETCoreWorker(ILambdaFunctionUploadHelpers functionHandler, IAmazonLambda lambdaClient)
            : base(functionHandler, lambdaClient)
        {
        }

        static readonly TimeSpan SLEEP_TIME_FOR_ROLE_PROPOGATION = TimeSpan.FromSeconds(15);
        public override void UploadFunction(UploadFunctionController.UploadFunctionState uploadState)
        {
            var logger = new DeployToolLogger(this.FunctionUploader);

            var lambdaDeploymentMetrics =
                new LambdaDeploymentMetrics(LambdaDeploymentMetrics.LambdaPublishMethod.NetCore,
                    uploadState.Request.Runtime);

            try
            {

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
                    var lambdaDeploymentProperties = new LambdaDeploymentMetrics.LambdaDeploymentProperties
                    {
                        TargetFramework = command.TargetFramework,
                        MemorySize = command.MemorySize.GetValueOrDefault().ToString(),

                    };

                    if (string.Equals(command.TracingMode, TracingMode.Active, StringComparison.OrdinalIgnoreCase))
                    {
                        lambdaDeploymentProperties.XRayEnabled = true;
                    }

                    var zipArchivePath = Path.Combine(uploadState.SourcePath, "bin", uploadState.Configuration,
                        uploadState.Framework, new DirectoryInfo(uploadState.SourcePath).Name + ".zip");
                    if (File.Exists(zipArchivePath))
                    {
                        lambdaDeploymentProperties.BundleSize = new FileInfo(zipArchivePath).Length;
                    }

                    lambdaDeploymentMetrics.QueueDeploymentSuccess(lambdaDeploymentProperties);

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
                lambdaDeploymentMetrics.QueueDeploymentFailure(e.Code, e.ServiceCode);
                this.FunctionUploader.UploadFunctionAsyncCompleteError("Error uploading function");
            }
            catch (ToolkitException e)
            {
                logger.WriteLine(e.Message);
                lambdaDeploymentMetrics.QueueDeploymentFailure(e.Code, e.ServiceErrorCode, e.ServiceStatusCode);
                this.FunctionUploader.UploadFunctionAsyncCompleteError("Error uploading function");
            }
            catch (Exception e)
            {
                logger.WriteLine(e.Message);
                LOGGER.Error("Error uploading Lambda function.", e);
                lambdaDeploymentMetrics.QueueDeploymentFailure(ToolkitException.CommonErrorCode.UnexpectedError.ToString(), null);
                this.FunctionUploader.UploadFunctionAsyncCompleteError("Error uploading function");
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
