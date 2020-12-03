using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonValidators;
using Amazon.AWSToolkit.Lambda.Model;
using Amazon.AWSToolkit.Shared;
using Amazon.ECR;
using Amazon.ECR.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using log4net;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Environment = System.Environment;

namespace Amazon.AWSToolkit.Lambda.ViewModel
{
    public class UploadFunctionViewModel : BaseModel, IDataErrorInfo
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(UploadFunctionViewModel));

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
        private Visibility _dotNetHandlerVisibility = Visibility.Visible;
        private string _framework;
        private Visibility _frameworkVisibility = Visibility.Visible;
        private string _functionName;
        private string _handler;
        private string _handlerTooltip;
        private Visibility _handlerVisibility = Visibility.Visible;
        private string _handlerAssembly;
        private string _handlerMethod;
        private string _handlerType;
        private PackageType _packageType;
        private bool _saveSettings;
        private bool _showSaveSettings;
        private bool _showSourceLocation;

        private bool _loadingFunctions;
        private RuntimeOption _runtime;
        private string _sourceCodeLocation;
        private string _dockerfile;
        private string _imageCommand;
        private string _imageRepo;
        private bool _loadingImageRepos;
        private string _imageTag = "latest";
        private bool _loadingImageTags;

        public UploadFunctionViewModel() : this(ToolkitFactory.Instance.ShellProvider)
        {
        }

        public UploadFunctionViewModel(IAWSToolkitShellProvider shellProvider)
        {
            _shellProvider = shellProvider;
        }

        public bool CanEditFunctionName
        {
            get => _canEditFunctionName;
            set { SetProperty(ref _canEditFunctionName, value, () => CanEditFunctionName); }
        }

        public bool CanEditPackageType
        {
            get => _canEditPackageType;
            set { SetProperty(ref _canEditPackageType, value, () => CanEditPackageType); }
        }

        public bool CanEditRuntime
        {
            get => _canEditRuntime;
            set { SetProperty(ref _canEditRuntime, value, () => CanEditRuntime); }
        }

        /// <summary>
        ///     Eg: Release, Debug
        /// </summary>
        public string Configuration
        {
            get => _configuration;
            set { SetProperty(ref _configuration, value, () => Configuration); }
        }

        public ObservableCollection<string> Configurations { get; } = new ObservableCollection<string>();

        public Visibility ConfigurationVisibility
        {
            get => _configurationVisibility;
            set { SetProperty(ref _configurationVisibility, value, () => ConfigurationVisibility); }
        }

        public string Description
        {
            get => _description;
            set { SetProperty(ref _description, value, () => Description); }
        }

        public Visibility DotNetHandlerVisibility
        {
            get => _dotNetHandlerVisibility;
            set { SetProperty(ref _dotNetHandlerVisibility, value, () => DotNetHandlerVisibility); }
        }

        public string Framework
        {
            get => _framework;
            set { SetProperty(ref _framework, value, () => Framework); }
        }

        public ObservableCollection<string> Frameworks { get; } = new ObservableCollection<string>();

        public Visibility FrameworkVisibility
        {
            get => _frameworkVisibility;
            set { SetProperty(ref _frameworkVisibility, value, () => FrameworkVisibility); }
        }

        public string FunctionName
        {
            get => _functionName;
            set { SetProperty(ref _functionName, value, () => FunctionName); }
        }

        public string FunctionNameTooltip => "Enter a name for your AWS Lambda function. This name will display in the AWS Management Console.";

        public bool FunctionExists => !string.IsNullOrEmpty(FunctionName) && _functionConfigs.ContainsKey(FunctionName);

        public string Handler
        {
            get => _handler;
            set { SetProperty(ref _handler, value, () => Handler); }
        }

        public string HandlerTooltip
        {
            get => _handlerTooltip;
            set { SetProperty(ref _handlerTooltip, value, () => HandlerTooltip); }
        }

        public Visibility HandlerVisibility
        {
            get => _handlerVisibility;
            set { SetProperty(ref _handlerVisibility, value, () => HandlerVisibility); }
        }

        public string HandlerAssembly
        {
            get => _handlerAssembly;
            set { SetProperty(ref _handlerAssembly, value, () => HandlerAssembly); }
        }

        public string HandlerMethod
        {
            get => _handlerMethod;
            set { SetProperty(ref _handlerMethod, value, () => HandlerMethod); }
        }

        public ObservableCollection<string> HandlerMethodSuggestions { get; } = new ObservableCollection<string>();

        public string HandlerType
        {
            get => _handlerType;
            set { SetProperty(ref _handlerType, value, () => HandlerType); }
        }

        public ObservableCollection<string> HandlerTypeSuggestions { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Indicates whether or not the viewmodel is currently loading Lambda Function details. 
        /// </summary>
        public bool LoadingFunctions
        {
            get => _loadingFunctions;
            private set { SetProperty(ref _loadingFunctions, value, () => LoadingFunctions); }
        }

        public ObservableCollection<string> Functions { get; } = new ObservableCollection<string>();

        public PackageType PackageType
        {
            get => _packageType;
            set { SetProperty(ref _packageType, value, () => PackageType); }
        }

        public ObservableCollection<PackageType> PackageTypes { get; } = new ObservableCollection<PackageType>()
        {
            PackageType.Image,
            PackageType.Zip,
        };

        public string PackageTypeTooltip => $"Zip based packages run code in the selected runtime environment.{Environment.NewLine}{Environment.NewLine}Image based packages define both the runtime environment and code.";

        public RuntimeOption Runtime
        {
            get => _runtime;
            set { SetProperty(ref _runtime, value, () => Runtime); }
        }

        public ObservableCollection<RuntimeOption> Runtimes { get; } = new ObservableCollection<RuntimeOption>();

        /// <summary>
        /// Whether or not to save the Deploy Wizard settings into a json file (aws-lambda-tools-defaults.json)
        /// </summary>
        public bool SaveSettings
        {
            get => _saveSettings;
            set { SetProperty(ref _saveSettings, value, () => SaveSettings); }
        }

        public bool ShowSaveSettings
        {
            get => _showSaveSettings;
            set { SetProperty(ref _showSaveSettings, value, () => ShowSaveSettings); }
        }

        /// <summary>
        /// Whether or not the "Source code" location controls should be shown.
        /// This includes the dockerfile field for image package types.
        /// </summary>
        public bool ShowSourceLocation
        {
            get => _showSourceLocation;
            set { SetProperty(ref _showSourceLocation, value, () => ShowSourceLocation); }
        }

        public string SourceCodeLocation
        {
            get => _sourceCodeLocation;

            set { SetProperty(ref _sourceCodeLocation, value, () => SourceCodeLocation); }
        }


        public string Dockerfile
        {
            get => _dockerfile;

            set { SetProperty(ref _dockerfile, value, () => Dockerfile); }
        }

        public string DockerfileTooltip => "Dockerfile is used to produce the image that is uploaded to Lambda.";

        public string ImageCommand
        {
            get => _imageCommand;
            set { SetProperty(ref _imageCommand, value, () => ImageCommand); }
        }

        public string ImageCommandTooltip => "Optional. Specify the fully-qualified name of your Lambda method, or specify it using CMD in the dockerfile.";


        public string ImageRepo
        {
            get => _imageRepo;
            set { SetProperty(ref _imageRepo, value, () => ImageRepo); }
        }


        /// <summary>
        /// Indicates whether or not the viewmodel is currently loading Image Repos. 
        /// </summary>
        public bool LoadingImageRepos
        {
            get => _loadingImageRepos;
            private set { SetProperty(ref _loadingImageRepos, value, () => LoadingImageRepos); }
        }

        public string ImageRepoTooltip => $"ECR repository to store image.{Environment.NewLine}{Environment.NewLine}Select an existing repo or enter a name to create a new container registry.";

        public ObservableCollection<string> ImageRepos { get; } = new ObservableCollection<string>();

        public string ImageTag
        {
            get => _imageTag;
            set { SetProperty(ref _imageTag, value, () => ImageTag); }
        }

        public string ImageTagTooltip => $"Label to tag the image with.{Environment.NewLine}{Environment.NewLine}Select an existing label or type in a new one.";

        public ObservableCollection<string> ImageTags { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Indicates whether or not the viewmodel is currently loading Image Tags. 
        /// </summary>
        public bool LoadingImageTags
        {
            get => _loadingImageTags;
            private set { SetProperty(ref _loadingImageTags, value, () => LoadingImageTags); }
        }

        public string SourceCodeLocationTooltip => $"The source location can reference a file or folder.{Environment.NewLine}{Environment.NewLine}The file can be a zip file or a single javascript file{Environment.NewLine}The folder will be zipped before being uploaded.";

        /// <summary>
        /// Prompts user to select a folder containing lambda files to publish
        /// </summary>
        public ICommand BrowseForSourceCodeFolder
        {
            get
            {
                if (_browseForSourceCodeFolder == null)
                {
                    _browseForSourceCodeFolder = new RelayCommand(BrowseSourceCodeFolder);
                }

                return _browseForSourceCodeFolder;
            }
        }

        /// <summary>
        /// Prompts user to select a file containing lambda code to publish
        /// </summary>
        public ICommand BrowseForSourceCodeFile
        {
            get
            {
                if (_browseForSourceCodeFile == null)
                {
                    _browseForSourceCodeFile = new RelayCommand(BrowseSourceCodeFile);
                }

                return _browseForSourceCodeFile;
            }
        }

        /// <summary>
        /// Prompts user to select dockerfile for image based lambda
        /// </summary>
        public ICommand BrowseForDockerfile
        {
            get
            {
                if (_browseForDockerfile == null)
                {
                    _browseForDockerfile = new RelayCommand(BrowseDockerfile);
                }

                return _browseForDockerfile;
            }
        }

        /// <summary>
        /// Setter to set <see cref="FunctionName"/> on the UI Thread
        /// </summary>
        private string UiThreadFunctionName
        {
            set
            {
                _shellProvider.ExecuteOnUIThread(() => { FunctionName = value; });
            }
        }

        /// <summary>
        /// Setter to set <see cref="LoadingFunctions"/> on the UI Thread
        /// </summary>
        private bool UiThreadLoadingFunctions
        {
            set
            {
                _shellProvider.ExecuteOnUIThread(() => { LoadingFunctions = value; });
            }
        }

        /// <summary>
        /// Setter to set <see cref="LoadingImageRepos"/> on the UI Thread
        /// </summary>
        private bool UiThreadLoadingImageRepos
        {
            set
            {
                _shellProvider.ExecuteOnUIThread(() => { LoadingImageRepos = value; });
            }
        }

        /// <summary>
        /// Setter to set <see cref="LoadingImageTags"/> on the UI Thread
        /// </summary>
        private bool UiThreadLoadingImageTags
        {
            set
            {
                _shellProvider.ExecuteOnUIThread(() => { LoadingImageTags = value; });
            }
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
                .Split(new[] {"::"}, StringSplitOptions.None)
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
            if (string.IsNullOrEmpty(HandlerAssembly) && string.IsNullOrEmpty(HandlerType) &&
                string.IsNullOrEmpty(HandlerMethod))
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
                    foreach (var function in response.Functions) _functionConfigs[function.FunctionName] = function;

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
            }
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
                var request = new DescribeImagesRequest()
                {
                    RepositoryName = ImageRepo
                };

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
            if (!string.IsNullOrEmpty(functionName))
            {
                return _functionConfigs.TryGetValue(functionName, out config);
            }

            return false;
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

        private void BrowseSourceCodeFolder(object parameter)
        {
            var directory = DirectoryBrowserDlgHelper.ChooseDirectory(
                "Select a directory containing the code for the Lambda function. This directory will be zipped up and uploaded to Lambda.");
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            SourceCodeLocation = directory;
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
            _shellProvider.ExecuteOnUIThread(() => { Functions.Clear(); });
        }

        /// <summary>
        /// Clears <see cref="ImageRepos"/> on the UI Thread
        /// </summary>
        private void UiThreadClearImageRepos()
        {
            _shellProvider.ExecuteOnUIThread(() => { ImageRepos.Clear(); });
        }

        /// <summary>
        /// Clears <see cref="ImageTags"/> on the UI Thread
        /// </summary>
        private void UiThreadClearImageTags()
        {
            _shellProvider.ExecuteOnUIThread(() => { ImageTags.Clear(); });
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
                    .ForEach(functionName => { Functions.Add(functionName); });
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