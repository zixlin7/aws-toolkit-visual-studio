using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.CommonUI.Components;

using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;

using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.AWSToolkit.Lambda.View;

using ICSharpCode.SharpZipLib.Zip;

using log4net;

namespace Amazon.AWSToolkit.Lambda.Controller
{
    public class UploadFunctionController : BaseContextCommand
    {
        public enum UploadMode { FromSourcePath, FromAWSExplorer, FromFunctionView };

        ILog LOGGER = LogManager.GetLogger(typeof(UploadFunctionController));

        public const string ZIP_FILTER = @"-\.njsproj$;-\.sln$;-\.suo$;-.ntvs_analysis\.dat;-\.git;-\.svn;-_testdriver.js;-_sampleEvent\.json";

        UploadFunctionControl _control;
        ActionResults _results;
        IAmazonLambda _seededLambdaClient;

        public override ActionResults Execute(IViewModel model)
        {
            var seedValues = new Dictionary<string, string>();
            this.Mode = UploadMode.FromAWSExplorer;

            this._control = new UploadFunctionControl(this, seedValues);
            ToolkitFactory.Instance.ShellProvider.ShowModal(this._control);

            if (this._results == null)
                return new ActionResults().WithSuccess(false);
            return this._results;
        }

        public ActionResults Execute(IAmazonLambda lambdaClient, string functionName)
        {
            this._seededLambdaClient = lambdaClient;
            this.Mode = UploadMode.FromFunctionView;
            var response = lambdaClient.GetFunctionConfiguration(functionName);

            var seedValues = new Dictionary<string, string>();
            seedValues[LambdaContants.SeedFunctionName] = functionName;
            seedValues[LambdaContants.SeedIAMRole] = response.Role;
            seedValues[LambdaContants.SeedMemory] = response.MemorySize.ToString();
            seedValues[LambdaContants.SeedTimeout] = response.Timeout.ToString();
            seedValues[LambdaContants.SeedDescription] = response.Description;
            seedValues[LambdaContants.SeedRuntime] = response.Runtime;

            this._control = new UploadFunctionControl(this, seedValues);
            ToolkitFactory.Instance.ShellProvider.ShowModal(this._control);

            if (this._results == null)
                return new ActionResults().WithSuccess(false);
            return this._results;
        }

        public ActionResults UploadFunctionFromPath(Dictionary<string, string> seedValues)
        {
            this.Mode = UploadMode.FromSourcePath;

            this._control = new UploadFunctionControl(this, seedValues);
            ToolkitFactory.Instance.ShellProvider.ShowModal(this._control);

            if (this._results == null)
                return new ActionResults().WithSuccess(false);
            return this._results;

        }

        public UploadMode Mode
        {
            get;
            private set;
        }

        public void UploadFunction(AccountViewModel account, RegionEndPointsManager.RegionEndPoints region)
        {
            var request = new CreateFunctionRequest
            {
                FunctionName = this._control.FunctionName,
                Description = this._control.Description,
                MemorySize = this._control.Memory,
                Timeout = this._control.Timeout,
                Runtime = this._control.Runtime,
                Handler = Path.GetFileNameWithoutExtension(this._control.FileName) + "." + this._control.Handler
            };

            var state = new UploadFunctionState
            {
                Account = account,
                Region = region,
                SourcePath = this._control.SourcePath,
                Request = request,
                OpenView = this._control.OpenView,
                SelectedRole = this._control.IAMPicker.SelectedRole,
                SelectedPolicyTemplates = this._control.IAMPicker.SelectedPolicyTemplates
            };

            ThreadPool.QueueUserWorkItem(new WaitCallback(this.UploadFunctionAsync), state);
        }

        public class UploadFunctionState
        {
            public AccountViewModel Account { get; set; }
            public RegionEndPointsManager.RegionEndPoints Region { get; set; }
            public string SourcePath { get; set; }
            public CreateFunctionRequest Request { get; set; }
            public bool OpenView { get; set; }
            public IAMCapabilityPicker.IAMEntity SelectedRole { get; set; }
            public IAMCapabilityPicker.PolicyTemplate[] SelectedPolicyTemplates { get; set; }
        }

