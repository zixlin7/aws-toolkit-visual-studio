using System;
using System.Collections.Generic;
using System.Linq;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Lambda.Util;
using Amazon.AWSToolkit.Regions;
using Amazon.CloudFormation;
using Amazon.Common.DotNetCli.Tools;
using Amazon.ECR;
using Amazon.IdentityManagement;
using Amazon.Lambda;
using Amazon.Lambda.Tools.Commands;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SecurityToken;

using log4net;

namespace Amazon.AWSToolkit.Lambda.DeploymentWorkers
{
    public class PublishServerlessApplicationWorker
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(PublishServerlessApplicationWorker));

        private readonly ITelemetryLogger _telemetryLogger;

        PublishServerlessApplicationWorkerSettings Settings { get; }
        ILambdaFunctionUploadHelpers Helpers { get; }

        IAmazonSecurityTokenService StsClient { get; }
        IAmazonCloudFormation CloudFormationClient { get; }
        IAmazonS3 S3Client { get; }
        IAmazonECR ECRClient { get; }
        private IAmazonIdentityManagementService IamClient { get; }
        private IAmazonLambda LambdaClient { get; }

        public PublishServerlessApplicationWorker(ILambdaFunctionUploadHelpers helpers,
            IAmazonSecurityTokenService stsClient,
            IAmazonS3 s3Client, IAmazonCloudFormation cloudFormationClient, IAmazonECR ecrClient,
            IAmazonIdentityManagementService iamClient, IAmazonLambda lambdaClient,
            PublishServerlessApplicationWorkerSettings settings,
            ITelemetryLogger telemetryLogger)
        {
            this.Helpers = helpers;
            this.StsClient = stsClient;
            this.S3Client = s3Client;
            this.CloudFormationClient = cloudFormationClient;
            this.ECRClient = ecrClient;
            this.IamClient = iamClient;
            this.LambdaClient = lambdaClient;
            this.Settings = settings;
            this._telemetryLogger = telemetryLogger;
        }

        public void Publish()
        {
            var logger = new UploadNETCoreWorker.DeployToolLogger(Helpers);

            try
            {
                var command = new DeployServerlessCommand(logger, Settings.SourcePath, new string[0]);
                command.DisableInteractive = true;
                command.STSClient = StsClient;
                command.S3Client = S3Client;
                command.CloudFormationClient = CloudFormationClient;
                command.ECRClient = ECRClient;
                command.IAMClient = IamClient;
                command.LambdaClient = LambdaClient;
                command.WaitForStackToComplete = false;
                command.Profile = Settings.Account.Identifier.ProfileName;
                command.Credentials = Settings.Credentials;
                command.Region = Settings.Region.Id;

                command.CloudFormationTemplate = this.Settings.Template;
                command.TemplateParameters = this.Settings.TemplateParameters;
                command.StackName = this.Settings.StackName;
                command.TargetFramework = this.Settings.Framework;
                command.Configuration = this.Settings.Configuration;
                command.S3Bucket = this.Settings.S3Bucket;
                command.PersistConfigFile = this.Settings.SaveSettings;

                if (command.ExecuteAsync().Result)
                {
                    _telemetryLogger.RecordServerlessApplicationDeploy(
                        Result.Succeeded,
                        Settings.AccountId,
                        Settings.Region?.Id
                    );

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
                RecordPublishFailure(e);

                this.Helpers.UploadFunctionAsyncCompleteError("Error publishing AWS Serverless application");
            }
            catch (ToolkitException e)
            {
                logger.WriteLine(e.Message);

                RecordPublishFailure(e);

                this.Helpers.UploadFunctionAsyncCompleteError("Error publishing AWS Serverless application");
            }
            catch (Exception e)
            {
                logger.WriteLine(e.Message);
                Logger.Error("Error publishing AWS Serverless application.", e);

                RecordPublishFailure(e);

                this.Helpers.UploadFunctionAsyncCompleteError("Error publishing AWS Serverless application");
            }
        }

        private void RecordPublishFailure(Exception e)
        {
            _telemetryLogger.RecordServerlessApplicationDeploy(
                Result.Failed,
                Settings.AccountId,
                Settings.Region?.Id,
                GetReason(e));
        }

        private string GetReason(Exception e)
        {
            var reasonChunks = new List<string>();

            switch (e)
            {
                case ToolsException toolsException:
                    reasonChunks.Add(toolsException.ServiceCode);
                    reasonChunks.Add(toolsException.Code);
                    break;
                case ToolkitException toolkitException:
                    reasonChunks.Add(toolkitException.ServiceErrorCode);
                    reasonChunks.Add(toolkitException.ServiceStatusCode);
                    reasonChunks.Add(toolkitException.Code);
                    break;
            }

            string reason = string.Join("-", reasonChunks.Where(r => !string.IsNullOrWhiteSpace(r)));

            return string.IsNullOrWhiteSpace(reason) ? "Unknown" : reason;
        }
    }

    public class PublishServerlessApplicationWorkerSettings
    {
        public AccountViewModel Account { get; set; }
        public string AccountId { get; set; }
        public AWSCredentials Credentials { get; set; }
        public ToolkitRegion Region { get; set; }
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
