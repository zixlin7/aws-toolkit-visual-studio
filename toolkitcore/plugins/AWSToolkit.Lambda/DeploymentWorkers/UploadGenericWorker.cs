using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Lambda.Util;
using Amazon.AWSToolkit.Navigator;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Util;
using Amazon.ECR;
using Amazon.Lambda;
using Amazon.Lambda.Model;

using log4net;
using static Amazon.AWSToolkit.Lambda.Controller.UploadFunctionController;
using Amazon.IdentityManagement.Model;
using Amazon.Runtime;

namespace Amazon.AWSToolkit.Lambda.DeploymentWorkers
{
    public class UploadGenericWorker : BaseUploadWorker
    {
        ILog LOGGER = LogManager.GetLogger(typeof(UploadGenericWorker));

        public static readonly Regex[] ExcludedFiles =
        {
            new Regex(@"^.*\.njsproj$", RegexOptions.IgnoreCase),
            new Regex(@"^.*\.sln$", RegexOptions.IgnoreCase),
            new Regex(@"^.*\.suo$", RegexOptions.IgnoreCase),
            new Regex(@"^.*ntvs[_]analysis\.dat$", RegexOptions.IgnoreCase),
            new Regex(@"^\.git", RegexOptions.IgnoreCase),
            new Regex(@"^\.svn", RegexOptions.IgnoreCase),
            new Regex(@"^[_]testdriver\.js$", RegexOptions.IgnoreCase),
            new Regex(@"^[_]sampleEvent\.json$", RegexOptions.IgnoreCase),
        };

        private readonly ITelemetryLogger _telemetryLogger;

        public UploadGenericWorker(ILambdaFunctionUploadHelpers functionUploader,
            IAmazonLambda lambdaClient,
            IAmazonECR ecrClient,
            ITelemetryLogger telemetryLogger)
            : base(functionUploader, lambdaClient, ecrClient)
        {
            _telemetryLogger = telemetryLogger;
        }

        public override void UploadFunction(UploadFunctionState uploadState)
        {
            string zipFile = null;
            bool deleteZipWhenDone = false;
            var deploymentProperties = new LambdaTelemetryUtils.RecordLambdaDeployProperties();

            try
            {
                deploymentProperties.RegionId = uploadState.Region?.SystemName;
                deploymentProperties.Runtime = uploadState.Request?.Runtime;
                deploymentProperties.TargetFramework = uploadState.Framework;
                deploymentProperties.LambdaPackageType = uploadState.Request?.PackageType;

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
                    // Zip up a folder
                    deleteZipWhenDone = true;
                    var zipContents = GetProjectFilesToUpload(uploadState.SourcePath)
                        .ToDictionary(
                            file => file,
                            file => file.Substring(uploadState.SourcePath.Length + 1)
                        );

                    this.FunctionUploader.AppendUploadStatus("Starting to zip up {0}", uploadState.SourcePath);
                    ZipUtil.CreateZip(zipFile, zipContents);
                    this.FunctionUploader.AppendUploadStatus("Finished zipping up directory to {0} with file size {1} bytes.", zipFile, new FileInfo(zipFile).Length);
                }
                else if (string.Equals(Path.GetExtension(uploadState.SourcePath), ".zip", StringComparison.InvariantCultureIgnoreCase))
                {
                    // Use provided Zip file
                    zipFile = uploadState.SourcePath;
                    this.FunctionUploader.AppendUploadStatus("Selected source is a zip archive.");
                }
                else
                {
                    // Add single file to Zip
                    deleteZipWhenDone = true;
                    var zipContents = new Dictionary<string, string>()
                    {
                        {uploadState.SourcePath, Path.GetFileName(uploadState.SourcePath)}
                    };
                    ZipUtil.CreateZip(zipFile, zipContents);
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
                deploymentProperties.NewResource = existingConfiguration == null;

                // Add retry logic in case the new IAM role has not propagated yet.
                const int MAX_RETRIES = 10;
                bool publishLambdaSuccess = false;
                for (int i = 0; !publishLambdaSuccess; i++)
                {
                    using (var stream = File.OpenRead(zipFile))
                    {
                        using (var ms = new MemoryStream())
                        {
                            stream.CopyTo(ms);
                            ms.Position = 0;

                            publishLambdaSuccess = CreateOrUpdateFunction(uploadState, existingConfiguration, ms, i,
                                MAX_RETRIES);
                        }
                    }
                }

                _telemetryLogger.RecordLambdaDeploy(Result.Succeeded, deploymentProperties);

                this.FunctionUploader.AppendUploadStatus("Upload complete.");

                this.Results = new ActionResults { Success = true, ShouldRefresh = true, FocalName = uploadState.Request.FunctionName };

                this.FunctionUploader.UploadFunctionAsyncCompleteSuccess(uploadState);
            }
            catch (ToolkitException e)
            {
                this.FunctionUploader.AppendUploadStatus(e.Message);
                this.FunctionUploader.AppendUploadStatus("Upload stopped.");

                _telemetryLogger.RecordLambdaDeploy(Result.Failed, deploymentProperties);
                this.FunctionUploader.UploadFunctionAsyncCompleteError("Error uploading Lambda function");
            }
            catch (Exception e)
            {
                string serviceCode = null;
                var serviceException = e as AmazonServiceException;
                if (serviceException != null)
                {
                    serviceCode = $"{serviceException.ErrorCode}-{serviceException.StatusCode}";
                    this.FunctionUploader.AppendUploadStatus("{0} - {1}", serviceException.ErrorCode, serviceException.StatusCode);
                }

                this.FunctionUploader.AppendUploadStatus(e.Message);
                this.FunctionUploader.AppendUploadStatus("Upload stopped.");

                LOGGER.Error("Error uploading Lambda function.", e);

                _telemetryLogger.RecordLambdaDeploy(Result.Failed, deploymentProperties);
                this.FunctionUploader.UploadFunctionAsyncCompleteError("Error uploading Lambda function");
            }
            finally
            {
                if (zipFile != null && deleteZipWhenDone)
                    File.Delete(zipFile);
            }
        }

