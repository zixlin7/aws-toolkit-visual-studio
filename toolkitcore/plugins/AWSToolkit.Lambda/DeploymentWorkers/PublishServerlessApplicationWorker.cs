using System;
using System.Collections.Generic;
using Amazon.S3;
using Amazon.CloudFormation;

using log4net;

using Amazon.Common.DotNetCli.Tools;
using Amazon.Lambda.Tools.Commands;

using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Account;
using System.IO;
using Amazon.AWSToolkit.Lambda.Util;

namespace Amazon.AWSToolkit.Lambda.DeploymentWorkers
{
    public class PublishServerlessApplicationWorker
    {
        ILog LOGGER = LogManager.GetLogger(typeof(UploadGenericWorker));

        PublishServerlessApplicationWorkerSettings Settings { get; }
        ILambdaFunctionUploadHelpers Helpers { get; }

        IAmazonCloudFormation CloudFormationClient { get; }
        IAmazonS3 S3Client { get; }

        public PublishServerlessApplicationWorker(ILambdaFunctionUploadHelpers helpers, IAmazonS3 s3Client, IAmazonCloudFormation cloudFormationClient,
            PublishServerlessApplicationWorkerSettings settings)
        {
            this.Helpers = helpers;
            this.S3Client = s3Client;
            this.CloudFormationClient = cloudFormationClient;
            this.Settings = settings;
        }

        public void Publish()
        {
            var logger = new UploadNETCoreWorker.DeployToolLogger(Helpers);

            var lambdaDeploymentMetrics =
                new LambdaDeploymentMetrics(LambdaDeploymentMetrics.LambdaPublishMethod.Serverless,
                    Settings.Framework);

            try
            {
                var command = new DeployServerlessCommand(logger, Settings.SourcePath, new string[0]);
                command.DisableInteractive = true;
                command.S3Client = this.S3Client;
                command.CloudFormationClient = this.CloudFormationClient;
                command.WaitForStackToComplete = false;
                command.Profile = Settings.Account.DisplayName;
                command.Region = Settings.Region.SystemName;

                command.CloudFormationTemplate = this.Settings.Template;
                command.TemplateParameters = this.Settings.TemplateParameters;
                command.StackName = this.Settings.StackName;
                command.TargetFramework = this.Settings.Framework;
                command.Configuration = this.Settings.Configuration;
                command.S3Bucket = this.Settings.S3Bucket;
                command.PersistConfigFile = this.Settings.SaveSettings;

                if (command.ExecuteAsync().Result)
                {
                    this.Helpers.PublishServerlessAsyncCompleteSuccess(this.Settings);
                }
                else
                {
                    if (command.LastToolsException != null)
                    {
                        throw command.LastToolsException;
                    }
                    else
                    {
                        throw new ToolkitException("Failed to publish AWS Serverless application",
                            ToolkitException.CommonErrorCode.UnexpectedError);
                    }
                }
            }
            catch (ToolsException e)
            {
                lambdaDeploymentMetrics.QueueDeploymentFailure(e.Code, e.ServiceCode);
                this.Helpers.UploadFunctionAsyncCompleteError("Error publishing AWS Serverless application");
            }
            catch (ToolkitException e)
            {
                logger.WriteLine(e.Message);
                lambdaDeploymentMetrics.QueueDeploymentFailure(e.Code, e.ServiceErrorCode, e.ServiceStatusCode);
                this.Helpers.UploadFunctionAsyncCompleteError("Error publishing AWS Serverless application");
            }
            catch (Exception e)
            {
                logger.WriteLine(e.Message);
                LOGGER.Error("Error publishing AWS Serverless application.", e);
                lambdaDeploymentMetrics.QueueDeploymentFailure(ToolkitException.CommonErrorCode.UnexpectedError.ToString(), null);
                this.Helpers.UploadFunctionAsyncCompleteError("Error publishing AWS Serverless application");
            }
        }
    }

    public class PublishServerlessApplicationWorkerSettings
    {
        public AccountViewModel Account { get; set; }
        public RegionEndPointsManager.RegionEndPoints Region { get; set; }

        public string SourcePath { get; set; }
        public string Configuration { get; set; }
        public string Framework { get; set; }
        public string Template { get; set; }
        public Dictionary<string,string> TemplateParameters { get; set; }
        public string StackName { get; set; }
        public string S3Bucket { get; set; }
        public bool SaveSettings { get; set; }
    }
}