        void UploadFunctionAsync(object state)
        {
            var uploadState = state as UploadFunctionState;

            if (uploadState == null)
                return;

            string zipFile = null;
            bool deleteZipWhenDone = false;
            try
            {
                IAmazonLambda lambdaClient;
                if (this.Mode == UploadMode.FromFunctionView)
                    lambdaClient = this._seededLambdaClient;
                else
                    lambdaClient = uploadState.Account.CreateServiceClient<AmazonLambdaClient>(uploadState.Region);

                if (uploadState.SelectedRole != null)
                {
                    uploadState.Request.Role = uploadState.SelectedRole.Arn;
                }
                else
                {
                    uploadState.Request.Role = this.CreateRole(uploadState.Account, uploadState.Region, uploadState.Request.FunctionName, uploadState.SelectedPolicyTemplates);                    
                }

                bool isSourceDir = (File.GetAttributes(uploadState.SourcePath) & FileAttributes.Directory)
                     == FileAttributes.Directory;

                zipFile = Path.GetTempFileName() + ".zip";
                if (isSourceDir)
                {
                    deleteZipWhenDone = true;
                    var zip = new FastZip();
                    AppendUploadStatus("Starting to zip up {0}", uploadState.SourcePath);
                    zip.CreateZip(zipFile, uploadState.SourcePath, true, ZIP_FILTER);
                    AppendUploadStatus("Finished zipping up directory to {0} with file size {1}.", zipFile, new FileInfo(zipFile).Length);
                }
                else if (string.Equals(Path.GetExtension(uploadState.SourcePath), ".zip", StringComparison.InvariantCultureIgnoreCase))
                {
                    zipFile = uploadState.SourcePath;
                    AppendUploadStatus("Selected source is a zip archive.");
                }
                else
                {
                    deleteZipWhenDone = true;
                    var zip = new FastZip();
                    zip.CreateZip(zipFile, Path.GetDirectoryName(uploadState.SourcePath), false, Path.GetFileName(uploadState.SourcePath));
                    AppendUploadStatus("Zipped up file {0}", uploadState.SourcePath);
                }

                AppendUploadStatus("Start upload lambda function zip file.");

                int lastReportedPercent = 0;
                uploadState.Request.StreamTransferProgress += (System.EventHandler<Amazon.Runtime.StreamTransferProgressArgs>)((s, args) =>
                    {
                        if(args.PercentDone != lastReportedPercent)
                        {
                            AppendUploadStatus("Uploaded {0}%", args.PercentDone);
                            lastReportedPercent = args.PercentDone;
                        }
                    });

                var existingConfiguration = this.GetExistingConfiguration(lambdaClient, uploadState.Request.FunctionName);
                // Add retry logic in case the new IAM role has not propagated yet.
                const int MAX_RETRIES = 10;
                for (int i = 0; true ; i++)
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
                                    lambdaClient.CreateFunction(uploadState.Request);
                                    break;
                                }
                                else
                                {
                                    // Check to see if the config for the function changed, if so update the config.
                                    if (!string.Equals(uploadState.Request.Description, existingConfiguration.Description) ||
                                        !string.Equals(uploadState.Request.Handler, existingConfiguration.Handler) ||
                                        uploadState.Request.MemorySize != existingConfiguration.MemorySize ||
                                        !string.Equals(uploadState.Request.Role, existingConfiguration.Role) ||
                                        !string.Equals(uploadState.Request.Runtime.ToString(), existingConfiguration.Runtime.ToString()) ||
                                        uploadState.Request.Timeout != existingConfiguration.Timeout)
                                    {
                                        var uploadConfigRequest = new UpdateFunctionConfigurationRequest
                                        {
                                            Description = uploadState.Request.Description,
                                            FunctionName = uploadState.Request.FunctionName,
                                            Handler = uploadState.Request.Handler,
                                            MemorySize = uploadState.Request.MemorySize,
                                            Role = uploadState.Request.Role,
                                            Timeout = uploadState.Request.Timeout,
                                            Runtime = uploadState.Request.Runtime
                                        };
                                        lambdaClient.UpdateFunctionConfiguration(uploadConfigRequest);
                                    }

                                    var uploadCodeRequest = new UpdateFunctionCodeRequest
                                    {
                                        FunctionName = uploadState.Request.FunctionName,
                                        ZipFile = ms
                                    };
                                    lambdaClient.UpdateFunctionCode(uploadCodeRequest);
                                    break;
                                }
                            }
                        }
                    }
                    catch(AmazonLambdaException e)
                    {
                        if (uploadState.SelectedRole == null &&
                            string.Equals(e.Message, LambdaContants.ERROR_MESSAGE_FOR_TASK_CANT_ASSUMED, StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (i == MAX_RETRIES)
                                throw;

                            AppendUploadStatus("New IAM role has not been propagated, retry attempt {0}.", i + 1);
                            Thread.Sleep(1000);
                        }
                        else
                            throw;
                    }
                }

                AppendUploadStatus("Upload complete.");

                this._results = new ActionResults { Success = true, ShouldRefresh = true, FocalName = uploadState.Request.FunctionName };

                this._control.UploadFunctionAsyncCompleteSuccess(uploadState);
            }
            catch(Exception e)
            {
                LOGGER.Error("Error uploading lambda function.", e);
                this._control.UploadFunctionAsyncCompleteError("Error uploading Lamdba function: " + e.Message);
            }
            finally
            {
                if (zipFile != null && deleteZipWhenDone)
                    File.Delete(zipFile);
            }
        }

        private GetFunctionConfigurationResponse GetExistingConfiguration(IAmazonLambda lambdaClient, string functionName)
        {
            try
            {
                var response = lambdaClient.GetFunctionConfiguration(functionName);
                return response;
            }
            catch(AmazonLambdaException)
            {
                return null;
            }
        }

        private void AppendUploadStatus(string message, params object[] tokens)
        {
            string formattedMessage = string.Format(message, tokens);
            this._control.ReportOnUploadStatus(formattedMessage);
        }

        private string CreateRole(AccountViewModel account, RegionEndPointsManager.RegionEndPoints region, string functionName, IAMCapabilityPicker.PolicyTemplate[] roleTemplates)
        {
            var iamClient = account.CreateServiceClient<AmazonIdentityManagementServiceClient>(region);

            var newRole = LambdaUtilities.CreateRole(iamClient, "lambda_exec_" + functionName, LambdaContants.LAMBDA_ASSUME_ROLE_POLICY);

            AppendUploadStatus("Created IAM Role {0}", newRole.Arn);

            foreach (var template in roleTemplates)
            {
                iamClient.PutRolePolicy(new PutRolePolicyRequest
                {
                    RoleName = newRole.RoleName,
                    PolicyName = template.IAMCompatibleName,
                    PolicyDocument = template.Body.Trim()
                });
                AppendUploadStatus("Creating policy \"{0}\" on role {1}.", template.Name, newRole.RoleName);
            }

            return newRole.Arn;
        }
    }
}