        public static IList<string> GetProjectFilesToUpload(string projectFolder)
        {
            return Directory.GetFiles(projectFolder, "*.*", SearchOption.AllDirectories)
                .Where(file =>
                {
                    var relativePath = file.Substring(projectFolder.Length + 1);
                    return !ExcludedFiles.Any(regex => regex.IsMatch(relativePath));
                })
                .ToList();
        }

        /// <summary>
        /// Creates a Lambda Function, and if it already exists, Updates it.
        ///
        /// An exception is thrown if an issue arises that isn't related to waiting on IAM
        /// Permission propagation, or if we have reached the retry attempt limit.
        /// </summary>
        /// <returns>
        /// true: Lambda Creation/Updating was successful
        /// false: Creation/Updating was not successful, but caller may want to try again
        /// </returns>
        private bool CreateOrUpdateFunction(UploadFunctionState uploadState,
            GetFunctionConfigurationResponse existingConfiguration, MemoryStream lambdaCodeStream, int attempt,
            int maxAttempts)
        {
            if (existingConfiguration == null)
            {
                uploadState.Request.Code = new FunctionCode {ZipFile = lambdaCodeStream};
                try
                {
                    this.LambdaClient.CreateFunction(uploadState.Request);
                    return true;
                }
                catch (Exception e)
                {
                    if (WaitForIamRolePropagation(uploadState.SelectedRole, e, attempt, maxAttempts))
                    {
                        return false;
                    }

                    throw new LambdaToolkitException(e.Message,
                        LambdaToolkitException.LambdaErrorCode.LambdaCreateFunction, e);
                }
            }
            else
            {
                // Check to see if the config for the function changed, if so update the config.
                if (!string.Equals(uploadState.Request.Description, existingConfiguration.Description) ||
                    !string.Equals(uploadState.Request.Handler, existingConfiguration.Handler) ||
                    uploadState.Request.MemorySize != existingConfiguration.MemorySize ||
                    !string.Equals(uploadState.Request.Role, existingConfiguration.Role) ||
                    uploadState.Request.Timeout != existingConfiguration.Timeout ||
                    uploadState.Request.Environment != null)
                {
                    try
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
                    catch (Exception e)
                    {
                        if (WaitForIamRolePropagation(uploadState.SelectedRole, e, attempt, maxAttempts))
                        {
                            return false;
                        }

                        throw new LambdaToolkitException(e.Message,
                            LambdaToolkitException.LambdaErrorCode.LambdaUpdateFunctionConfig, e);
                    }
                }

                try
                {
                    var uploadCodeRequest = new UpdateFunctionCodeRequest
                    {
                        FunctionName = uploadState.Request.FunctionName,
                        ZipFile = lambdaCodeStream
                    };
                    this.LambdaClient.UpdateFunctionCode(uploadCodeRequest);
                }
                catch (Exception e)
                {
                    if (WaitForIamRolePropagation(uploadState.SelectedRole, e, attempt, maxAttempts))
                    {
                        return false;
                    }

                    throw new LambdaToolkitException(e.Message,
                        LambdaToolkitException.LambdaErrorCode.LambdaUpdateFunctionCode, e);
                }
            }

            return true;
        }

        /// <summary>
        /// Determines if the exception is related to IAM Permissions, and if we are interested in waiting to see if the permissions
        /// will update.
        /// </summary>
        /// <returns>
        /// true: we will delay a while, and the caller should make another attempt at their operation
        /// false: the exception is not of interest, or we have made enough attempts. The caller should deal with the exception.
        /// </returns>
        private bool WaitForIamRolePropagation(Role lambdaFunctionRole, Exception exception, int attempt, int maxAttempts)
        {
            if (lambdaFunctionRole == null &&
                exception.Message.Contains(LambdaConstants.ERROR_MESSAGE_CANT_BE_ASSUMED) &&
                attempt < maxAttempts)
            {
                this.FunctionUploader.AppendUploadStatus("New IAM role has not been propagated, retry attempt {0}.", attempt + 1);
                Thread.Sleep(1000);
                return true;
            }

            return false;
        }

    }
}
