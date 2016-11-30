using System;
using System.IO;
using System.Threading;
using Amazon.AWSToolkit.Navigator;

using Amazon.Lambda;
using Amazon.Lambda.Model;

using ICSharpCode.SharpZipLib.Zip;

using log4net;
using static Amazon.AWSToolkit.Lambda.Controller.UploadFunctionController;
using Amazon.AWSToolkit.MobileAnalytics;

namespace Amazon.AWSToolkit.Lambda.DeploymentWorkers
{
    public class UploadGenericWorker : BaseUploadWorker
    {
        ILog LOGGER = LogManager.GetLogger(typeof(UploadGenericWorker));        

        public UploadGenericWorker(ILambdaFunctionUploadHelpers functionUploader, IAmazonLambda lambdaClient)
            :base(functionUploader, lambdaClient)
        {
        }

        public const string ZIP_FILTER = @"-\.njsproj$;-\.sln$;-\.suo$;-.ntvs_analysis\.dat;-\.git;-\.svn;-_testdriver.js;-_sampleEvent\.json";

        public override void UploadFunction(UploadFunctionState uploadState)
        {
            string zipFile = null;
            bool deleteZipWhenDone = false;
            try
            {
                if (uploadState.SelectedRole != null)
                {
                    uploadState.Request.Role = uploadState.SelectedRole.Arn;
                }
                else
                {
                    uploadState.Request.Role = this.CreateRole(uploadState);
                }

                bool isSourceDir = (File.GetAttributes(uploadState.SourcePath) & FileAttributes.Directory)
                     == FileAttributes.Directory;

                zipFile = Path.GetTempFileName() + ".zip";
                if (isSourceDir)
                {
                    deleteZipWhenDone = true;
                    var zip = new FastZip();
                    this.FunctionUploader.AppendUploadStatus("Starting to zip up {0}", uploadState.SourcePath);
                    zip.CreateZip(zipFile, uploadState.SourcePath, true, ZIP_FILTER);
                    this.FunctionUploader.AppendUploadStatus("Finished zipping up directory to {0} with file size {1} bytes.", zipFile, new FileInfo(zipFile).Length);
                }
                else if (string.Equals(Path.GetExtension(uploadState.SourcePath), ".zip", StringComparison.InvariantCultureIgnoreCase))
                {
                    zipFile = uploadState.SourcePath;
                    this.FunctionUploader.AppendUploadStatus("Selected source is a zip archive.");
                }
                else
                {
                    deleteZipWhenDone = true;
                    var zip = new FastZip();
                    zip.CreateZip(zipFile, Path.GetDirectoryName(uploadState.SourcePath), false, Path.GetFileName(uploadState.SourcePath));
                    this.FunctionUploader.AppendUploadStatus("Zipped up file {0}", uploadState.SourcePath);
                }

                this.FunctionUploader.AppendUploadStatus("Starting upload of Lambda function zip file.");

                int lastReportedPercent = 0;
                uploadState.Request.StreamTransferProgress += (System.EventHandler<Amazon.Runtime.StreamTransferProgressArgs>)((s, args) =>
                {
                    if (args.PercentDone != lastReportedPercent)
                    {
                        this.FunctionUploader.AppendUploadStatus("Uploaded {0}%", args.PercentDone);
                        lastReportedPercent = args.PercentDone;
                    }
                });

                var existingConfiguration = this.FunctionUploader.GetExistingConfiguration(this.LambdaClient, uploadState.Request.FunctionName);
                // Add retry logic in case the new IAM role has not propagated yet.
                const int MAX_RETRIES = 10;
                for (int i = 0; true; i++)
                {
                    try
                    {
                        using (var stream = File.OpenRead(zipFile))
                        {
                            using (var ms = new MemoryStream())
                            {
                                stream.CopyTo(ms);
                                ms.Position = 0;

                                if (existingConfiguration == null)
                                {
                                    uploadState.Request.Code = new FunctionCode { ZipFile = ms };
                                    this.LambdaClient.CreateFunction(uploadState.Request);
                                    break;
                                }
                                else
                                {
                                    // Check to see if the config for the function changed, if so update the config.
                                    if (!string.Equals(uploadState.Request.Description, existingConfiguration.Description) ||
                                        !string.Equals(uploadState.Request.Handler, existingConfiguration.Handler) ||
                                        uploadState.Request.MemorySize != existingConfiguration.MemorySize ||
                                        !string.Equals(uploadState.Request.Role, existingConfiguration.Role) ||
                                        uploadState.Request.Timeout != existingConfiguration.Timeout ||
                                        uploadState.Request.Environment != null )
                                    {
                                        var uploadConfigRequest = new UpdateFunctionConfigurationRequest
                                        {
                                            Description = uploadState.Request.Description,
                                            FunctionName = uploadState.Request.FunctionName,
                                            Handler = uploadState.Request.Handler,
                                            MemorySize = uploadState.Request.MemorySize,
                                            Role = uploadState.Request.Role,
                                            Timeout = uploadState.Request.Timeout,
                                            Environment = uploadState.Request.Environment
                                        };
                                        this.LambdaClient.UpdateFunctionConfiguration(uploadConfigRequest);
                                    }

                                    var uploadCodeRequest = new UpdateFunctionCodeRequest
                                    {
                                        FunctionName = uploadState.Request.FunctionName,
                                        ZipFile = ms
                                    };
                                    this.LambdaClient.UpdateFunctionCode(uploadCodeRequest);
                                    break;
                                }
                            }
                        }
                    }
                    catch (AmazonLambdaException e)
                    {
                        if (uploadState.SelectedRole == null &&
                            e.Message.Contains(LambdaConstants.ERROR_MESSAGE_CANT_BE_ASSUMED))
                        {
                            if (i == MAX_RETRIES)
                                throw;

                            this.FunctionUploader.AppendUploadStatus("New IAM role has not been propagated, retry attempt {0}.", i + 1);
                            Thread.Sleep(1000);
                        }
                        else
                            throw;
                    }
                }

                ToolkitEvent evnt = new ToolkitEvent();
                evnt.AddProperty(AttributeKeys.LambdaFunctionDeploymentSuccess, uploadState.Request.Runtime);
                SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);

                this.FunctionUploader.AppendUploadStatus("Upload complete.");

                this.Results = new ActionResults { Success = true, ShouldRefresh = true, FocalName = uploadState.Request.FunctionName };

                this.FunctionUploader.UploadFunctionAsyncCompleteSuccess(uploadState);
            }
            catch (Exception e)
            {
                ToolkitEvent evnt = new ToolkitEvent();
                evnt.AddProperty(AttributeKeys.LambdaFunctionDeploymentError, uploadState.Request.Runtime);
                SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);

                LOGGER.Error("Error uploading Lambda function.", e);
                this.FunctionUploader.UploadFunctionAsyncCompleteError("Error uploading Lamdba function: " + e.Message);
            }
            finally
            {
                if (zipFile != null && deleteZipWhenDone)
                    File.Delete(zipFile);
            }
        }
    }
}
