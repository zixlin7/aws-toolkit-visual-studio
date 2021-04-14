using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Lambda.Controller;
using Amazon.AWSToolkit.Lambda.Model;
using Amazon.AWSToolkit.Lambda.Nodes;
using Amazon.AWSToolkit.Lambda.ViewModel;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Tasks;
using Amazon.ECR;
using Amazon.Lambda;
using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Util;
using Amazon.Common.DotNetCli.Tools;
using Amazon.Runtime;
using static Amazon.AWSToolkit.Lambda.Controller.UploadFunctionController;

namespace Amazon.AWSToolkit.Lambda.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for UploadFunctionDetailsPage.xaml
    /// </summary>
    public partial class UploadFunctionDetailsPage : INotifyPropertyChanged
    {
        const string HandlerTooltipBase = "The function within your code that Lambda calls to begin execution.";
        const string HandlerTooltipDotNet = "For .NET, it is in the form: <assembly>::<type>::<method>";
        const string HandlerTooltipGeneric = "For Node.js, it is the module-name.export value of your function.";
        const string HandlerTooltipCustomRuntime = "For custom runtimes the handler field is optional. The value is made available to the Lambda function through the _HANDLER environment variable.";
        private const int AccountRegionChangedDebounceMs = 250;

        static readonly ILog LOGGER = LogManager.GetLogger(typeof(UploadFunctionDetailsPage));
        public static readonly string LambdaServiceName = new AmazonLambdaConfig().RegionEndpointServiceName;

        private static readonly IDictionary<string, RuntimeOption> RuntimeByFramework =
            new Dictionary<string, RuntimeOption>(StringComparer.OrdinalIgnoreCase)
            {
                {Frameworks.NetCoreApp21, RuntimeOption.NetCore_v2_1},
                {Frameworks.NetCoreApp31, RuntimeOption.NetCore_v3_1},
            };

        private static readonly IDictionary<RuntimeOption, string> FrameworkByRuntime =
            new Dictionary<RuntimeOption, string>()
            {
                {RuntimeOption.NetCore_v2_1, Frameworks.NetCoreApp21},
                {RuntimeOption.NetCore_v3_1, Frameworks.NetCoreApp31},
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
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;

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

            ViewModel.Runtime = deploymentType == DeploymentType.NETCore
                ? RuntimeOption.NetCore_v2_1
                : RuntimeOption.NodeJS_v12_X;

            if (ViewModel.Runtime.IsNetCore)
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
                this.UpdateExistingFunctions();

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

            if (hostWizard.IsPropertySet(UploadFunctionWizardProperties.Runtime))
            {
                var runtime = hostWizard[UploadFunctionWizardProperties.Runtime] as Amazon.Lambda.Runtime;
                ViewModel.Runtime = ViewModel.Runtimes.FirstOrDefault(x => x.Value == runtime?.Value);
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

            ViewModel.SaveSettings = true;
        }

        // todo : rename?
        private void SetPanelsForOriginatorAndType()
        {
            switch (UploadOriginator)
            {
                case UploadFunctionController.UploadOriginator.FromSourcePath:
                    this._ctlAccountPanel.Visibility = Visibility.Visible;
                    ViewModel.ShowSourceLocation = false;
                    ViewModel.CanEditPackageType = true;
                    break;
                case UploadFunctionController.UploadOriginator.FromAWSExplorer:
                    this._ctlAccountPanel.Visibility = Visibility.Collapsed;
                    ViewModel.ShowSourceLocation = true;

                    // Entering the Lambda Deploy Wizard from the AWS Explorer does not
                    // support image deploys at this time.
                    ViewModel.PackageType = Amazon.Lambda.PackageType.Zip;
                    ViewModel.CanEditPackageType = false;
                    break;
                case UploadFunctionController.UploadOriginator.FromFunctionView:
                    this._ctlAccountPanel.Visibility = Visibility.Collapsed;
                    ViewModel.ShowSourceLocation = true;
                    ViewModel.CanEditFunctionName = false;

                    // Entering the Lambda Deploy Wizard from the the function view
                    // only works with node-based managed runtimes.
                    ViewModel.PackageType = Amazon.Lambda.PackageType.Zip;
                    ViewModel.CanEditPackageType = false;
                    break;
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

            var projectFrameworks = this.PageController.HostingWizard[UploadFunctionWizardProperties.ProjectTargetFrameworks] as IList<string>;
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

                if (_shellProvider.HostInfo.Name == ToolkitHosts.Vs2017.Name)
                {
                    // Select a framework supported by VS2017
                    ViewModel.Framework = ViewModel.Frameworks.First(x => x.MatchesFramework(Frameworks.NetCoreApp21));
                }
                else
                {
                    ViewModel.Framework = ViewModel.Frameworks.Last();
                }
            }
        }

        public AccountViewModel SelectedAccount => ViewModel.Connection.Account;

        public ToolkitRegion SelectedRegion => ViewModel.Connection.Region;

        private bool ShowDotNetHandlerComponents =>
            ViewModel.Runtime != null && ViewModel.Runtime.IsNetCore && !ViewModel.Runtime.IsCustomRuntime;

        private static RuntimeOption GetRuntimeOptionForFramework(string framework)
        {
            if (!RuntimeByFramework.TryGetValue(framework, out var runtimeOption))
            {
                runtimeOption = RuntimeOption.PROVIDED;
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
                    this.UpdateExistingFunctions();
                    this.UpdateImageRepos().LogExceptionAndForget();
                    this.UpdateImageTags().LogExceptionAndForget();
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

                using (var lambdaClient = CreateServiceClient<AmazonLambdaClient>())
                {
                    ViewModel.UpdateFunctionsList(lambdaClient).LogExceptionAndForget();
                }
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

                using (var ecrClient = CreateServiceClient<AmazonECRClient>())
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

                using (var ecrClient = CreateServiceClient<AmazonECRClient>())
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

        public bool AllRequiredFieldsAreSet
        {
            get
            {
                if (!ViewModel.Connection.ConnectionIsValid || ViewModel.Connection.IsValidating)
                {
                    return false;
                }

                if (string.IsNullOrEmpty(this.ViewModel.FunctionName))
                {
                    return false;
                }

                if (ViewModel.PackageType.Equals(Amazon.Lambda.PackageType.Zip))
                {
                    return AllRequiredFieldsForZipAreSet();
                }

                return AllRequiredFieldsForImageAreSet();
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
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
                ViewModel.Runtime = GetRuntimeOptionForFramework(ViewModel.Framework);
            }

            if (e.PropertyName == nameof(ViewModel.Runtime))
            {
                OnRuntimeChanged();
            }

            if (e.PropertyName == nameof(ViewModel.HandlerAssembly) ||
                e.PropertyName == nameof(ViewModel.HandlerType) || e.PropertyName == nameof(ViewModel.HandlerMethod))
            {
                if (ShowDotNetHandlerComponents)
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

            if (e.PropertyName == nameof(ViewModel.FunctionName))
            {
                OnFunctionNameChanged();
            }

            if (e.PropertyName == nameof(ViewModel.ImageRepo))
            {
                UpdateImageTags().LogExceptionAndForget();
            }
        }

        private void OnFunctionNameChanged()
        {
            if (ViewModel.TryGetFunctionConfig(this.ViewModel.FunctionName, out var existingConfig))
            {
                // Retrieve some of the Existing Function config so that a new
                // publish does not clobber existing settings.
                // TODO : Some of this could move to the Controller.
                ViewModel.Description = existingConfig.Description;

                ViewModel.Handler = existingConfig.Handler;

                IAWSWizard hostWizard = PageController.HostingWizard;
                hostWizard.SetProperty(UploadFunctionWizardProperties.Role, existingConfig.Role);
                hostWizard.SetProperty(UploadFunctionWizardProperties.ManagedPolicy, null);
                hostWizard.SetProperty(UploadFunctionWizardProperties.MemorySize, existingConfig.MemorySize);
                hostWizard.SetProperty(UploadFunctionWizardProperties.Timeout, existingConfig.Timeout);


                if (existingConfig.DeadLetterConfig?.TargetArn != null)
                    hostWizard.SetProperty(UploadFunctionWizardProperties.DeadLetterTargetArn, existingConfig.DeadLetterConfig.TargetArn);

                if (existingConfig.TracingConfig?.Mode != null)
                    hostWizard.SetProperty(UploadFunctionWizardProperties.TracingMode, existingConfig.TracingConfig.Mode.ToString());


                List<EnvironmentVariable> variables = new List<EnvironmentVariable>();
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

        private void OnRuntimeChanged()
        {
            // Set the Framework to a compatible value
            if (ViewModel.Runtime != null && FrameworkByRuntime.TryGetValue(ViewModel.Runtime, out var framework))
            {
                ViewModel.SetFrameworkIfExists(framework);
            }

            ViewModel.HandlerTooltip = CreateHandlerTooltip();
            ViewModel.DotNetHandlerVisibility = ShowDotNetHandlerComponents ? Visibility.Visible : Visibility.Collapsed;
            ViewModel.HandlerVisibility = !ShowDotNetHandlerComponents ? Visibility.Visible : Visibility.Collapsed;
            
            // Toggle control visibilities
            var showConfigAndFramework = ViewModel.Runtime?.IsNetCore ?? false;
            ViewModel.ConfigurationVisibility = showConfigAndFramework ? Visibility.Visible : Visibility.Collapsed;
            ViewModel.FrameworkVisibility = showConfigAndFramework ? Visibility.Visible : Visibility.Collapsed;
            ViewModel.ShowSaveSettings = showConfigAndFramework;
        }

        private string CreateHandlerTooltip()
        {
            var tooltipText = "";
            if (ViewModel.Runtime == RuntimeOption.PROVIDED)
            {
                tooltipText = HandlerTooltipCustomRuntime;
            }
            else if (ViewModel.Runtime?.IsNetCore ?? false)
            {
                tooltipText = HandlerTooltipDotNet;
            }
            else
            {
                tooltipText = HandlerTooltipGeneric;
            }

            return string.Format("{1}{0}{0}{2}",
                Environment.NewLine,
                HandlerTooltipBase,
                tooltipText
            );
        }

        private bool AllRequiredFieldsForZipAreSet()
        {
            if (!File.Exists(ViewModel.SourceCodeLocation) && !Directory.Exists(ViewModel.SourceCodeLocation))
            {
                return false;
            }

            if (ViewModel.Runtime == null)
            {
                return false;
            }

            if (ShowDotNetHandlerComponents)
            {
                if (string.IsNullOrEmpty(ViewModel.HandlerAssembly))
                {
                    return false;
                }

                if (string.IsNullOrEmpty(ViewModel.HandlerType))
                {
                    return false;
                }

                if (string.IsNullOrEmpty(ViewModel.HandlerMethod))
                {
                    return false;
                }
            }

            if (ViewModel.Runtime != RuntimeOption.PROVIDED && string.IsNullOrEmpty(ViewModel.Handler))
            {
                return false;
            }

            return true;
        }

        private bool AllRequiredFieldsForImageAreSet()
        {
            if (!File.Exists(ViewModel.Dockerfile))
            {
                return false;
            }
            if (string.IsNullOrEmpty(ViewModel.ImageRepo))
            {
                return false;
            }
            if (string.IsNullOrEmpty(ViewModel.ImageTag))
            {
                return false;
            }
            return true;
        }

        private TServiceClient CreateServiceClient<TServiceClient>() where TServiceClient : class, IAmazonService
        {
            return ViewModel.Connection.Account 
                .CreateServiceClient<TServiceClient>(ViewModel.Connection.Region);
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
