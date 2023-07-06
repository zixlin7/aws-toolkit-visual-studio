using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.Lambda;
using Amazon.Lambda.Model;

using log4net;
using Amazon.AWSToolkit.Lambda.WizardPages;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Lambda.WizardPages.PageControllers;
using Amazon.Lambda.Tools;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Lambda.WizardPages.PageUI;
using Amazon.AWSToolkit.Regions;
using Amazon.ECR;
using Amazon.Runtime;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;

using Amazon.AWSToolkit.Telemetry;
using Amazon.AWSToolkit.Telemetry.Model;
using Amazon.AWSToolkit.Lambda.Util;
using Amazon.AWSToolkit.Exceptions;

namespace Amazon.AWSToolkit.Lambda.Controller
{
    public class UploadFunctionController : BaseContextCommand
    {
        public enum DeploymentType { NETCore, Generic};

        public enum UploadOriginator { FromSourcePath, FromAWSExplorer, FromFunctionView };

        private static readonly ILog _logger = LogManager.GetLogger(typeof(UploadFunctionController));

        public const string ZIP_FILTER = @"-\.njsproj$;-\.sln$;-\.suo$;-.ntvs_analysis\.dat;-\.git;-\.svn;-_testdriver.js;-_sampleEvent\.json";

        ActionResults _results;
        private readonly ToolkitContext _toolkitContext;
        private readonly ICredentialIdentifier _credentialIdentifier;
        private readonly ToolkitRegion _region;
        private readonly AWSViewModel _awsViewModel;

        /// <param name="toolkitContext">Core Toolkit functionality</param>
        /// <param name="credentialIdentifier">The credentials the upload operation was requested against</param>
        /// <param name="region">The region the upload operation was requested against</param>
        /// <param name="awsViewModel">The Toolkit's collection of valid accounts</param>
        public UploadFunctionController(ToolkitContext toolkitContext,
            ICredentialIdentifier credentialIdentifier, ToolkitRegion region,
            AWSViewModel awsViewModel)
        {
            _toolkitContext = toolkitContext;
            _credentialIdentifier = credentialIdentifier;
            _region = region;
            _awsViewModel = awsViewModel;
        }

        /// <summary>
        /// Called from the AWS Explorer (Lambda node) context menu to deploy a zip or folder containing code
        /// </summary>
        public override ActionResults Execute(IViewModel model)
        {
            ActionResults results = null;

            void Invoke() => results = DeployFromExplorer();

            void Record(ITelemetryLogger telemetryLogger, double duration)
            {
                var metricSource = UploadOriginator.FromAWSExplorer.AsMetricSource();
                RecordLambdaPublishWizard(results, duration, metricSource);
            }

            _toolkitContext.TelemetryLogger.TimeAndRecord(Invoke, Record);
            return results;
        }

        private ActionResults DeployFromExplorer()
        {
            var seedValues = new Dictionary<string, object>();

            seedValues[UploadFunctionWizardProperties.UploadOriginator] = UploadOriginator.FromAWSExplorer;
            seedValues[UploadFunctionWizardProperties.DeploymentType] = DeploymentType.NETCore;

            return DisplayUploadWizard(seedValues);
        }

        /// <summary>
        /// Called from the Lambda function viewer to update code for an existing Lambda function
        /// </summary>
        public ActionResults Execute(IAmazonLambda lambdaClient, IAmazonECR ecrClient, string functionName)
        {
            ActionResults results = null;

            void Invoke()
            {
                try
                {
                    results = DeployFromFunctionView(lambdaClient, ecrClient, functionName);
                }
                catch (Exception e)
                {
                    results = ActionResults.CreateFailed(e);
                }
            }

            void Record(ITelemetryLogger telemetryLogger, double duration)
            {
                var metricSource = UploadOriginator.FromFunctionView.AsMetricSource();
                RecordLambdaPublishWizard(results, duration, metricSource);
            }

            _toolkitContext.TelemetryLogger.TimeAndRecord(Invoke, Record);

            //if an exception occurred re-throw it for handling from the calling function
            if (results?.Exception != null)
            {
                throw results.Exception;
            }
            return results;
        }

