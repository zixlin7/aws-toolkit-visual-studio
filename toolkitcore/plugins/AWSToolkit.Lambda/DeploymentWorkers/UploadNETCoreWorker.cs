

using Amazon.AWSToolkit.MobileAnalytics;
using Amazon.Lambda;

using Amazon.Lambda.Tools;
using Amazon.Lambda.Tools.Commands;

using log4net;
using System;
using System.Threading;
using Amazon.AWSToolkit.Lambda.Controller;
using System.IO;

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
            try
            {

                var command = new DeployFunctionCommand(logger, uploadState.SourcePath, new string[0]);
                command.EnableInteractive = false;
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

                if (uploadState.Request.VpcConfig != null)
                {
                    command.SubnetIds = uploadState.Request.VpcConfig.SubnetIds?.ToArray();
                    command.SecurityGroupIds = uploadState.Request.VpcConfig.SecurityGroupIds?.ToArray();
                }


                if (uploadState.SelectedRole != null)
                {
                    command.Role = uploadState.SelectedRole.Arn;
                }
                else
                {
                    command.Role = this.CreateRole(uploadState);
                    logger.WriteLine(string.Format("Created IAM role {0} with managed policy {1}", command.Role, uploadState.SelectedManagedPolicy.PolicyName));
                    logger.WriteLine("Waiting for new IAM Role to propagate to AWS regions");
                    Thread.Sleep(SLEEP_TIME_FOR_ROLE_PROPOGATION);
                }

                if (command.ExecuteAsync().Result)
                {
                    ToolkitEvent evnt = new ToolkitEvent();
                    evnt.AddProperty(AttributeKeys.LambdaFunctionDeploymentSuccess, uploadState.Request.Runtime);

                    var zipArchivePath = Path.Combine(uploadState.SourcePath, "bin", uploadState.Configuration, uploadState.Framework, new DirectoryInfo(uploadState.SourcePath).Name + ".zip");
                    if(File.Exists(zipArchivePath))
                    {
                        long size = new FileInfo(zipArchivePath).Length;
                        evnt.AddProperty(MetricKeys.LambdaDeploymentBundleSize, size);
                    }

                    SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);

                    this.FunctionUploader.UploadFunctionAsyncCompleteSuccess(uploadState);
                }
                else
                {
                    ToolkitEvent evnt = new ToolkitEvent();
                    evnt.AddProperty(AttributeKeys.LambdaFunctionDeploymentError, uploadState.Request.Runtime);
                    if(command.LastToolsException != null)
                    {
                        if(string.IsNullOrEmpty(command.LastToolsException.ServiceCode))
                            evnt.AddProperty(AttributeKeys.LambdaFunctionDeploymentErrorDetail, $"{command.LastToolsException.Code}");
                        else
                            evnt.AddProperty(AttributeKeys.LambdaFunctionDeploymentErrorDetail, $"{command.LastToolsException.Code}-{command.LastToolsException.ServiceCode}");
                    }

                    SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);

                    this.FunctionUploader.UploadFunctionAsyncCompleteError("Error uploading function");
                }
            }
            catch(Exception e)
            {
                ToolkitEvent evnt = new ToolkitEvent();
                evnt.AddProperty(AttributeKeys.LambdaFunctionDeploymentError, uploadState.Request.Runtime);
                SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);

                LOGGER.Error("Error uploading Lambda function.", e);
                this.FunctionUploader.UploadFunctionAsyncCompleteError("Error uploading function: " + e.Message);
            }
        }

        internal class DeployToolLogger : IToolLogger
        {
            ILambdaFunctionUploadHelpers FunctionHandler { get; set; }

            internal DeployToolLogger(ILambdaFunctionUploadHelpers controller)
            {
                this.FunctionHandler = controller;
            }

            public void WriteLine(string message)
            {
                this.FunctionHandler.AppendUploadStatus(message);
            }
        }
    }
}
