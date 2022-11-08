using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Controls;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CloudFormation.Nodes;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Lambda.DeploymentWorkers;
using Amazon.AWSToolkit.Lambda.Nodes;
using Amazon.AWSToolkit.Lambda.Util;
using Amazon.AWSToolkit.Lambda.WizardPages.PageUI;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Regions;
using Amazon.CloudFormation;
using Amazon.ECR;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.S3;
using Amazon.SecurityToken;

using log4net;

using static Amazon.AWSToolkit.Lambda.Controller.UploadFunctionController;

namespace Amazon.AWSToolkit.Lambda.WizardPages.PageControllers
{
    public class UploadFunctionProgressPageController : IAWSWizardPageController, ILambdaFunctionUploadHelpers
    {
        public enum Mode { Lambda, Serverless }
        ILog LOGGER = LogManager.GetLogger(typeof(UploadFunctionProgressPageController));

        private readonly ToolkitContext _toolkitContext;
        private UploadFunctionProgressPage _pageUI;

        public Mode PublishMode { get; }

        public UploadFunctionProgressPageController(Mode publishMode, ToolkitContext toolkitContext)
        {
            this.PublishMode = publishMode;
            _toolkitContext = toolkitContext;
        }

        public IAWSWizard HostingWizard { get; set; }

        public string PageDescription
        {
            get
            {
                if (this.PublishMode == Mode.Serverless)
                {
                    return "Please wait while we publish your AWS Serverless application.";
                }

                return "Please wait while we upload your function to AWS Lambda.";
            }
        }

        public string PageGroup => AWSWizardConstants.DefaultPageGroup;

        public string PageID => GetType().FullName;

        public string PageTitle
        {
            get
            {
                if(this.PublishMode == Mode.Serverless)
                {
                    return "Publish AWS Serverless Application";
                }
                return "Uploading Function";
            }
        }

        public string ShortPageTitle => null;

        public bool AllowShortCircuit()
        {
            return true;
        }

        public void ResetPage()
        {

        }


        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            // we'll re-enable Back if an error occurs. Cancel (aka Close) will enable if we have
            // a successful upload and the 'auto close wizard' option has been unchecked.