        private ActionResults DeployFromFunctionView(IAmazonLambda lambdaClient, IAmazonECR ecrClient, string functionName)
        {
            var response = lambdaClient.GetFunctionConfiguration(functionName);

            var seedValues = new Dictionary<string, object>();

            seedValues[UploadFunctionWizardProperties.LambdaClient] = lambdaClient;
            seedValues[UploadFunctionWizardProperties.ECRClient] = ecrClient;

            seedValues[UploadFunctionWizardProperties.UploadOriginator] = UploadOriginator.FromFunctionView;
            if (response.Runtime.Value.StartsWith("netcore", StringComparison.OrdinalIgnoreCase))
            {
                seedValues[UploadFunctionWizardProperties.DeploymentType] = DeploymentType.NETCore;
            }
            else
            {
                seedValues[UploadFunctionWizardProperties.DeploymentType] = DeploymentType.Generic;
            }

            seedValues[UploadFunctionWizardProperties.FunctionName] = functionName;
            seedValues[UploadFunctionWizardProperties.Role] = response.Role;
            seedValues[UploadFunctionWizardProperties.MemorySize] = response.MemorySize.ToString();
            seedValues[UploadFunctionWizardProperties.Timeout] = response.Timeout.ToString();
            seedValues[UploadFunctionWizardProperties.Description] = response.Description;
            seedValues[UploadFunctionWizardProperties.Runtime] = response.Runtime;

            return DisplayUploadWizard(seedValues);
        }

        /// <summary>
        /// Called from the "Publish to AWS Lambda" context menu in the Solution Explorer.
        /// This is the main Lambda deployment path.
        /// </summary>
        public ActionResults UploadFunctionFromPath(Dictionary<string, object> seedValues)
        {
            ActionResults results = null;
            var isServerless = false;

            void Invoke()
            {
                try
                {
                    results = UploadFunctionFromPath(seedValues, out isServerless);
                }
                catch (Exception e)
                {
                    results = ActionResults.CreateFailed(e);
                }
            }

            void Record(ITelemetryLogger telemetryLogger, double duration)
            {
                var metricSource = UploadOriginator.FromSourcePath.AsMetricSource();
                if(isServerless)
                {
                    RecordServerlessPublishWizard(results, duration, metricSource);
                }
                else
                {
                    RecordLambdaPublishWizard(results, duration, metricSource);
                }
            }

            _toolkitContext.TelemetryLogger.TimeAndRecord(Invoke, Record);

            //if an exception occurred re-throw it for handling from the calling function
            if (results?.Exception != null)
            {
                throw results.Exception;
            }
            return results;
        }


