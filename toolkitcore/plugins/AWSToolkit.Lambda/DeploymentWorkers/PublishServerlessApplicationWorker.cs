using System;
using System.Collections.Generic;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Lambda.Controller;
using Amazon.AWSToolkit.Lambda.Util;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Telemetry;
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
        private readonly UploadFunctionController.UploadOriginator _originator;

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
            UploadFunctionController.UploadOriginator originator,
            ITelemetryLogger telemetryLogger)
        {
            Helpers = helpers;
            StsClient = stsClient;
            S3Client = s3Client;
            CloudFormationClient = cloudFormationClient;
            ECRClient = ecrClient;
            IamClient = iamClient;
            LambdaClient = lambdaClient;
            Settings = settings;
            _originator = originator;
            _telemetryLogger = telemetryLogger;
        }

        public void Publish()
        {
            ActionResults results = null;

            void Invoke() => results = PublishServerless();

            void Record(ITelemetryLogger telemetryLogger, double duration)
            {
                var metricSource = _originator.AsMetricSource();
                _telemetryLogger.RecordServerlessApplicationDeploy(results, duration, metricSource, Settings.AccountId,
                    Settings.Region?.Id);
            }

            _telemetryLogger.TimeAndRecord(Invoke, Record);
        }

        private ActionResults PublishServerless()
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

                command.CloudFormationTemplate = Settings.Template;
                command.TemplateParameters = Settings.TemplateParameters;
                command.StackName = Settings.StackName;
                command.TargetFramework = Settings.Framework;
                command.Configuration = Settings.Configuration;
                command.S3Bucket = Settings.S3Bucket;
                command.PersistConfigFile = Settings.SaveSettings;

                if (command.ExecuteAsync().Result)
                {
                    Helpers.PublishServerlessAsyncCompleteSuccess(Settings);
                    return new ActionResults().WithSuccess(true);
                }
                else
                {
                    if (command.LastException != null)
                    {
                        throw command.LastException;
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
                Helpers.UploadFunctionAsyncCompleteError("Error publishing AWS Serverless application");
                return ActionResults.CreateFailed(e);
            }
            catch (ToolkitException e)
            {
                logger.WriteLine(e.Message);
                Helpers.UploadFunctionAsyncCompleteError("Error publishing AWS Serverless application");
                return ActionResults.CreateFailed(e);
            }
            catch (Exception e)
            {
                logger.WriteLine(e.Message);
                Logger.Error("Error publishing AWS Serverless application.", e);

                Helpers.UploadFunctionAsyncCompleteError("Error publishing AWS Serverless application");
                return ActionResults.CreateFailed(e);
            }
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
        public Dictionary<string, string> TemplateParameters { get; set; }
        public string StackName { get; set; }
        public string S3Bucket { get; set; }
        public bool SaveSettings { get; set; }
    }
}