            // Wizard framework currently disallows changes to this button
            //HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Cancel, false);

            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Back, false);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, false);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, false);

            if(!string.IsNullOrEmpty(HostingWizard[UploadFunctionWizardProperties.CloudFormationTemplate] as string))
                PublishServerlessApplication();
            else
                UploadFunction();
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new UploadFunctionProgressPage(this);
            }

            return _pageUI;
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (navigationReason == AWSWizardConstants.NavigationReason.movingBack)
                _pageUI.SetUploadFailedState(false); // toggles back to progress bar for next attenmpt

            return true;
        }

        public bool QueryFinishButtonEnablement()
        {
            // don't stand in the way of our previous sibling pages!
            return true;
        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return true;
        }

        public void TestForwardTransitionEnablement()
        {
        }

        public void PublishServerlessApplication()
        {
            _pageUI.StartProgressBar();

            var account = HostingWizard.GetSelectedAccount(UploadFunctionWizardProperties.UserAccount);
            var region = HostingWizard.GetSelectedRegion(UploadFunctionWizardProperties.Region);


            IAmazonSecurityTokenService stsClient = account.CreateServiceClient<IAmazonSecurityTokenService>(region);
            IAmazonCloudFormation cloudFormationClient = account.CreateServiceClient<AmazonCloudFormationClient>(region);
            IAmazonS3 s3Client = account.CreateServiceClient<AmazonS3Client>(region);
            IAmazonECR ecrClient = account.CreateServiceClient<AmazonECRClient>(region);
            var iamClient = account.CreateServiceClient<AmazonIdentityManagementServiceClient>(region);
            var lambdaClient = account.CreateServiceClient<AmazonLambdaClient>(region);


            var credentials = _toolkitContext.CredentialManager.GetAwsCredentials(account.Identifier, region);
            var settings = new PublishServerlessApplicationWorkerSettings();
            settings.Account = account;
            settings.Credentials = credentials;
            settings.Region = region;
            settings.AccountId = account?.ToolkitContext.ServiceClientManager.GetAccountId(new AwsConnectionSettings(account?.Identifier, region));
            settings.SourcePath = HostingWizard[UploadFunctionWizardProperties.SourcePath] as string;
            settings.Configuration = HostingWizard[UploadFunctionWizardProperties.Configuration] as string;
            settings.Framework = HostingWizard[UploadFunctionWizardProperties.Framework] as string;
            settings.Template = HostingWizard[UploadFunctionWizardProperties.CloudFormationTemplate] as string;
            settings.StackName = HostingWizard[UploadFunctionWizardProperties.StackName] as string;
            settings.S3Bucket = HostingWizard[UploadFunctionWizardProperties.S3Bucket] as string;

            if (HostingWizard[UploadFunctionWizardProperties.SaveSettings] is bool)
            {
                settings.SaveSettings = (bool)HostingWizard[UploadFunctionWizardProperties.SaveSettings];
            }


            var setParamterValues = HostingWizard[UploadFunctionWizardProperties.CloudFormationTemplateParameters] as Dictionary<string, CloudFormationTemplateWrapper.TemplateParameter>;
            if (setParamterValues != null)
            {
                Dictionary<string, string> parameters = new Dictionary<string, string>();
                foreach (var kvp in setParamterValues)
                {
                    parameters[kvp.Key] = kvp.Value.OverrideValue;
                }

                settings.TemplateParameters = parameters;
            }


            var worker = new PublishServerlessApplicationWorker(this,
                stsClient, s3Client, cloudFormationClient, ecrClient,
                iamClient, lambdaClient, settings, _toolkitContext.TelemetryLogger);

            ThreadPool.QueueUserWorkItem(x =>
            {

                worker.Publish();
                //this._results = worker.Results;
            }, null);
        }

        public void UploadFunction()
        {
            try
            {
                _pageUI.StartProgressBar();

                var account = HostingWizard.GetSelectedAccount(UploadFunctionWizardProperties.UserAccount);
                var region = HostingWizard.GetSelectedRegion(UploadFunctionWizardProperties.Region);
                var accountId = account?.ToolkitContext.ServiceClientManager.GetAccountId(new AwsConnectionSettings(account?.Identifier, region));
                var runtime = HostingWizard[UploadFunctionWizardProperties.Runtime] as string;
                var functionName = HostingWizard[UploadFunctionWizardProperties.FunctionName] as string;
                var description = HostingWizard[UploadFunctionWizardProperties.Description] as string;
                var architecture = HostingWizard[UploadFunctionWizardProperties.Architecture] as string;
                var configuration = HostingWizard[UploadFunctionWizardProperties.Configuration] as string;
                var framework = HostingWizard[UploadFunctionWizardProperties.Framework] as string;

                var kmsArn = HostingWizard[UploadFunctionWizardProperties.KMSKey] as string;

                var memorySize = (int) HostingWizard[UploadFunctionWizardProperties.MemorySize];
                var timeout = (int) HostingWizard[UploadFunctionWizardProperties.Timeout];
                var handler = HostingWizard[UploadFunctionWizardProperties.Handler] as string;
                var sourcePath = HostingWizard[UploadFunctionWizardProperties.SourcePath] as string;
                var selectedRole = HostingWizard[UploadFunctionWizardProperties.Role] as Role;
                var selectedManagedPolicy =
                    HostingWizard[UploadFunctionWizardProperties.ManagedPolicy] as ManagedPolicy;

                var selectedDeadLetterTargetArn =
                    HostingWizard[UploadFunctionWizardProperties.DeadLetterTargetArn] as string;
                var selectedTracingMode = HostingWizard[UploadFunctionWizardProperties.TracingMode] as string;

                var subnets = HostingWizard[UploadFunctionWizardProperties.Subnets] as IEnumerable<SubnetWrapper>;
                var securityGroups =
                    HostingWizard[UploadFunctionWizardProperties.SecurityGroups] as IEnumerable<SecurityGroupWrapper>;

                var packageType = HostingWizard[UploadFunctionWizardProperties.PackageType] as PackageType;
                var imageRepo = HostingWizard[UploadFunctionWizardProperties.ImageRepo] as string;
                var imageTag = HostingWizard[UploadFunctionWizardProperties.ImageTag] as string;
                var imageCommand = HostingWizard[UploadFunctionWizardProperties.ImageCommand] as string;
                var dockerfile = HostingWizard[UploadFunctionWizardProperties.Dockerfile] as string;

                var environmentVariables =
                    HostingWizard[UploadFunctionWizardProperties.EnvironmentVariables] as
                        ICollection<EnvironmentVariable>;

                bool saveSettings = false;
                if (HostingWizard[UploadFunctionWizardProperties.SaveSettings] is bool)
                {
                    saveSettings = (bool) HostingWizard[UploadFunctionWizardProperties.SaveSettings];
                }

                var originator = (UploadOriginator) HostingWizard[UploadFunctionWizardProperties.UploadOriginator];
                ImageConfig imageConfig = null;
                var imageCommandList = SplitByComma(imageCommand);
                if (packageType.Equals(Amazon.Lambda.PackageType.Image))
                {
                    imageConfig = new ImageConfig();
                    imageConfig.Command = imageCommandList;
                    imageConfig.IsCommandSet = imageCommandList != null;
                }

                var request = new CreateFunctionRequest
                {
                    Runtime = runtime,
                    Architectures = new List<string> { architecture },
                    FunctionName = functionName,
                    PackageType = packageType,
                    Description = description,
                    MemorySize = memorySize,
                    Timeout = timeout,
                    Handler = handler,
                    KMSKeyArn = kmsArn,
                    ImageConfig = imageConfig
                };

                if (!string.IsNullOrEmpty(selectedDeadLetterTargetArn))
                {
                    request.DeadLetterConfig = new DeadLetterConfig {TargetArn = selectedDeadLetterTargetArn};
                }

                if (!string.IsNullOrEmpty(selectedTracingMode))
                {
                    request.TracingConfig = new TracingConfig {Mode = selectedTracingMode};
                }

                if (environmentVariables != null)
                {
                    request.Environment = new Amazon.Lambda.Model.Environment
                    {
                        Variables = new Dictionary<string, string>()
                    };

                    foreach (var env in environmentVariables)
                    {
                        request.Environment.Variables[env.Variable] = env.Value;
                    }
                }



                if (subnets != null)
                {
                    request.VpcConfig = new VpcConfig();

                    request.VpcConfig.SubnetIds = new List<string>();
                    foreach (var subnet in subnets)
                    {
                        request.VpcConfig.SubnetIds.Add(subnet.SubnetId);
                    }

                    request.VpcConfig.SecurityGroupIds = new List<string>();
                    foreach (var group in securityGroups)
                    {
                        request.VpcConfig.SecurityGroupIds.Add(group.GroupId);
                    }
                }

                var credentials = _toolkitContext.CredentialManager.GetAwsCredentials(account.Identifier, region);
                var state = new UploadFunctionState
                {
                    Account = account,
                    AccountId = accountId,
                    Credentials = credentials,
                    Region = region,
                    SourcePath = sourcePath,
                    SaveSettings = saveSettings,
                    Request = request,
                    OpenView = _pageUI.OpenView,
                    SelectedRole = selectedRole,
                    SelectedManagedPolicy = selectedManagedPolicy,
                    Configuration = configuration,
                    Framework = framework,
                    ImageRepo = imageRepo,
                    ImageTag = imageTag,
                    Dockerfile = dockerfile
                };

                IAmazonECR ecrClient;
                IAmazonLambda lambdaClient;
                if (originator == UploadOriginator.FromFunctionView)
                {
                    lambdaClient = HostingWizard[UploadFunctionWizardProperties.LambdaClient] as IAmazonLambda;
                    ecrClient = HostingWizard[UploadFunctionWizardProperties.ECRClient] as IAmazonECR;
                }
                else
                {
                    lambdaClient = state.Account.CreateServiceClient<AmazonLambdaClient>(state.Region);
                    ecrClient = state.Account.CreateServiceClient<AmazonECRClient>(state.Region);
                }

                IAmazonSecurityTokenService stsClient =
                    state.Account.CreateServiceClient<IAmazonSecurityTokenService>(state.Region);

                BaseUploadWorker worker;

                if (DetermineDeploymentType(state.SourcePath) == DeploymentType.NETCore)
                {
                    var iamClient =
                        state.Account.CreateServiceClient<AmazonIdentityManagementServiceClient>(state.Region);
                    var s3Client = state.Account.CreateServiceClient<AmazonS3Client>(state.Region);
                    worker = new UploadNETCoreWorker(this, stsClient, lambdaClient, ecrClient, iamClient, s3Client,
                        _toolkitContext.TelemetryLogger);
                }
                else
                {
                    worker = new UploadGenericWorker(this, stsClient, lambdaClient, ecrClient, _toolkitContext);
                }

                ThreadPool.QueueUserWorkItem(x =>
                {
                    var uploadState = state as UploadFunctionState;

                    if (uploadState == null)
                        return;

                    worker.UploadFunction(uploadState);
                    //this._results = worker.Results;
                }, state);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error uploading Lambda function.", e);

                var uploader = this as ILambdaFunctionUploadHelpers;

                var message = $"Error uploading Lambda Function: {e.Message}";

                uploader.AppendUploadStatus(e.Message);
                uploader.AppendUploadStatus("Upload stopped.");
                uploader.UploadFunctionAsyncCompleteError(message);
            }
        }

        string ILambdaFunctionUploadHelpers.CreateRole(AccountViewModel account, ToolkitRegion region, string functionName, ManagedPolicy managedPolicy)
        {
            var iamClient = account.CreateServiceClient<AmazonIdentityManagementServiceClient>(region);
            Role newRole = null;

            try
            {
                newRole = IAMUtilities.CreateRole(iamClient, "lambda_exec_" + functionName, LambdaConstants.LAMBDA_ASSUME_ROLE_POLICY);
            }
            catch (Exception e)
            {
                throw new IamToolkitException($"Error creating IAM Role: {e.Message}", IamToolkitException.IamErrorCode.IamCreateRole, e);
            }

            (this as ILambdaFunctionUploadHelpers).AppendUploadStatus("Created IAM Role {0}", newRole.RoleName);

            if (managedPolicy != null)
            {
                try
                {
                    iamClient.AttachRolePolicy(new AttachRolePolicyRequest
                    {
                        RoleName = newRole.RoleName,
                        PolicyArn = managedPolicy.Arn
                    });
                }
                catch (Exception e)
                {
                    throw new IamToolkitException($"Error attaching IAM Role Policy: {e.Message}", IamToolkitException.IamErrorCode.IamAttachRolePolicy, e);
                }

                (this as ILambdaFunctionUploadHelpers).AppendUploadStatus("Attached policy {0} to role {1}", managedPolicy.PolicyName, newRole.RoleName);
            }

            return newRole.Arn;
        }

        void ILambdaFunctionUploadHelpers.PublishServerlessAsyncCompleteSuccess(PublishServerlessApplicationWorkerSettings settings)
        {
            PostDeploymentAnalysis(settings.SaveSettings);
            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
            {
                (this as UploadFunctionProgressPageController)._pageUI.StopProgressBar();
                HostingWizard[UploadFunctionWizardProperties.WizardResult] = true;
                if (_pageUI.AutoCloseWizard && !_pageUI.IsUnloaded)
                    HostingWizard.CancelRun();
            }));
            var navigator = ToolkitFactory.Instance.Navigator;
            //sync up navigator connection settings with the deployment settings and check if they have been validated
            var isConnectionValid = navigator.TryWaitForSelection(_toolkitContext.ConnectionManager, settings.Account, settings.Region);
           
            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action) (() =>
            {
                if (!isConnectionValid)
                {
                    ToolkitFactory.Instance.ShellProvider.OutputToHostConsole("Serverless application has been successfully deployed. You can view it under AWS CloudFormation.");
                }
                else
                {
                    var cloudFormationNode =
                        settings.Account.Children.FirstOrDefault(x => x is ICloudFormationRootViewModel);
                    if (cloudFormationNode != null)
                    {
                        cloudFormationNode.Refresh(false);

                        var funcNode =
                            cloudFormationNode.Children.FirstOrDefault(x => x.Name == settings.StackName) as
                                ICloudFormationStackViewModel;
                        if (funcNode != null)
                        {
                            var metaNode = funcNode.MetaNode as ICloudFormationStackViewMetaNode;
                            metaNode.OnOpen(funcNode);
                        }
                    }
                }
            }));
        }

        void ILambdaFunctionUploadHelpers.UploadFunctionAsyncCompleteSuccess(UploadFunctionState uploadState)
        {
            PostDeploymentAnalysis(uploadState.SaveSettings);
            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
            {
                (this as UploadFunctionProgressPageController)._pageUI.StopProgressBar();
                HostingWizard[UploadFunctionWizardProperties.WizardResult] = true;
                if (_pageUI.AutoCloseWizard && !_pageUI.IsUnloaded)
                    HostingWizard.CancelRun();
            }));

            var navigator = ToolkitFactory.Instance.Navigator;
            //sync up navigator connection settings with the deployment settings and check if they have been validated
            var isConnectionValid = navigator.TryWaitForSelection(_toolkitContext.ConnectionManager, uploadState.Account, uploadState.Region);
            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action) (() =>
            {
                if (!isConnectionValid)
                {
                    ToolkitFactory.Instance.ShellProvider.OutputToHostConsole("Lambda function has been successfully deployed. You can view it under AWS Lambda.");
                }
                else
                {
                    var lambdaNode = navigator.SelectedAccount.FindSingleChild<LambdaRootViewModel>(false);
                    if (lambdaNode != null)
                    {
                        lambdaNode.Refresh(false);

                        var originator = (UploadOriginator) HostingWizard[UploadFunctionWizardProperties.UploadOriginator];
                        if (_pageUI.OpenView && originator != UploadOriginator.FromFunctionView)
                        {
                            var funcNode = lambdaNode.Children.FirstOrDefault(x => x.Name == uploadState.Request.FunctionName) as LambdaFunctionViewModel;
                            if (funcNode != null)
                            {
                                var metaNode = funcNode.MetaNode as LambdaFunctionViewMetaNode;
                                metaNode.OnOpen(funcNode);
                            }
                        }
                    }
                }
            }));
        }

        private void PostDeploymentAnalysis(bool persist)
        {
            if (!(HostingWizard[UploadFunctionWizardProperties.SelectedProjectFile] is string projectFile))
                return;

            if (persist)
            {
                Utility.AddDotnetCliToolReference(projectFile, "Amazon.Lambda.Tools");
            }
        }

        bool ILambdaFunctionUploadHelpers.XRayEnabled()
        {
            if (!(HostingWizard[UploadFunctionWizardProperties.SelectedProjectFile] is string projectFile))
                return false;

            var projectContent = File.ReadAllText(projectFile);
            return projectContent.Contains("AWSXRayRecorder");
        }

        string ILambdaFunctionUploadHelpers.GetFunctionLanguage()
        {
            if (!(HostingWizard[UploadFunctionWizardProperties.SelectedProjectFile] is string projectFile))
                return null;

            var projectExtension = Path.GetExtension(projectFile);

            if (string.Equals(projectExtension, ".csproj", StringComparison.OrdinalIgnoreCase))
            {
                return "C#";
            }
            else if (string.Equals(projectExtension, ".fsproj", StringComparison.OrdinalIgnoreCase))
            {
                return "F#";
            }

            return null;
        }

        void ILambdaFunctionUploadHelpers.UploadFunctionAsyncCompleteError(string message)
        {
            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
            {
                (this as UploadFunctionProgressPageController)._pageUI.StopProgressBar();
                (this as UploadFunctionProgressPageController)._pageUI.SetUploadFailedState(true);

                ToolkitFactory.Instance.ShellProvider.ShowError("Error Uploading", message);

                // wizard framework doesn't allow this one to be changed currently
                // HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Cancel, true);

                HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Back, true);
                HostingWizard[UploadFunctionWizardProperties.WizardResult] = false;

            }));
        }

        void ILambdaFunctionUploadHelpers.AppendUploadStatus(string message, params object[] tokens)
        {
            string formattedMessage;
            try
            {
                formattedMessage = tokens.Length == 0 ? message : string.Format(message, tokens);
            }
            catch
            {
                formattedMessage = message;
            }
            
            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
            {
                (this as UploadFunctionProgressPageController)._pageUI.OutputProgressMessage(formattedMessage);
                ToolkitFactory.Instance.ShellProvider.UpdateStatus(formattedMessage);
                ToolkitFactory.Instance.ShellProvider.OutputToHostConsole(formattedMessage, true);
            }));
        }

        GetFunctionConfigurationResponse ILambdaFunctionUploadHelpers.GetExistingConfiguration(IAmazonLambda lambdaClient, string functionName)
        {
            return GetExistingConfiguration(lambdaClient, functionName);
        }

        private List<string> SplitByComma(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            return text.Split(new char[] { ',' }, StringSplitOptions.None)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();
        }

        void ILambdaFunctionUploadHelpers.WaitForUpdatableState(IAmazonLambda lambdaClient, string functionName)
        {
            var uploader = this as ILambdaFunctionUploadHelpers;
            uploader.AppendUploadStatus("Waiting for function state to be updatable...");

            _toolkitContext.ToolkitHost.ExecuteOnUIThread(async () =>
            {
                var waiter = new LambdaStateWaiter(lambdaClient);
                await waiter.WaitForUpdatableStateAsync(functionName);
            });

            uploader.AppendUploadStatus("... Function can now be updated");
        }
    }
}