        private ActionResults UploadFunctionFromPath(Dictionary<string, object> seedValues, out bool isServerless)
        {
            isServerless = false;
            string sourcePath = null;
            var deploymentType = DeploymentType.Generic;

            if (seedValues.ContainsKey(UploadFunctionWizardProperties.SourcePath))
            {
                sourcePath = seedValues[UploadFunctionWizardProperties.SourcePath] as string;
                deploymentType = DetermineDeploymentType(sourcePath);


                seedValues[UploadFunctionWizardProperties.UploadOriginator] = UploadOriginator.FromSourcePath;
                seedValues[UploadFunctionWizardProperties.DeploymentType] = deploymentType;

                try
                {
                    var serverlessTemplatePath = Path.Combine(sourcePath, Constants.AWS_SERVERLESS_TEMPLATE_DEFAULT_FILENAME);

                    var defaults = new LambdaToolsDefaults();
                    defaults.LoadDefaults(sourcePath, LambdaToolsDefaults.DEFAULT_FILE_NAME);

                    // Use the account and region from the lambda defaults json file if it is available
                    var account = GetAccount(defaults.Profile);
                    ApplyDefaultAccount(seedValues, account);
                    ApplyDefaultRegion(seedValues, defaults.Region, _toolkitContext.RegionProvider);

                    if (File.Exists(serverlessTemplatePath) || !string.IsNullOrEmpty(defaults.CloudFormationTemplate))
                    {
                        isServerless = true;
                        string templateFile;
                        // If there is a template specified in the defaults then use that as way for a customer to use a template besides the hard coded serverless.template
                        if (!string.IsNullOrEmpty(defaults.CloudFormationTemplate))
                        {
                            templateFile = Path.Combine(sourcePath, defaults.CloudFormationTemplate);
                        }
                        else
                        {
                            templateFile = serverlessTemplatePath;
                        }

                        seedValues[UploadFunctionWizardProperties.CloudFormationTemplate] = templateFile;

                        if (defaults.CloudFormationTemplateParameters != null)
                        {
                            seedValues[UploadFunctionWizardProperties.CloudFormationParameters] = defaults.CloudFormationTemplateParameters;
                        }

                        try
                        {
                            var wrapper = CloudFormationTemplateWrapper.FromLocalFile(templateFile);

                            // this inherently loads and parses the template file
                            // any validation errors encountered would be shown as an error
                            if (wrapper.ContainsUserVisibleParameters)
                            {
                                // All the template parameters in the defaults file to the template
                                if (defaults.CloudFormationTemplateParameters?.Count > 0)
                                {
                                    foreach (var kvp in defaults.CloudFormationTemplateParameters)
                                    {
                                        var parameter = wrapper.Parameters.Values.FirstOrDefault(x => string.Equals(kvp.Key, x.Name, StringComparison.Ordinal));
                                        if (parameter != null)
                                        {
                                            parameter.OverrideValue = kvp.Value;
                                        }
                                    }
                                }

                                seedValues[UploadFunctionWizardProperties.CloudFormationTemplateWrapper] = wrapper;
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.Error($"Error parsing template {templateFile}", e);
                            return ActionResults.CreateFailed(new TemplateToolkitException(e.Message, TemplateToolkitException.TemplateErrorCode.InvalidFormat,
                                e));
                        }


                        if (!string.IsNullOrEmpty(defaults.StackName))
                            seedValues[UploadFunctionWizardProperties.StackName] = defaults.StackName;
                        if (!string.IsNullOrEmpty(defaults.S3Bucket))
                            seedValues[UploadFunctionWizardProperties.S3Bucket] = defaults.S3Bucket;
                        if (!string.IsNullOrEmpty(defaults.S3Prefix))
                            seedValues[UploadFunctionWizardProperties.S3Prefix] = defaults.S3Prefix;
                        if (!string.IsNullOrEmpty(defaults.Configuration))
                            seedValues[UploadFunctionWizardProperties.Configuration] = defaults.Configuration;
                        if (!string.IsNullOrEmpty(defaults.Framework))
                            seedValues[UploadFunctionWizardProperties.Framework] = defaults.Framework;

                        return DisplayServerlessWizard(seedValues);
                    }
                    else
                    {
                        isServerless = false;
                        if (!string.IsNullOrEmpty(defaults.FunctionHandler))
                            seedValues[UploadFunctionWizardProperties.Handler] = defaults.FunctionHandler;
                        if (!string.IsNullOrEmpty(defaults.FunctionName))
                            seedValues[UploadFunctionWizardProperties.FunctionName] = defaults.FunctionName;
                        if (!string.IsNullOrEmpty(defaults.FunctionRole))
                            seedValues[UploadFunctionWizardProperties.Role] = defaults.FunctionRole;
                        if (!string.IsNullOrEmpty(defaults.FunctionArchitecture))
                            seedValues[UploadFunctionWizardProperties.Architecture] = defaults.FunctionArchitecture;
                        if (defaults.FunctionMemory.HasValue)
                            seedValues[UploadFunctionWizardProperties.MemorySize] = defaults.FunctionMemory.Value;
                        if (defaults.FunctionTimeout.HasValue)
                            seedValues[UploadFunctionWizardProperties.Timeout] = defaults.FunctionTimeout.Value;
                        if (defaults.FunctionSubnets != null)
                            seedValues[UploadFunctionWizardProperties.SeedSubnetIds] = defaults.FunctionSubnets;
                        if (defaults.FunctionSecurityGroups != null)
                            seedValues[UploadFunctionWizardProperties.SeedSecurityGroupIds] = defaults.FunctionSecurityGroups;
                        if (!string.IsNullOrEmpty(defaults.KMSKeyArn))
                            seedValues[UploadFunctionWizardProperties.KMSKey] = defaults.KMSKeyArn;
                        if (!string.IsNullOrEmpty(defaults.Configuration))
                            seedValues[UploadFunctionWizardProperties.Configuration] = defaults.Configuration;
                        if (!string.IsNullOrEmpty(defaults.Framework))
                            seedValues[UploadFunctionWizardProperties.Framework] = defaults.Framework;
                        if (!string.IsNullOrEmpty(defaults.FunctionRuntime))
                            seedValues[UploadFunctionWizardProperties.Runtime] = defaults.FunctionRuntime;
                        if (!string.IsNullOrEmpty(defaults.DeadLetterTargetArn))
                            seedValues[UploadFunctionWizardProperties.DeadLetterTargetArn] = defaults.DeadLetterTargetArn;
                        if (!string.IsNullOrEmpty(defaults.TracingMode))
                            seedValues[UploadFunctionWizardProperties.TracingMode] = defaults.TracingMode;
                        if (!string.IsNullOrEmpty(defaults.PackageType))
                            seedValues[UploadFunctionWizardProperties.PackageType] = new PackageType(defaults.PackageType);
                        if (!string.IsNullOrEmpty(defaults.ImageTag))
                            seedValues[UploadFunctionWizardProperties.ImageTag] = defaults.ImageTag;
                        if (!string.IsNullOrEmpty(defaults.ImageRepo))
                            seedValues[UploadFunctionWizardProperties.ImageRepo] = defaults.ImageRepo;
                        if (!string.IsNullOrEmpty(defaults.ImageCommand))
                            seedValues[UploadFunctionWizardProperties.ImageCommand] = defaults.ImageCommand;


                        if (defaults.EnvironmentVariables != null)
                        {
                            var envs = new List<EnvironmentVariable>();
                            foreach (var kvp in defaults.EnvironmentVariables)
                            {
                                envs.Add(new EnvironmentVariable { Variable = kvp.Key, Value = kvp.Value });
                            }
                            seedValues[UploadFunctionWizardProperties.EnvironmentVariables] = envs;
                        }

                        return DisplayUploadWizard(seedValues);
                    }
                }
                catch (LambdaToolsException e)
                {
                    ToolkitFactory.Instance.ShellProvider.ShowMessage("Error", e.Message);
                    return ActionResults.CreateFailed(e);
                }
            }

            return ActionResults.CreateFailed(new LambdaToolkitException("Failed to upload Lambda function from source path", LambdaToolkitException.LambdaErrorCode.NoLambdaSourcePath));
        }

        public static void ApplyDefaultAccount(Dictionary<string, object> seedValues,
            AccountViewModel account)
        {
            if (account != null)
            {
                seedValues[UploadFunctionWizardProperties.UserAccount] = account;
            }
        }

        public static void ApplyDefaultRegion(Dictionary<string, object> seedValues,
            string regionId,
            IRegionProvider regionProvider)
        {
            if (string.IsNullOrEmpty(regionId)) return;

            var region = regionProvider.GetRegion(regionId);
            if (region != null)
            {
                seedValues[UploadFunctionWizardProperties.Region] = region;
            }
        }

        private ActionResults DisplayUploadWizard(Dictionary<string, object> seedProperties)
        {
            var wizard = AWSWizardFactory.CreateStandardWizard("Amazon.AWSToolkit.Lambda.UploadFunction", seedProperties);
            wizard.Title = "Upload to AWS Lambda";

            ApplyConnectionIfMissing(wizard);

            var defaultPages = new IAWSWizardPageController[]
            {
                new UploadFunctionDetailsPageController(_toolkitContext),
                new UploadFunctionAdvancedPageController(_toolkitContext),
                new UploadFunctionProgressPageController(UploadFunctionProgressPageController.Mode.Lambda, _toolkitContext)
            };

            wizard.RegisterPageControllers(defaultPages, 0);
            wizard.SetNavigationButtonText(AWSWizardConstants.NavigationButtons.Finish, "Upload");
            wizard.SetShortCircuitPage(AWSWizardConstants.WizardPageReferences.LastPageID);

            // as the last page of the wizard performs the upload process, and invokes CancelRun
            // to shutdown the UI, we place no stock in the result of Run() and instead will look
            // for a specific property to be true on exit indicating successful upload vs user
            // cancel.
            wizard.Run();
            var success = wizard.IsPropertySet(UploadFunctionWizardProperties.WizardResult) && (bool) wizard[UploadFunctionWizardProperties.WizardResult];

            return success ? new ActionResults().WithSuccess(true) : ActionResults.CreateCancelled();
        }

        private ActionResults DisplayServerlessWizard(Dictionary<string, object> seedProperties)
        {
            var wizard = AWSWizardFactory.CreateStandardWizard("Amazon.AWSToolkit.Lambda.PublishServerless", seedProperties);
            wizard.Title = "Publish AWS Serverless Application";

            ApplyConnectionIfMissing(wizard);

            var defaultPages = new IAWSWizardPageController[]
            {
                new PublishServerlessDetailsPageController(),
                new ServerlessTemplateParametersPageController(),
                new UploadFunctionProgressPageController(UploadFunctionProgressPageController.Mode.Serverless, _toolkitContext)
            };

            wizard.RegisterPageControllers(defaultPages, 0);
            wizard.SetNavigationButtonText(AWSWizardConstants.NavigationButtons.Finish, "Publish");
            wizard.SetShortCircuitPage(AWSWizardConstants.WizardPageReferences.LastPageID);
            // as the last page of the wizard performs the upload process, and invokes CancelRun
            // to shutdown the UI, we place no stock in the result of Run() and instead will look
            // for a specific property to be true on exit indicating successful upload vs user
            // cancel.
            wizard.Run();

            var success = wizard.IsPropertySet(UploadFunctionWizardProperties.WizardResult) && (bool) wizard[UploadFunctionWizardProperties.WizardResult];
          
            return success ? new ActionResults().WithSuccess(true) : ActionResults.CreateCancelled();
        }

        private void RecordServerlessPublishWizard(ActionResults results, double duration, BaseMetricSource metricSource)
        {
            var connectionSettings = new AwsConnectionSettings(_credentialIdentifier, _region);
            var accountId = connectionSettings.GetAccountId(_toolkitContext.ServiceClientManager);
          
            _toolkitContext.TelemetryLogger.RecordServerlessapplicationPublishWizard(new ServerlessapplicationPublishWizard()
            {
                AwsAccount = accountId ?? MetadataValue.Invalid,
                AwsRegion = _region?.Id ?? MetadataValue.Invalid,
                Result = results.AsTelemetryResult(),
                Duration = duration,
                Source = metricSource?.Location,
                ServiceType = metricSource?.Service,
                Reason = LambdaHelpers.GetMetricsReason(results?.Exception)
            });
        }

        private void RecordLambdaPublishWizard(ActionResults results, double duration, BaseMetricSource metricSource)
        {
            var connectionSettings = new AwsConnectionSettings(_credentialIdentifier, _region);
            var accountId = connectionSettings.GetAccountId(_toolkitContext.ServiceClientManager);

            _toolkitContext.TelemetryLogger.RecordLambdaPublishWizard(new LambdaPublishWizard()
            {
                AwsAccount = accountId ?? MetadataValue.Invalid,
                AwsRegion = _region?.Id ?? MetadataValue.Invalid,
                Result = results.AsTelemetryResult() ,
                Duration = duration,
                Source = metricSource?.Location,
                ServiceType = metricSource?.Service,
                Reason = LambdaHelpers.GetMetricsReason(results?.Exception)
            });
        }

        private void ApplyConnectionIfMissing(IAWSWizard wizard)
        {
            var account = GetAccount(_credentialIdentifier);
            ApplyAccountIfMissing(wizard, account);
            ApplyRegionIfMissing(wizard, _region);
        }

        public static void ApplyAccountIfMissing(IAWSWizard wizard, AccountViewModel account)
        {
            if (wizard.GetSelectedAccount(UploadFunctionWizardProperties.UserAccount) == null && account != null)
            {
                wizard.SetSelectedAccount(account, UploadFunctionWizardProperties.UserAccount);
            }
        }

        public static void ApplyRegionIfMissing(IAWSWizard wizard, ToolkitRegion region)
        {
            if (wizard.GetSelectedRegion(UploadFunctionWizardProperties.Region) == null && region != null)
            {
                wizard.SetSelectedRegion(region, UploadFunctionWizardProperties.Region);
            }
        }

        private AccountViewModel GetAccount(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName)) return null;

