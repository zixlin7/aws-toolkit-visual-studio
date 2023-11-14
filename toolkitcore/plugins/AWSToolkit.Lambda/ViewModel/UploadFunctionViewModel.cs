using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.Components;
using Amazon.AWSToolkit.CommonValidators;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Lambda.Model;
using Amazon.AWSToolkit.Shared;
using Amazon.ECR;
using Amazon.ECR.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Runtime;

using log4net;

using Microsoft.Win32;

using Environment = System.Environment;

namespace Amazon.AWSToolkit.Lambda.ViewModel
{
    public class UploadFunctionViewModel : BaseModel, IDataErrorInfo
    {
        public const string HandlerTooltipBase = "The handler tells Lambda where to find your code to process events with.";
        public const string HandlerTooltipDotNet = "For .NET runtimes, the Lambda handler format is: <assembly>::<type>::<method>";
        public const string HandlerTooltipDotNetTopLevel = "For .NET managed runtimes using top-level statements, the Lambda handler value should be set to the assembly name.";
        public const string HandlerTooltipGeneric = "For Node.js, the Lambda handler format is: <module-name>.<export value of your function>";
        public const string HandlerTooltipCustomRuntime = "For custom runtimes the Lambda handler field is optional. The value is made available to the Lambda function through the _HANDLER environment variable.";

        private static readonly ILog Logger = LogManager.GetLogger(typeof(UploadFunctionViewModel));

        private static readonly ICollection<RuntimeOption> RuntimesWithoutArmSupport = new List<RuntimeOption>()
        {
            RuntimeOption.PROVIDED
        };

        private readonly AccountAndRegionPickerViewModel _connectionViewModel;

        private readonly Dictionary<string, FunctionConfiguration> _functionConfigs =
            new Dictionary<string, FunctionConfiguration>();

        private readonly IAWSToolkitShellProvider _shellProvider;

        private ICommand _browseForSourceCodeFile;
        private ICommand _browseForDockerfile;

        private ICommand _browseForSourceCodeFolder;

        private bool _canEditFunctionName = true;
        private bool _canEditPackageType = true;
        private bool _canEditRuntime = true;
        private string _configuration;
        private Visibility _configurationVisibility = Visibility.Visible;
        private string _description;
        private string _framework;
        private Visibility _frameworkVisibility = Visibility.Visible;
        private string _functionName;
        private bool _isExistingFunction = true;
        private string _handler;
        private string _handlerHelpText;
        private string _handlerTooltip;
        private string _handlerAssembly;
        private string _handlerMethod;
        private string _handlerType;
        private PackageType _packageType;
        private bool _saveSettings;
        private bool _showSaveSettings;
        private bool _showSourceLocation;

        private bool _loadingFunctions = true;
        private RuntimeOption _runtime;
        private LambdaArchitecture _architecture = LambdaArchitecture.X86;
        private bool _supportsArmArchitecture = true;
        private string _sourceCodeLocation;
        private string _dockerfile;
        private string _imageCommand;
        private string _imageRepo;
        private bool _loadingImageRepos;
        private string _imageTag = "latest";
        private bool _loadingImageTags;

        public UploadFunctionViewModel() : this(ToolkitFactory.Instance.ShellProvider,
            ToolkitFactory.Instance.ToolkitContext)
        {
        }

        public UploadFunctionViewModel(IAWSToolkitShellProvider shellProvider, ToolkitContext toolkitContext)
        {
            _shellProvider = shellProvider;
            _connectionViewModel = new AccountAndRegionPickerViewModel(toolkitContext);
        }

        public bool CanEditFunctionName
        {
            get => _canEditFunctionName;
            set => SetProperty(ref _canEditFunctionName, value);
        }

        public bool CanEditPackageType
        {
            get => _canEditPackageType;
            set => SetProperty(ref _canEditPackageType, value);
        }

        public bool CanEditRuntime
        {
            get => _canEditRuntime;
            set => SetProperty(ref _canEditRuntime, value);
        }

        /// <summary>
        ///     Eg: Release, Debug
        /// </summary>
        public string Configuration
        {
            get => _configuration;
            set => SetProperty(ref _configuration, value);
        }

        public ObservableCollection<string> Configurations { get; } = new ObservableCollection<string>();

        public Visibility ConfigurationVisibility
        {
            get => _configurationVisibility;
            set => SetProperty(ref _configurationVisibility, value);
        }

