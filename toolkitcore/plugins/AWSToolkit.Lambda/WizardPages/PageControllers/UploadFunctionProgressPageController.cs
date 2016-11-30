using Amazon.AWSToolkit;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.Components;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.Lambda.Controller;
using Amazon.AWSToolkit.Lambda.Nodes;
using Amazon.AWSToolkit.Lambda.WizardPages.PageUI;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.S3;
using Amazon.CloudFormation;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Amazon.AWSToolkit.CloudFormation.Nodes;
using static Amazon.AWSToolkit.Lambda.Controller.UploadFunctionController;
using Amazon.AWSToolkit.Lambda.DeploymentWorkers;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;

namespace Amazon.AWSToolkit.Lambda.WizardPages.PageControllers
{
    public class UploadFunctionProgressPageController : IAWSWizardPageController, ILambdaFunctionUploadHelpers
    {
        public enum Mode { Lamdba, Serverless }
        ILog LOGGER = LogManager.GetLogger(typeof(UploadFunctionProgressPageController));

        private UploadFunctionProgressPage _pageUI;

        public Mode PublishMode { get; }

        public UploadFunctionProgressPageController(Mode publishMode)
        {
            this.PublishMode = publishMode;
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

        public string PageGroup
        {
            get { return AWSWizardConstants.DefaultPageGroup; }
        }

        public string PageID
        {
            get { return GetType().FullName; }
        }

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

        public string ShortPageTitle
        {
            get { return null; }
        }

        public bool AllowShortCircuit()
        {
            return true;
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

            var account = HostingWizard[UploadFunctionWizardProperties.UserAccount] as AccountViewModel;
            var region = HostingWizard[UploadFunctionWizardProperties.Region] as RegionEndPointsManager.RegionEndPoints;

            IAmazonCloudFormation cloudFormationClient = account.CreateServiceClient<AmazonCloudFormationClient>(region.GetEndpoint(RegionEndPointsManager.CLOUDFORMATION_SERVICE_NAME));
            IAmazonS3 s3Client = account.CreateServiceClient<AmazonS3Client>(region.GetEndpoint(RegionEndPointsManager.S3_SERVICE_NAME));


            var settings = new PublishServerlessApplicationWorkerSettings();
            settings.Account = account;
            settings.Region = region;
            settings.SourcePath = HostingWizard[UploadFunctionWizardProperties.SourcePath] as string;
            settings.Configuration = HostingWizard[UploadFunctionWizardProperties.Configuration] as string;
            settings.Framework = HostingWizard[UploadFunctionWizardProperties.Framework] as string;
            settings.Template = HostingWizard[UploadFunctionWizardProperties.CloudFormationTemplate] as string;
            settings.StackName = HostingWizard[UploadFunctionWizardProperties.StackName] as string;
            settings.S3Bucket = HostingWizard[UploadFunctionWizardProperties.S3Bucket] as string;

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


            var worker = new PublishServerlessApplicationWorker(this, s3Client, cloudFormationClient, settings);

            ThreadPool.QueueUserWorkItem(x =>
            {

                worker.Publish();
                //this._results = worker.Results;
            }, null);
        }

        public void UploadFunction()
        {
            _pageUI.StartProgressBar();

            var account = HostingWizard[UploadFunctionWizardProperties.UserAccount] as AccountViewModel;
            var region = HostingWizard[UploadFunctionWizardProperties.Region] as RegionEndPointsManager.RegionEndPoints;

            var runtime = HostingWizard[UploadFunctionWizardProperties.Runtime] as string;
            var functionName = HostingWizard[UploadFunctionWizardProperties.FunctionName] as string;
            var description = HostingWizard[UploadFunctionWizardProperties.Description] as string;
            var configuration = HostingWizard[UploadFunctionWizardProperties.Configuration] as string;
            var framework = HostingWizard[UploadFunctionWizardProperties.Framework] as string;

            var memorySize = (int)HostingWizard[UploadFunctionWizardProperties.MemorySize];
            var timeout = (int)HostingWizard[UploadFunctionWizardProperties.Timeout];
            var handler = HostingWizard[UploadFunctionWizardProperties.Handler] as string;
            var sourcePath = HostingWizard[UploadFunctionWizardProperties.SourcePath] as string;
            var selectedRole = HostingWizard[UploadFunctionWizardProperties.Role] as Role;
            var selectedManagedPolicy = HostingWizard[UploadFunctionWizardProperties.ManagedPolicy] as ManagedPolicy;

            var subnets = HostingWizard[UploadFunctionWizardProperties.Subnets] as IEnumerable<SubnetWrapper>;
            var securityGroups = HostingWizard[UploadFunctionWizardProperties.SecurityGroups] as IEnumerable<SecurityGroupWrapper>;

            var environmentVariables = HostingWizard[UploadFunctionWizardProperties.EnvironmentVariables] as ICollection<EnvironmentVariable>;

            var originator = (UploadOriginator)HostingWizard[UploadFunctionWizardProperties.UploadOriginator];

            var request = new CreateFunctionRequest
            {
                Runtime = runtime,
                FunctionName = functionName,
                Description = description,
                MemorySize = memorySize,
                Timeout = timeout,
                Handler = handler
            };

            if(environmentVariables != null)
            {
                request.Environment = new Amazon.Lambda.Model.Environment
                {
                    Variables = new Dictionary<string, string>()
                };

                foreach(var env in environmentVariables)
                {
                    request.Environment.Variables[env.Variable] = env.Value;
                }
            }

            if (subnets != null && subnets.Any())
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

            var state = new UploadFunctionState
            {
                Account = account,
                Region = region,
                SourcePath = sourcePath,
                Request = request,
                OpenView = _pageUI.OpenView,
                SelectedRole = selectedRole,
                SelectedManagedPolicy = selectedManagedPolicy,
                Configuration = configuration,
                Framework = framework
            };

            IAmazonLambda lambdaClient;
            if (originator == UploadOriginator.FromFunctionView)
                lambdaClient = HostingWizard[UploadFunctionWizardProperties.LambdaClient] as IAmazonLambda;
            else
                lambdaClient = state.Account.CreateServiceClient<AmazonLambdaClient>(state.Region.GetEndpoint(RegionEndPointsManager.LAMBDA_SERVICE_NAME));

            BaseUploadWorker worker;

            if (DetermineDeploymentType(state.SourcePath) == DeploymentType.NETCore)
                worker = new UploadNETCoreWorker(this, lambdaClient);
            else
                worker = new UploadGenericWorker(this, lambdaClient);

            ThreadPool.QueueUserWorkItem(x =>
            {
                var uploadState = state as UploadFunctionState;

                if (uploadState == null)
                    return;

                worker.UploadFunction(uploadState);
                //this._results = worker.Results;
            }, state);
        }

        string ILambdaFunctionUploadHelpers.CreateRole(AccountViewModel account, RegionEndPointsManager.RegionEndPoints region, string functionName, ManagedPolicy managedPolicy)
        {
            var iamClient = account.CreateServiceClient<AmazonIdentityManagementServiceClient>(region);

            var newRole = LambdaUtilities.CreateRole(iamClient, "lambda_exec_" + functionName, LambdaConstants.LAMBDA_ASSUME_ROLE_POLICY);

            (this as ILambdaFunctionUploadHelpers).AppendUploadStatus("Created IAM Role {0}", newRole.RoleName);

            if(managedPolicy != null)
            {
                iamClient.AttachRolePolicy(new AttachRolePolicyRequest
                {
                    RoleName = newRole.RoleName,
                    PolicyArn = managedPolicy.Arn
                });
                (this as ILambdaFunctionUploadHelpers).AppendUploadStatus("Attach policy {0} to role {1}", managedPolicy.PolicyName, newRole.RoleName);
            }

            return newRole.Arn;
        }

        void ILambdaFunctionUploadHelpers.PublishServerlessAsyncCompleteSuccess(PublishServerlessApplicationWorkerSettings settings)
        {
            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke((Action)(() =>
            {
                (this as UploadFunctionProgressPageController)._pageUI.StopProgressBar();

                var navigator = ToolkitFactory.Instance.Navigator;
                if (navigator.SelectedAccount != settings.Account)
                    navigator.UpdateAccountSelection(new Guid(settings.Account.SettingsUniqueKey), false);
                if (navigator.SelectedRegionEndPoints != settings.Region)
                    navigator.UpdateRegionSelection(settings.Region);

                var cloudFormationNode = settings.Account.Children.FirstOrDefault(x => x is ICloudFormationRootViewModel);
                if(cloudFormationNode != null)
                {
                    cloudFormationNode.Refresh(false);

                    var funcNode = cloudFormationNode.Children.FirstOrDefault(x => x.Name == settings.StackName) as ICloudFormationStackViewModel;
                    if (funcNode != null)
                    {
                        var metaNode = funcNode.MetaNode as ICloudFormationStackViewMetaNode;
                        metaNode.OnOpen(funcNode);
                    }
                }
                
                HostingWizard[UploadFunctionWizardProperties.WizardResult] = true;
                if (_pageUI.AutoCloseWizard)
                    HostingWizard.CancelRun();
            }));
        }

        void ILambdaFunctionUploadHelpers.UploadFunctionAsyncCompleteSuccess(UploadFunctionState uploadState)
        {
            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke((Action)(() =>
            {
                (this as UploadFunctionProgressPageController)._pageUI.StopProgressBar();

                var navigator = ToolkitFactory.Instance.Navigator;
                if (navigator.SelectedAccount != uploadState.Account)
                    navigator.UpdateAccountSelection(new Guid(uploadState.Account.SettingsUniqueKey), false);
                if (navigator.SelectedRegionEndPoints != uploadState.Region)
                    navigator.UpdateRegionSelection(uploadState.Region);

                var lambdaNode = uploadState.Account.FindSingleChild<LambdaRootViewModel>(false);
                lambdaNode.Refresh(false);

                var originator = (UploadOriginator)HostingWizard[UploadFunctionWizardProperties.UploadOriginator];
                if (_pageUI.OpenView && originator != UploadOriginator.FromFunctionView)
                {
                    var funcNode = lambdaNode.Children.FirstOrDefault(x => x.Name == uploadState.Request.FunctionName) as LambdaFunctionViewModel;
                    if (funcNode != null)
                    {
                        var metaNode = funcNode.MetaNode as LambdaFunctionViewMetaNode;
                        metaNode.OnOpen(funcNode);
                    }
                }

                HostingWizard[UploadFunctionWizardProperties.WizardResult] = true;
                if (_pageUI.AutoCloseWizard)
                    HostingWizard.CancelRun();
            }));
        }

        void ILambdaFunctionUploadHelpers.UploadFunctionAsyncCompleteError(string message)
        {
            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke((Action)(() =>
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
            string formattedMessage = string.Format(message, tokens);
            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke((Action)(() =>
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
    }
}