            return _awsViewModel.RegisteredAccounts.FirstOrDefault(a => a.Identifier?.ProfileName == profileName);
        }

        private AccountViewModel GetAccount(ICredentialIdentifier credentialIdentifier)
        {
            if (credentialIdentifier == null) return null;

            return _awsViewModel.RegisteredAccounts.FirstOrDefault(a => a.Identifier?.Id == credentialIdentifier.Id);
        }

        public static DeploymentType DetermineDeploymentType(string sourcePath)
        {
            if (!Directory.Exists(sourcePath))
                return DeploymentType.Generic;

            var files = Directory.GetFiles(sourcePath);
            if (files.Any(x => x.EndsWith(".csproj") || x.EndsWith(".fsproj") || x.EndsWith(Constants.AWS_SERVERLESS_TEMPLATE_DEFAULT_FILENAME)))
                return DeploymentType.NETCore;

            return DeploymentType.Generic;
        } 

        internal static GetFunctionConfigurationResponse GetExistingConfiguration(IAmazonLambda lambdaClient, string functionName)
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

        public class UploadFunctionState
        {
            public AccountViewModel Account { get; set; }
            public string AccountId { get; set; }
            public AWSCredentials Credentials { get; set; }
            public ToolkitRegion Region { get; set; }
            public string SourcePath { get; set; }
            public CreateFunctionRequest Request { get; set; }
            public bool OpenView { get; set; }
            public Amazon.IdentityManagement.Model.Role SelectedRole { get; set; }
            public Amazon.IdentityManagement.Model.ManagedPolicy SelectedManagedPolicy { get; set; }

            public string Configuration { get; set; }
            public string Framework { get; set; }
            public bool SaveSettings { get; set; }
            public string ImageRepo { get; set; }
            public string ImageTag { get; set; }
            public string Dockerfile { get; set; }

            public List<string> GetRequestArchitectures()
            {
                return Request?.Architectures ?? new List<string>();
            } 
        }
    }
}