        public AccountAndRegionPickerViewModel Connection
        {
            get => _connectionViewModel;
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public string Framework
        {
            get => _framework;
            set => SetProperty(ref _framework, value);
        }

        public ObservableCollection<string> Frameworks { get; } = new ObservableCollection<string>();

        public Visibility FrameworkVisibility
        {
            get => _frameworkVisibility;
            set => SetProperty(ref _frameworkVisibility, value);
        }

        public string FunctionName
        {
            get => _functionName;
            set => SetProperty(ref _functionName, value);
        }

        public string FunctionNameTooltip =>
            "Enter a name for your AWS Lambda function. This name will display in the AWS Management Console.";

        public bool FunctionExists => !string.IsNullOrEmpty(FunctionName) && _functionConfigs.ContainsKey(FunctionName);

        public string Handler
        {
            get => _handler;
            set => SetProperty(ref _handler, value);
        }

        public bool IsExistingFunction
        {
            get => _isExistingFunction;
            set => SetProperty(ref _isExistingFunction, value);
        }

        public string HandlerHelpText
        {
            get => _handlerHelpText;
            set => SetProperty(ref _handlerHelpText, value);
        }

        public string HandlerTooltip
        {
            get => _handlerTooltip;
            set => SetProperty(ref _handlerTooltip, value);
        }

        /// <summary>
        /// Lambda functions are typically functions within Class Libraries.
        /// 
        /// Functions based on custom runtimes or that use top-level statements reside in
        /// executable projects.
        ///
        /// This can affect Handlers and validation.
        /// </summary>
        public bool ProjectIsExecutable = false;

        public string HandlerAssembly
        {
            get => _handlerAssembly;
            set => SetProperty(ref _handlerAssembly, value);
        }

        public string HandlerMethod
        {
            get => _handlerMethod;
            set => SetProperty(ref _handlerMethod, value);
        }

        public ObservableCollection<string> HandlerMethodSuggestions { get; } = new ObservableCollection<string>();

        public string HandlerType
        {
            get => _handlerType;
            set => SetProperty(ref _handlerType, value);
        }

        public ObservableCollection<string> HandlerTypeSuggestions { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Indicates whether or not the viewmodel is currently loading Lambda Function details. 
        /// </summary>
        public bool LoadingFunctions
        {
            get => _loadingFunctions;
            private set => SetProperty(ref _loadingFunctions, value);
        }

        public ObservableCollection<string> Functions { get; } = new ObservableCollection<string>();

        public PackageType PackageType
        {
            get => _packageType;
            set => SetProperty(ref _packageType, value);
        }

        public ObservableCollection<PackageType> PackageTypes { get; } = new ObservableCollection<PackageType>()
        {
            PackageType.Image, PackageType.Zip,
        };

        public string PackageTypeTooltip =>
            $"Zip based packages run code in the selected runtime environment.{Environment.NewLine}{Environment.NewLine}Image based packages define both the runtime environment and code.";

        public RuntimeOption Runtime
        {
            get => _runtime;
            set => SetProperty(ref _runtime, value);
        }

        public ObservableCollection<RuntimeOption> Runtimes { get; } = new ObservableCollection<RuntimeOption>();

        public LambdaArchitecture Architecture
        {
            get => _architecture;
            set => SetProperty(ref _architecture, value);
        }

        public bool SupportsArmArchitecture
        {
            get => _supportsArmArchitecture;
            private set => SetProperty(ref _supportsArmArchitecture, value);
        }

        /// <summary>
        /// Whether or not to save the Deploy Wizard settings into a json file (aws-lambda-tools-defaults.json)
        /// </summary>
        public bool SaveSettings
        {
            get => _saveSettings;
            set => SetProperty(ref _saveSettings, value);
        }

        public bool ShowSaveSettings
        {
            get => _showSaveSettings;
            set => SetProperty(ref _showSaveSettings, value);
        }

        /// <summary>
        /// Whether or not the "Source code" location controls should be shown.
        /// This includes the dockerfile field for image package types.
        /// </summary>
        public bool ShowSourceLocation
        {
            get => _showSourceLocation;
            set => SetProperty(ref _showSourceLocation, value);
        }

        public string SourceCodeLocation
        {
            get => _sourceCodeLocation;
            set => SetProperty(ref _sourceCodeLocation, value);
        }


        public string Dockerfile
        {
            get => _dockerfile;
            set => SetProperty(ref _dockerfile, value);
        }

        public string DockerfileTooltip => "Dockerfile is used to produce the image that is uploaded to Lambda.";

        public string ImageCommand
        {
            get => _imageCommand;
            set => SetProperty(ref _imageCommand, value);
        }

        public string ImageCommandTooltip =>
            "Optional. Specify the fully-qualified name of your Lambda method, or specify it using CMD in the dockerfile.";


        public string ImageRepo
        {
            get => _imageRepo;
            set => SetProperty(ref _imageRepo, value);
        }


        /// <summary>
        /// Indicates whether or not the viewmodel is currently loading Image Repos. 
        /// </summary>
        public bool LoadingImageRepos
        {
            get => _loadingImageRepos;
            private set => SetProperty(ref _loadingImageRepos, value);
        }

        public string ImageRepoTooltip =>
            $"ECR repository to store image.{Environment.NewLine}{Environment.NewLine}Select an existing repo or enter a name to create a new container registry.";

        public ObservableCollection<string> ImageRepos { get; } = new ObservableCollection<string>();

        public string ImageTag
        {
            get => _imageTag;
            set => SetProperty(ref _imageTag, value);
        }

        public string ImageTagTooltip =>
            $"Label to tag the image with.{Environment.NewLine}{Environment.NewLine}Select an existing label or type in a new one.";

        public ObservableCollection<string> ImageTags { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Indicates whether or not the viewmodel is currently loading Image Tags. 
        /// </summary>
        public bool LoadingImageTags
        {
            get => _loadingImageTags;
            private set => SetProperty(ref _loadingImageTags, value);
        }

        public string SourceCodeLocationTooltip =>
            $"The source location can reference a file or folder.{Environment.NewLine}{Environment.NewLine}The file can be a zip file or a single javascript file{Environment.NewLine}The folder will be zipped before being uploaded.";

        /// <summary>
        /// Prompts user to select a folder containing lambda files to publish
        /// </summary>
        public ICommand BrowseForSourceCodeFolder
        => _browseForSourceCodeFolder ?? (_browseForSourceCodeFolder = new RelayCommand(BrowseSourceCodeFolder));

        /// <summary>
        /// Prompts user to select a file containing lambda code to publish
        /// </summary>
        public ICommand BrowseForSourceCodeFile => _browseForSourceCodeFile ?? (_browseForSourceCodeFile = new RelayCommand(BrowseSourceCodeFile));

        /// <summary>
        /// Prompts user to select dockerfile for image based lambda
        /// </summary>
        public ICommand BrowseForDockerfile => _browseForDockerfile ?? (_browseForDockerfile = new RelayCommand(BrowseDockerfile));

        /// <summary>
        /// Setter to set <see cref="FunctionName"/> on the UI Thread
        /// </summary>
        private string UiThreadFunctionName
        {
            set => _shellProvider.ExecuteOnUIThread(() => FunctionName = value);
        }

        /// <summary>
        /// Setter to set <see cref="LoadingFunctions"/> on the UI Thread
        /// </summary>
        private bool UiThreadLoadingFunctions
        {
            set => _shellProvider.ExecuteOnUIThread(() => LoadingFunctions = value);
        }

        /// <summary>
        /// Setter to set <see cref="LoadingImageRepos"/> on the UI Thread
        /// </summary>
        private bool UiThreadLoadingImageRepos
        {
            set => _shellProvider.ExecuteOnUIThread(() => LoadingImageRepos = value);
        }

        /// <summary>
        /// Setter to set <see cref="LoadingImageTags"/> on the UI Thread
        /// </summary>
        private bool UiThreadLoadingImageTags
        {
            set => _shellProvider.ExecuteOnUIThread(() => LoadingImageTags = value);
        }

        public bool IsValidConfiguration()
        {
            if (!Connection.ConnectionIsValid || Connection.IsValidating)
            {
                return false;
            }

            if (string.IsNullOrEmpty(FunctionName))
            {
                return false;
            }

            if (PackageType.Equals(PackageType.Zip))
            {
                return IsValidZipConfiguration();
            }

            return IsValidImageConfiguration();
        }

        private bool IsValidZipConfiguration()
        {
            if (!File.Exists(SourceCodeLocation) && !Directory.Exists(SourceCodeLocation))
            {
                return false;
            }

            if (Runtime == null)
            {
                return false;
            }

            if (Architecture == null)
            {
                return false;
            }

            if (RequiresDotNetHandlerComponents())
            {
                if (string.IsNullOrEmpty(HandlerAssembly))
                {
                    return false;
                }

                if (string.IsNullOrEmpty(HandlerType))
                {
                    return false;
                }

                if (string.IsNullOrEmpty(HandlerMethod))
                {
                    return false;
                }
            }

            if (!Runtime.IsCustomRuntime && string.IsNullOrEmpty(Handler))
            {
                return false;
            }

            return true;
        }

        public bool RequiresDotNetHandlerComponents()
        {
            return Runtime != null && Runtime.IsDotNet && !Runtime.IsCustomRuntime
            && !ProjectIsExecutable;
        }

        private bool IsValidImageConfiguration()
        {
            if (Architecture == null)
            {
                return false;
            }

            if (!File.Exists(Dockerfile))
            {
                return false;
            }

            if (string.IsNullOrEmpty(ImageRepo))
            {
                return false;
            }

            if (string.IsNullOrEmpty(ImageTag))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        ///     If the given Lambda handler follows the .NET handler format, it is
        ///     applied to the Handler related fields. Otherwise, the related fields
        ///     are cleared.
        ///     Also sets the Handler property.
        /// </summary>
        public void ApplyDotNetHandler(string handler)
        {
            handler = handler?.Trim();

            var tokens = handler?
                .Split(new[] { "::" }, StringSplitOptions.None)
                .Select(text => text.Trim())
                .ToArray();

            if (tokens?.Length == 3)
            {
                HandlerAssembly = tokens[0];
                HandlerType = tokens[1];
                HandlerMethod = tokens[2];
            }
            else
            {
                HandlerAssembly = string.Empty;
                HandlerType = string.Empty;
                HandlerMethod = string.Empty;
            }

            Handler = handler;
        }

        /// <summary>
        ///     Generates a .NET Lambda handler from the Handler component properties
        /// </summary>
        /// <example>MyAssembly::MyNamespace.MyClass::MyFunction</example>
        public string CreateDotNetHandler()
        {
            if (string.IsNullOrEmpty(HandlerAssembly)
                && string.IsNullOrEmpty(HandlerType)
                && string.IsNullOrEmpty(HandlerMethod))
            {
                return string.Empty;
            }

            return string.Format("{0}::{1}::{2}",
                HandlerAssembly?.Trim() ?? string.Empty,
                HandlerType?.Trim() ?? string.Empty,
                HandlerMethod?.Trim() ?? string.Empty);
        }

        /// <summary>
        ///     Sets the Framework property to the given value only if
        ///     the value exists in the Frameworks collection.
        ///     Otherwise the value is ignored.
        /// </summary>
        public void SetFrameworkIfExists(string framework)
        {
            if (framework != null && Frameworks.Contains(framework))
            {
                Framework = framework;
            }
        }

        /// <summary>
        /// Queries the given Lambda client for Functions, updates list of available functions
        /// </summary>
        public async Task UpdateFunctionsList()
        {
            using (var lambdaClient = CreateServiceClient<AmazonLambdaClient>())
            {
                await UpdateFunctionsList(lambdaClient).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Queries the given Lambda client for Functions, updates list of available functions
        /// </summary>
        public async Task UpdateFunctionsList(IAmazonLambda lambda)
        {
            UiThreadLoadingFunctions = true;

            // Clearing Functions can wipe FunctionName, explicitly restore it afterwards
            var currentFunctionName = FunctionName;

            try
            {
                UiThreadClearFunctions();
                _functionConfigs.Clear();

                var request = new ListFunctionsRequest();

                do
                {
                    var response = await lambda.ListFunctionsAsync(request);

                    response.Functions.ForEach(function => _functionConfigs[function.FunctionName] = function);

                    request.Marker = response.NextMarker;
                } while (!string.IsNullOrEmpty(request.Marker));

                UiThreadFilterFunctions();

            }
            catch (Exception e)
            {
                Logger.Error("Error refreshing existing lambda functions.", e);
            }
            finally
            {
                UiThreadLoadingFunctions = false;
                //in presence of an existing function, defaults such as function name might be loaded after the async list function call is made
                //check if function name has been set already while it was loading functions
                //if yes, after loading functions notify that function name has been set, in order to reload and verify all properties are set to allow navigation to next page 
                //if not, set the function name stored before
                if (string.IsNullOrEmpty(FunctionName))
                {
                    UiThreadFunctionName = currentFunctionName;
                }
                else
                {
                    NotifyPropertyChanged(nameof(FunctionName));
                }

                UpdateIsExistingFunction();
            }
        }

        public void UpdateIsExistingFunction()
        {
            IsExistingFunction = FunctionExists;
        }

        public TServiceClient CreateServiceClient<TServiceClient>() where TServiceClient : class, IAmazonService
        {
            return Connection.Account
                .CreateServiceClient<TServiceClient>(Connection.Region);
        }

        /// <summary>
        /// Queries the given ECR client for Repos, updates list of available repos.
        /// </summary>
        public async Task UpdateImageRepos(IAmazonECR ecr)
        {
            UiThreadLoadingImageRepos = true;

            try
            {
                UiThreadClearImageRepos();
                var repos = new List<Repository>();
                var request = new DescribeRepositoriesRequest();

                do
                {
                    var response = await ecr.DescribeRepositoriesAsync(request);
                    repos.AddRange(response.Repositories);

                    request.NextToken = response.NextToken;
                } while (!string.IsNullOrEmpty(request.NextToken));

                _shellProvider.ExecuteOnUIThread(() =>
                {
                    repos
                        .Select(r => r.RepositoryName)
                        .OrderBy(r => r)
                        .ToList()
                        .ForEach(r => ImageRepos.Add(r));
                });
            }
            catch (Exception e)
            {
                Logger.Error("Error listing ECR Repos.", e);
            }
            finally
            {
                UiThreadLoadingImageRepos = false;
            }
        }

        /// <summary>
        /// Queries the given ECR client for Repo Tags, updates list of available tags.
        /// <seealso cref="ImageRepo"/> is the repo that is queried.
        /// </summary>
        public async Task UpdateImageTags(IAmazonECR ecr)
        {
            UiThreadLoadingImageTags = true;

            try
            {
                UiThreadClearImageTags();

                if (string.IsNullOrWhiteSpace(ImageRepo))
                {
                    return;
                }

                // Skip invalid repo names
                if (!string.IsNullOrEmpty(EcsRepoNameValidator.Validate(ImageRepo)))
                {
                    return;
                }

                var imageDetails = new List<ImageDetail>();
                var request = new DescribeImagesRequest() { RepositoryName = ImageRepo };

                do
                {
                    var response = await ecr.DescribeImagesAsync(request);
                    imageDetails.AddRange(response.ImageDetails);

                    request.NextToken = response.NextToken;
                } while (!string.IsNullOrEmpty(request.NextToken));

                _shellProvider.ExecuteOnUIThread(() =>
                {
                    imageDetails
                        .SelectMany(d => d.ImageTags)
                        .Distinct()
                        .Where(tag => !string.IsNullOrWhiteSpace(tag))
                        .OrderBy(tag => tag)
                        .ToList()
                        .ForEach(r => ImageTags.Add(r));
                });
            }
            catch (Exception e)
            {
                Logger.Error("Error listing ECR Tags.", e);
            }
            finally
            {
                UiThreadLoadingImageTags = false;
            }
        }

        public bool TryGetFunctionConfig(string functionName, out FunctionConfiguration config)
        {
            config = null;
            return !string.IsNullOrEmpty(functionName) && _functionConfigs.TryGetValue(functionName, out config);
        }

        /// <summary>
        /// Filters existing functions by selected package type
        /// </summary>
        public void FilterExistingFunctions()
        {
            // Clearing Functions can wipe FunctionName, explicitly restore it afterwards
            var currentFunctionName = FunctionName;
            try
            {
                if (Functions.Count != 0)
                {
                    UiThreadClearFunctions();
                }

                UiThreadFilterFunctions();
            }
            catch (Exception e)
            {
                Logger.Error("Error refreshing existing lambda functions.", e);
            }
            finally
            {
                if (string.IsNullOrEmpty(FunctionName))
                {
                    UiThreadFunctionName = currentFunctionName;
                }
            }
        }

        public string CreateHandlerHelpText()
        {
            if (Runtime?.IsCustomRuntime ?? false)
            {
                return HandlerTooltipCustomRuntime;
            }

            if (Runtime?.IsDotNet ?? false)
            {
                return ProjectIsExecutable ? HandlerTooltipDotNetTopLevel : HandlerTooltipDotNet;
            }

            return HandlerTooltipGeneric;
        }

        public string CreateHandlerTooltip()
        {
            return string.Format("{1}{0}{0}{2}",
                Environment.NewLine,
                HandlerTooltipBase,
                CreateHandlerHelpText()
            );
        }

        public LambdaArchitecture GetArchitectureOrDefault(string architectureValue, LambdaArchitecture defaultValue)
        {
            return LambdaArchitecture.All.FirstOrDefault(x => x.Value == architectureValue) ?? defaultValue;
        }

        public void UpdateArchitectureState()
        {
            UpdateArmEnabledState();
            EnsureValidArchitectureValue();
        }

        private void UpdateArmEnabledState()
        {
            var supportsArm = SupportsArm(Runtime);
            if (SupportsArmArchitecture != supportsArm)
            {
                _shellProvider.ExecuteOnUIThread(() => SupportsArmArchitecture = supportsArm);
            }
        }

        private void EnsureValidArchitectureValue()
        {
            if (!SupportsArm(Runtime) && Architecture == LambdaArchitecture.Arm)
            {
                _shellProvider.ExecuteOnUIThread(() => Architecture = LambdaArchitecture.X86);
            }
        }

        private bool SupportsArm(RuntimeOption runtime)
        {
            return runtime != null && !RuntimesWithoutArmSupport.Contains(runtime);
        }

        private void BrowseSourceCodeFolder(object parameter)
        {
            var dlg = _shellProvider.GetDialogFactory()?.CreateFolderBrowserDialog();
            if (dlg == null)
            {
                Debug.Assert(!Debugger.IsAttached, "Unable to get the folder browser. Users will not be able to select a folder for upload.");
                return;
            }

            dlg.Title = "Select folder to zip and upload to Lambda";
            dlg.FolderPath = SourceCodeLocation;

            if (dlg.ShowModal())
            {
                SourceCodeLocation = dlg.FolderPath;
            }
        }

        private void BrowseSourceCodeFile(object parameter)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select Lambda Function code",
                CheckPathExists = true,
                Multiselect = false,
                Filter = "Lambda Functions (*.js, *.zip)|*.js;*.zip|All Files (*.*)|*"
            };

            if (!dlg.ShowDialog().GetValueOrDefault())
            {
                return;
            }

            SourceCodeLocation = dlg.FileName;
        }

        private void BrowseDockerfile(object parameter)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select Dockerfile to create Image used as Lambda function",
                CheckPathExists = true,
                Multiselect = false,
                Filter = "Dockerfile|Dockerfile|All Files (*.*)|*"
            };

            if (!dlg.ShowDialog().GetValueOrDefault())
            {
                return;
            }

            Dockerfile = dlg.FileName;
        }

        /// <summary>
        /// Clears <see cref="Functions"/> on the UI Thread
        /// </summary>
        private void UiThreadClearFunctions()
        {
            _shellProvider.ExecuteOnUIThread(() => Functions.Clear());
        }

        /// <summary>
        /// Clears <see cref="ImageRepos"/> on the UI Thread
        /// </summary>
        private void UiThreadClearImageRepos()
        {
            _shellProvider.ExecuteOnUIThread(() => ImageRepos.Clear());
        }

        /// <summary>
        /// Clears <see cref="ImageTags"/> on the UI Thread
        /// </summary>
        private void UiThreadClearImageTags()
        {
            _shellProvider.ExecuteOnUIThread(() => ImageTags.Clear());
        }

        /// <summary>
        /// Filters existing functions by current selected package type
        /// on the UI thread
        /// </summary>
        private void UiThreadFilterFunctions()
        {
            _shellProvider.ExecuteOnUIThread(() =>
            {
                _functionConfigs
                    .Where(functionConfig => functionConfig.Value.PackageType.Equals(PackageType))
                    .Select(x=>x.Key)
                    .OrderBy(x => x.ToLowerInvariant())
                    .ToList()
                    .ForEach(functionName => Functions.Add(functionName));
            });
        }

        #region IDataErrorInfo

        // Allows for databinding against validation issues
        // When fields are valid, null is returned, else a validation message is returned.
        public string this[string columnName]
        {
            get
            {
                if (columnName == nameof(ImageRepo))
                {
                    return EcsRepoNameValidator.Validate(ImageRepo);
                }

                if (columnName == nameof(ImageTag))
                {
                    return DockerTagValidator.Validate(ImageTag);
                }

                if (columnName == nameof(FunctionName))
                {
                    if (string.IsNullOrWhiteSpace(FunctionName))
                    {
                        return "Function name cannot be empty";
                    }
                }

                return null;
            }
        }

        public string Error
        {
            // We don't use this part of IDataErrorInfo
            get => null;
        }

        #endregion
    }
}
