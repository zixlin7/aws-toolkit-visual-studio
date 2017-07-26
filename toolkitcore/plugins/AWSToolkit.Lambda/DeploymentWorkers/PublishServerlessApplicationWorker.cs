using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon.S3;
using Amazon.CloudFormation;

using log4net;

using Amazon.Lambda.Tools;
using Amazon.Lambda.Tools.Commands;

using Amazon.AWSToolkit.MobileAnalytics;
using Amazon.AWSToolkit.Account;
using System.IO;

namespace Amazon.AWSToolkit.Lambda.DeploymentWorkers
{
    public class PublishServerlessApplicationWorker
    {
        const string MOBILEANALYTICS_TYPE = "Serverless";

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
                    ToolkitEvent evnt = new ToolkitEvent();
                    evnt.AddProperty(AttributeKeys.LambdaFunctionDeploymentSuccess, MOBILEANALYTICS_TYPE);

                    var zipArchivePath = Path.Combine(Settings.SourcePath, "bin", Settings.Configuration, Settings.Framework, new DirectoryInfo(Settings.SourcePath).Name + ".zip");
                    if (File.Exists(zipArchivePath))
                    {
                        long size = new FileInfo(zipArchivePath).Length;
                        evnt.AddProperty(MetricKeys.LambdaDeploymentBundleSize, size);
                    }

                    SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);

                    this.Helpers.PublishServerlessAsyncCompleteSuccess(this.Settings);
                }
                else
                {
                    ToolkitEvent evnt = new ToolkitEvent();
                    evnt.AddProperty(AttributeKeys.LambdaFunctionDeploymentError, MOBILEANALYTICS_TYPE);
                    if (command.LastToolsException != null)
                    {
                        if (string.IsNullOrEmpty(command.LastToolsException.ServiceCode))
                            evnt.AddProperty(AttributeKeys.LambdaFunctionDeploymentErrorDetail, $"{command.LastToolsException.Code}");
                        else
                            evnt.AddProperty(AttributeKeys.LambdaFunctionDeploymentErrorDetail, $"{command.LastToolsException.Code}-{command.LastToolsException.ServiceCode}");
                    }
                    SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);

                    this.Helpers.UploadFunctionAsyncCompleteError("Error publishing AWS Serverless application");
                }

            }
            catch (Exception e)
            {
                ToolkitEvent evnt = new ToolkitEvent();
                evnt.AddProperty(AttributeKeys.LambdaFunctionDeploymentError, MOBILEANALYTICS_TYPE);

                SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);

                LOGGER.Error("Error publishing AWS Serverless application.", e);
                this.Helpers.UploadFunctionAsyncCompleteError("Error publishing AWS Serverless application: " + e.Message);
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
