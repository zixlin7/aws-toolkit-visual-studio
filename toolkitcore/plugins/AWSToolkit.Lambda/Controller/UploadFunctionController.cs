﻿using System;
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
using Amazon.AWSToolkit.Lambda.WizardPages.PageUI;
using Amazon.AWSToolkit.Regions;
using Amazon.ECR;
using Amazon.Runtime;

namespace Amazon.AWSToolkit.Lambda.Controller
{
    public class UploadFunctionController : BaseContextCommand
    {
        public enum DeploymentType { NETCore, Generic};

        public enum UploadOriginator { FromSourcePath, FromAWSExplorer, FromFunctionView };

        ILog LOGGER = LogManager.GetLogger(typeof(UploadFunctionController));

        public const string ZIP_FILTER = @"-\.njsproj$;-\.sln$;-\.suo$;-.ntvs_analysis\.dat;-\.git;-\.svn;-_testdriver.js;-_sampleEvent\.json";

        ActionResults _results;
        private readonly ToolkitContext _toolkitContext;

        public UploadFunctionController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public override ActionResults Execute(IViewModel model)
        {
            var seedValues = new Dictionary<string, object>();

            seedValues[UploadFunctionWizardProperties.UploadOriginator] = UploadOriginator.FromAWSExplorer;
            seedValues[UploadFunctionWizardProperties.DeploymentType] = DeploymentType.NETCore;

            DisplayUploadWizard(seedValues);
            return _results;
        }

        public ActionResults Execute(IAmazonLambda lambdaClient, IAmazonECR ecrClient, string functionName)
        {
            var response = lambdaClient.GetFunctionConfiguration(functionName);

            var seedValues = new Dictionary<string, object>();

            seedValues[UploadFunctionWizardProperties.LambdaClient] = lambdaClient;
            seedValues[UploadFunctionWizardProperties.ECRClient] = ecrClient;

            seedValues[UploadFunctionWizardProperties.UploadOriginator] = UploadOriginator.FromFunctionView;
            if (response.Runtime.Value.StartsWith("netcore", StringComparison.OrdinalIgnoreCase))
                seedValues[UploadFunctionWizardProperties.DeploymentType] = DeploymentType.NETCore;
            else
                seedValues[UploadFunctionWizardProperties.DeploymentType] = DeploymentType.Generic;

            seedValues[UploadFunctionWizardProperties.FunctionName] = functionName;
            seedValues[UploadFunctionWizardProperties.Role] = response.Role;
            seedValues[UploadFunctionWizardProperties.MemorySize] = response.MemorySize.ToString();
            seedValues[UploadFunctionWizardProperties.Timeout] = response.Timeout.ToString();
            seedValues[UploadFunctionWizardProperties.Description] = response.Description;
            seedValues[UploadFunctionWizardProperties.Runtime] = response.Runtime;

            DisplayUploadWizard(seedValues);
            return _results;
        }

        public ActionResults UploadFunctionFromPath(Dictionary<string, object> seedValues)
        {
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

                    if (File.Exists(serverlessTemplatePath) || !string.IsNullOrEmpty(defaults.CloudFormationTemplate))
                    {
                        string templateFile;
                        // If there is a template specified in the defaults then use that as way for a customer to use a template besides the hard coded serverless.template
                        if (!string.IsNullOrEmpty(defaults.CloudFormationTemplate))
                            templateFile = Path.Combine(sourcePath, defaults.CloudFormationTemplate);
                        else
                            templateFile = serverlessTemplatePath;

                        seedValues[UploadFunctionWizardProperties.CloudFormationTemplate] = templateFile;

                        if (defaults.CloudFormationTemplateParameters != null)
                            seedValues[UploadFunctionWizardProperties.CloudFormationParameters] = defaults.CloudFormationTemplateParameters;

                        try
                        {
                            var wrapper = CloudFormationTemplateWrapper.FromLocalFile(templateFile);
                            if (wrapper.ContainsUserVisibleParameters)
                            {
                                // All the template parameters in the defaults file to the template
                                if(defaults.CloudFormationTemplateParameters?.Count > 0)
                                {
                                    foreach (var kvp in defaults.CloudFormationTemplateParameters)
                                    {
                                        var parameter = wrapper.Parameters.Values.FirstOrDefault(x => string.Equals(kvp.Key, x.Name, StringComparison.Ordinal));
                                        if(parameter != null)
                                        {
                                            parameter.OverrideValue = kvp.Value;
                                        }
                                    }
                                }

                                seedValues[UploadFunctionWizardProperties.CloudFormationTemplateWrapper] = wrapper;
                            }
                        }
                        catch(Exception e)
                        {
                            ToolkitFactory.Instance.ShellProvider.ShowMessage("Error parsing CloudFormation Template", $"Error parsing CloudFormation Template {templateFile}: {e.Message}");
                            LOGGER.Error($"Error parsing template {templateFile}", e);
                            return _results;
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

                        DisplayServerlessWizard(seedValues);
                    }
                    else
                    {
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
                            foreach(var kvp in defaults.EnvironmentVariables)
                            {
                                envs.Add(new EnvironmentVariable { Variable = kvp.Key, Value = kvp.Value });
                            }
                            seedValues[UploadFunctionWizardProperties.EnvironmentVariables] = envs;
                        }

                        DisplayUploadWizard(seedValues);
                    }
                }
                catch (LambdaToolsException e)
                {
                    ToolkitFactory.Instance.ShellProvider.ShowMessage("Error", e.Message);
                }
            }



            return _results;
        }

        private void DisplayUploadWizard(Dictionary<string, object> seedProperties)
        {
            var navigator = ToolkitFactory.Instance.Navigator;

            IAWSWizard wizard = AWSWizardFactory.CreateStandardWizard("Amazon.AWSToolkit.Lambda.UploadFunction", seedProperties);
            wizard.Title = "Upload to AWS Lambda";
            wizard.SetSelectedAccount(navigator.SelectedAccount, UploadFunctionWizardProperties.UserAccount);
            wizard.SetSelectedRegion(navigator.SelectedRegion, UploadFunctionWizardProperties.Region);

            IAWSWizardPageController[] defaultPages = new IAWSWizardPageController[]
            {
                new UploadFunctionDetailsPageController(_toolkitContext),
                new UploadFunctionAdvancedPageController(),
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
            var success = wizard.IsPropertySet(UploadFunctionWizardProperties.WizardResult) && (bool)wizard[UploadFunctionWizardProperties.WizardResult];
            _results = new ActionResults().WithSuccess(success);
        }

        private void DisplayServerlessWizard(Dictionary<string, object> seedProperties)
        {
            var navigator = ToolkitFactory.Instance.Navigator;

            IAWSWizard wizard = AWSWizardFactory.CreateStandardWizard("Amazon.AWSToolkit.Lambda.PublishServerless", seedProperties);
            wizard.Title = "Publish AWS Serverless Application";
            wizard.SetSelectedAccount(navigator.SelectedAccount, UploadFunctionWizardProperties.UserAccount);
            wizard.SetSelectedRegion(navigator.SelectedRegion, UploadFunctionWizardProperties.Region);

            IAWSWizardPageController[] defaultPages = new IAWSWizardPageController[]
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
            var success = wizard.IsPropertySet(UploadFunctionWizardProperties.WizardResult) && (bool)wizard[UploadFunctionWizardProperties.WizardResult];
            _results = new ActionResults().WithSuccess(success);
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
