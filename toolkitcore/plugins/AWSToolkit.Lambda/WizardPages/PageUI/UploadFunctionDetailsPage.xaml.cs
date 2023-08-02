using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Lambda.Model;
using Amazon.AWSToolkit.Lambda.ViewModel;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Tasks;
using Amazon.AWSToolkit.Util;
using Amazon.Common.DotNetCli.Tools;
using Amazon.ECR;
using Amazon.Lambda;

using log4net;

using static Amazon.AWSToolkit.Lambda.Controller.UploadFunctionController;

namespace Amazon.AWSToolkit.Lambda.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for UploadFunctionDetailsPage.xaml
    /// </summary>
    public partial class UploadFunctionDetailsPage : INotifyPropertyChanged
    {
        private const int AccountRegionChangedDebounceMs = 250;

        static readonly ILog LOGGER = LogManager.GetLogger(typeof(UploadFunctionDetailsPage));
        public static readonly string LambdaServiceName = new AmazonLambdaConfig().RegionEndpointServiceName;

        private static readonly IDictionary<string, RuntimeOption> RuntimeByFramework =
            new Dictionary<string, RuntimeOption>(StringComparer.OrdinalIgnoreCase)
            {
                {Frameworks.Net60, RuntimeOption.DotNet6},
            };

        private static readonly IDictionary<RuntimeOption, string> FrameworkByRuntime =
            new Dictionary<RuntimeOption, string>()
            {
                {RuntimeOption.DotNet6, Frameworks.Net60},
            };

        public IAWSWizardPageController PageController { get; }
        private IAWSToolkitShellProvider _shellProvider;
        private readonly DebounceDispatcher _accountRegionChangeDebounceDispatcher = new DebounceDispatcher();

        public UploadOriginator UploadOriginator { get; }
        public UploadFunctionViewModel ViewModel { get; }

        public UploadFunctionDetailsPage() : this(ToolkitFactory.Instance.ShellProvider, ToolkitFactory.Instance.ToolkitContext)
        {

        }

        public UploadFunctionDetailsPage(IAWSToolkitShellProvider shellProvider, ToolkitContext toolkitContext)
        {
            _shellProvider = shellProvider;
            InitializeComponent();

            ViewModel = new UploadFunctionViewModel(shellProvider, toolkitContext)
            {
                PackageType = Amazon.Lambda.PackageType.Zip
            };

            ViewModel.Connection.SetServiceFilter(new List<string>() {LambdaServiceName});
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            DataContext = this;
        }

        public UploadFunctionDetailsPage(IAWSWizardPageController pageController)
            : this(pageController, ToolkitFactory.Instance.ShellProvider, ToolkitFactory.Instance.ToolkitContext)
        {
        }

        public UploadFunctionDetailsPage(IAWSWizardPageController pageController, 
            IAWSToolkitShellProvider shellProvider,
            ToolkitContext toolkitContext)
            : this(shellProvider, toolkitContext)
        {
            PageController = pageController;
            var hostWizard = PageController.HostingWizard;

            var deploymentType 
                = (DeploymentType)hostWizard.CollectedProperties[UploadFunctionWizardProperties.DeploymentType];
            UploadOriginator
                = (UploadOriginator)hostWizard.CollectedProperties[UploadFunctionWizardProperties.UploadOriginator];

            RuntimeOption.ALL_OPTIONS.ToList().ForEach(r => ViewModel.Runtimes.Add(r));

            SetPanelsForOriginatorAndType();

            ViewModel.ProjectIsExecutable = hostWizard.GetProperty<bool>(UploadFunctionWizardProperties.IsExecutable, false);

            ViewModel.Runtime = deploymentType == DeploymentType.NETCore
                ? RuntimeOption.DotNet6
                : RuntimeOption.NodeJS_v12_X;

            if (ViewModel.Runtime.IsDotNet)
            {
                InitializeNETCoreFields();
            }

            var userAccount = hostWizard.GetSelectedAccount(UploadFunctionWizardProperties.UserAccount);
            var region = hostWizard.GetSelectedRegion(UploadFunctionWizardProperties.Region);

            ViewModel.Connection.Account = userAccount;
            ViewModel.Connection.Region = region;

            var buildConfiguration = hostWizard[UploadFunctionWizardProperties.Configuration] as string;
            if (!string.IsNullOrEmpty(buildConfiguration) && ViewModel.Configurations.Contains(buildConfiguration))
            {
                ViewModel.Configuration = buildConfiguration;
            }

            var targetFramework = hostWizard[UploadFunctionWizardProperties.Framework] as string;
            ViewModel.SetFrameworkIfExists(targetFramework);

            if (UploadOriginator == UploadOriginator.FromSourcePath)
            {
                UpdateExistingFunctions();
            }

            if (hostWizard.IsPropertySet(UploadFunctionWizardProperties.FunctionName))
            {
                ViewModel.FunctionName = hostWizard.CollectedProperties[UploadFunctionWizardProperties.FunctionName] as string;
            }

            if (hostWizard.IsPropertySet(UploadFunctionWizardProperties.Handler))
            {
                ViewModel.Handler = hostWizard.CollectedProperties[UploadFunctionWizardProperties.Handler] as string;
            }

            if (hostWizard.IsPropertySet(UploadFunctionWizardProperties.SourcePath))
            {
                ViewModel.SourceCodeLocation = hostWizard.CollectedProperties[UploadFunctionWizardProperties.SourcePath] as string;
            }

            if (hostWizard.IsPropertySet(UploadFunctionWizardProperties.Description))
            {
                ViewModel.Description = hostWizard.CollectedProperties[UploadFunctionWizardProperties.Description] as string;
            }

            if (hostWizard.IsPropertySet(UploadFunctionWizardProperties.SuggestedMethods))
            {
                PopulateSuggestionCache(
                    hostWizard.CollectedProperties[UploadFunctionWizardProperties.SuggestedMethods] as
                        IDictionary<string, IList<string>>);

                _suggestionCache.Keys
                    .Select(type => new
                    {
                        Type = type,
                        Segments = type.Split('.').Length
                    })
                    // Sort so that files higher up the namespace come first.
                    // Eg: Foo.Bar, then Alpha.Beta.Gamma
                    .OrderBy(x => x.Segments)
                    .ThenBy(x => x.Type)
                    .ToList()
                    .ForEach(x => ViewModel.HandlerTypeSuggestions.Add(x.Type));
            }

            if (hostWizard.IsPropertySet(UploadFunctionWizardProperties.Architecture))
            {
                var architecture = hostWizard[UploadFunctionWizardProperties.Architecture] as string;
                ViewModel.Architecture = ViewModel.GetArchitectureOrDefault(architecture, LambdaArchitecture.X86);
            }

            if (hostWizard.IsPropertySet(UploadFunctionWizardProperties.Runtime))
            {
                var runtime = hostWizard[UploadFunctionWizardProperties.Runtime] as string;
                ViewModel.Runtime = ViewModel.Runtimes.FirstOrDefault(x => x.Value == runtime);
            }

            UpdateImageRepos().LogExceptionAndForget();

            if (hostWizard.IsPropertySet(UploadFunctionWizardProperties.PackageType))
            {
                ViewModel.PackageType =
                    hostWizard.CollectedProperties[UploadFunctionWizardProperties.PackageType] as
                        PackageType ?? Amazon.Lambda.PackageType.Zip;
            }

            if (hostWizard.IsPropertySet(UploadFunctionWizardProperties.Dockerfile))
            {
                ViewModel.Dockerfile = hostWizard.CollectedProperties[UploadFunctionWizardProperties.Dockerfile] as string;
            }

            if (hostWizard.IsPropertySet(UploadFunctionWizardProperties.ImageCommand))
            {
                ViewModel.ImageCommand = hostWizard.CollectedProperties[UploadFunctionWizardProperties.ImageCommand] as string;
            }

            if (hostWizard.IsPropertySet(UploadFunctionWizardProperties.ImageRepo))
            {
                ViewModel.ImageRepo = hostWizard.CollectedProperties[UploadFunctionWizardProperties.ImageRepo] as string;
            }

            if (hostWizard.IsPropertySet(UploadFunctionWizardProperties.ImageTag))
            {
                ViewModel.ImageTag = hostWizard.CollectedProperties[UploadFunctionWizardProperties.ImageTag] as string;
            }

            SetPanelsForDeploymentType(deploymentType);
            ViewModel.SaveSettings = true;
        }

        // todo : rename?
        private void SetPanelsForOriginatorAndType()
        {
            switch (UploadOriginator)
            {
                case UploadOriginator.FromSourcePath:
                    _ctlAccountPanel.Visibility = Visibility.Visible;
                    ViewModel.ShowSourceLocation = false;
                    ViewModel.CanEditPackageType = true;
                    break;
                case UploadOriginator.FromAWSExplorer:
                    _ctlAccountPanel.Visibility = Visibility.Collapsed;
                    ViewModel.ShowSourceLocation = true;

                    // Entering the Lambda Deploy Wizard from the AWS Explorer does not
                    // support image deploys at this time.
                    ViewModel.PackageType = Amazon.Lambda.PackageType.Zip;
                    ViewModel.CanEditPackageType = false;
                    break;
                case UploadOriginator.FromFunctionView:
                    _ctlAccountPanel.Visibility = Visibility.Collapsed;
                    ViewModel.ShowSourceLocation = true;
                    ViewModel.CanEditFunctionName = false;

                    // Entering the Lambda Deploy Wizard from the the function view
                    // only works with node-based managed runtimes.
                    ViewModel.PackageType = Amazon.Lambda.PackageType.Zip;
                    ViewModel.CanEditPackageType = false;
                    break;
            }
        }

        private void SetPanelsForDeploymentType(DeploymentType deploymentType)
        {
            if (deploymentType == DeploymentType.Generic)
            {
                // NodeJS/Non-Netcore workflow does not support image deploys at this time.
                ViewModel.PackageType = Amazon.Lambda.PackageType.Zip;
                ViewModel.CanEditPackageType = false;
            }
        }

        /// <summary>
        /// Map of Handler "Type" values to Lists of related Function names
        /// </summary>
        private readonly IDictionary<string, IList<string>> _suggestionCache = new Dictionary<string, IList<string>>();

        /// <summary>
        /// Set the Handler Method field to a suggested method if there is one.
        /// </summary>
        private void UpdateHandlerMethodSuggestion()
        {
            ViewModel.HandlerMethodSuggestions.Clear();

            if (!_suggestionCache.TryGetValue(ViewModel.HandlerType, out var methods))
            {
                return;
            }

            foreach (var method in methods)
            {
                ViewModel.HandlerMethodSuggestions.Add(method);
            }

            if (ViewModel.HandlerMethodSuggestions.Count > 0)
            {
                ViewModel.HandlerMethod = ViewModel.HandlerMethodSuggestions.First();
            }
        }

        private void InitializeNETCoreFields()
        {
            ViewModel.Configurations.Add("Release");
            ViewModel.Configurations.Add("Debug");
            ViewModel.Configuration = "Release";

            var projectFrameworks = PageController.HostingWizard[UploadFunctionWizardProperties.ProjectTargetFrameworks] as IList<string>;
            if(projectFrameworks != null && projectFrameworks.Count > 0)
            {
                foreach(var framework in projectFrameworks)
                {
                    ViewModel.Frameworks.Add(framework);
                }

                ViewModel.Framework = ViewModel.Frameworks.First();
            }
            else
            {
                ViewModel.Frameworks.Add(Frameworks.NetCoreApp10);
                ViewModel.Frameworks.Add(Frameworks.NetCoreApp21);
                ViewModel.Frameworks.Add(Frameworks.NetCoreApp31);
                ViewModel.Frameworks.Add(Frameworks.Net50);
                ViewModel.Frameworks.Add(Frameworks.Net60);

                if (_shellProvider.HostInfo.Name == ToolkitHosts.Vs2017.Name)
                {
                    // Select a framework supported by VS2017
                    ViewModel.Framework = ViewModel.Frameworks.First(x => x.MatchesFramework(Frameworks.NetCoreApp21));
                }
                else if (_shellProvider.HostInfo.Name == ToolkitHosts.Vs2019.Name)
                {
                    // Select a framework supported by VS2019
                    ViewModel.Framework = ViewModel.Frameworks.Last(x => !x.MatchesFramework(Frameworks.Net60));
                }
                else
                {
                    ViewModel.Framework = ViewModel.Frameworks.Last();
                }
            }
        }

        public AccountViewModel SelectedAccount => ViewModel.Connection.Account;

        public ToolkitRegion SelectedRegion => ViewModel.Connection.Region;

        private static RuntimeOption GetRuntimeOptionForFramework(string framework, LambdaArchitecture architecture)
        {
            if (!RuntimeByFramework.TryGetValue(framework, out var runtimeOption))
            {
                runtimeOption = architecture.Value.Equals(LambdaArchitecture.Arm.Value) ? RuntimeOption.PROVIDED_AL2 : RuntimeOption.PROVIDED;
            }

            return runtimeOption;
        }

        private void AccountAndRegion_ConnectionChanged(object sender, EventArgs e)
        {
            if (!ViewModel.Connection.ConnectionIsValid)
            {
                return;
            }

            PageController.HostingWizard.SetSelectedAccount(ViewModel.Connection.Account, UploadFunctionWizardProperties.UserAccount);
            PageController.HostingWizard.SetSelectedRegion(ViewModel.Connection.Region, UploadFunctionWizardProperties.Region);

            // Prevent multiple loads caused by property changed events in rapid succession
            _accountRegionChangeDebounceDispatcher.Debounce(AccountRegionChangedDebounceMs, _ =>
            {
                _shellProvider.ExecuteOnUIThread(() =>
                {
                    UpdateExistingFunctions();
                    UpdateImageRepos().LogExceptionAndForget();
                    UpdateImageTags().LogExceptionAndForget();
                });
            });
        }

        private void UpdateExistingFunctions()
        {
            try
            {
                if (!ViewModel.Connection.ConnectionIsValid)
                {
                    return;
                }

                ViewModel.UpdateFunctionsList().LogExceptionAndForget();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing existing lambda functions.", e);
            }
        }

        private async Task UpdateImageRepos()
        {
            try
            {
                if (!ViewModel.Connection.ConnectionIsValid)
                {
                    return;
                }

                using (var ecrClient = ViewModel.CreateServiceClient<AmazonECRClient>())
                {
                    // Reset ImageRepo to be re-selected or re-entered
                    _shellProvider.ExecuteOnUIThread(() => { ViewModel.ImageRepo = string.Empty; });
                    await ViewModel.UpdateImageRepos(ecrClient);
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing existing lambda functions.", e);
            }
        }

        private async Task UpdateImageTags()
        {
            try
            {
                if (!ViewModel.Connection.ConnectionIsValid)
                {
                    return;
                }

                using (var ecrClient = ViewModel.CreateServiceClient<AmazonECRClient>())
                {
                    // Reset ImageTag to be re-selected or re-entered
                    _shellProvider.ExecuteOnUIThread(() => { ViewModel.ImageTag = "latest"; });
                    await ViewModel.UpdateImageTags(ecrClient);
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing existing lambda functions.", e);
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.PackageType))
            {
                ViewModel.CanEditRuntime = ViewModel.PackageType == Amazon.Lambda.PackageType.Zip;
                ViewModel.FilterExistingFunctions();
                RecommendRepoNameIfEmpty();
            }

            if (e.PropertyName == nameof(ViewModel.Framework))
            {
                // Set the Runtime to a compatible value
                ViewModel.Runtime = GetRuntimeOptionForFramework(ViewModel.Framework, ViewModel.Architecture);
            }

            if (e.PropertyName == nameof(ViewModel.Runtime))
            {
                Runtime_PropertyChanged();
            }

            if (e.PropertyName == nameof(ViewModel.HandlerAssembly) ||
                e.PropertyName == nameof(ViewModel.HandlerType) || e.PropertyName == nameof(ViewModel.HandlerMethod))
            {
                if (ShouldComponentsUpdateHandler())
                {
                    ViewModel.Handler = ViewModel.CreateDotNetHandler();
                }
            }

            if (e.PropertyName == nameof(ViewModel.HandlerType))
            {
                UpdateHandlerMethodSuggestion();
            }

            if (e.PropertyName == nameof(ViewModel.Handler))
            {
                ViewModel.ApplyDotNetHandler(ViewModel.Handler);
            }

            if (e.PropertyName == nameof(ViewModel.IsExistingFunction))
            {
                IsExistingFunction_PropertyChanged();
            }

            if (e.PropertyName == nameof(ViewModel.FunctionName))
            {
                if (ViewModel.IsExistingFunction)
                {
                    FunctionName_PropertyChanged();
                }
            }

            if (e.PropertyName == nameof(ViewModel.ImageRepo))
            {
                UpdateImageTags().LogExceptionAndForget();
            }
        }

        private bool ShouldComponentsUpdateHandler()
        {
            return ViewModel.RequiresDotNetHandlerComponents() && !ViewModel.ProjectIsExecutable;
        }

        /// <summary>
        /// Resets Function Name value when switching between Create New Function
        /// and Redeploy to Existing function selections
        /// </summary>
        private void IsExistingFunction_PropertyChanged()
        {
            ViewModel.FunctionName = string.Empty;
        }

        private void FunctionName_PropertyChanged()
        {
            if (ViewModel.TryGetFunctionConfig(this.ViewModel.FunctionName, out var existingConfig))
            {
                // Retrieve some of the Existing Function config so that a new
                // publish does not clobber existing settings.
                // TODO : Some of this could move to the Controller.
                ViewModel.Description = existingConfig.Description;

                ViewModel.Handler = existingConfig.Handler;

                var hostWizard = PageController.HostingWizard;
                hostWizard.SetProperty(UploadFunctionWizardProperties.Role, existingConfig.Role);
                hostWizard.SetProperty(UploadFunctionWizardProperties.ManagedPolicy, null);
                hostWizard.SetProperty(UploadFunctionWizardProperties.MemorySize, existingConfig.MemorySize);
                hostWizard.SetProperty(UploadFunctionWizardProperties.Timeout, existingConfig.Timeout);


                if (existingConfig.DeadLetterConfig?.TargetArn != null)
                    hostWizard.SetProperty(UploadFunctionWizardProperties.DeadLetterTargetArn, existingConfig.DeadLetterConfig.TargetArn);

                if (existingConfig.TracingConfig?.Mode != null)
                    hostWizard.SetProperty(UploadFunctionWizardProperties.TracingMode, existingConfig.TracingConfig.Mode.ToString());


                var variables = new List<EnvironmentVariable>();
                if (existingConfig?.Environment?.Variables != null)
                {
                    foreach (var kvp in existingConfig?.Environment?.Variables)
                    {
                        variables.Add(new EnvironmentVariable { Variable = kvp.Key, Value = kvp.Value });
                    }
                }
                hostWizard.SetProperty(UploadFunctionWizardProperties.EnvironmentVariables, variables);

                if (existingConfig.VpcConfig != null)
                {
                    hostWizard.SetProperty(UploadFunctionWizardProperties.SeedSubnetIds, existingConfig.VpcConfig.SubnetIds.ToArray());
                    hostWizard.SetProperty(UploadFunctionWizardProperties.SeedSecurityGroupIds, existingConfig.VpcConfig.SecurityGroupIds.ToArray());
                }
                else
                {
                    hostWizard.SetProperty(UploadFunctionWizardProperties.SeedSubnetIds, null);
                    hostWizard.SetProperty(UploadFunctionWizardProperties.SeedSecurityGroupIds, null);
                }
            }
        }

        private void PopulateSuggestionCache(IDictionary<string, IList<string>> suggestionCache)
        {
            if (suggestionCache == null)
            {
                return;
            }

            foreach (var keyValuePair in suggestionCache)
            {
                _suggestionCache[keyValuePair.Key] = keyValuePair.Value;
            }
        }

        private void Runtime_PropertyChanged()
        {
            ViewModel.UpdateArchitectureState();

            // Set the Framework to a compatible value
            if (ViewModel.Runtime != null && FrameworkByRuntime.TryGetValue(ViewModel.Runtime, out var framework))
            {
                ViewModel.SetFrameworkIfExists(framework);
            }

            ViewModel.HandlerHelpText = ViewModel.CreateHandlerHelpText();
            ViewModel.HandlerTooltip = ViewModel.CreateHandlerTooltip();
            
            // Toggle control visibilities
            var showConfigAndFramework = ViewModel.Runtime?.IsDotNet ?? false;
            ViewModel.ConfigurationVisibility = showConfigAndFramework ? Visibility.Visible : Visibility.Collapsed;
            ViewModel.FrameworkVisibility = showConfigAndFramework ? Visibility.Visible : Visibility.Collapsed;
            ViewModel.ShowSaveSettings = showConfigAndFramework;
        }

        /// <summary>
        /// Generate a repository name if repo name is empty and package type is image
        /// </summary>
        private void RecommendRepoNameIfEmpty()
        {
            var hostWizard = PageController?.HostingWizard;
            if (hostWizard!=null && ViewModel.PackageType.Equals(Amazon.Lambda.PackageType.Image) &&
                string.IsNullOrEmpty(ViewModel.ImageRepo))
            {
                var sourcePath = hostWizard.CollectedProperties[UploadFunctionWizardProperties.SourcePath] as string;
                if (sourcePath != null)
                {
                    if (Utilities.TryGenerateECRRepositoryName(sourcePath, out var generatedRepositoryName))
                    {
                        ViewModel.ImageRepo = generatedRepositoryName;
                    }
                }
            }
        }
    }
}
