using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Lambda.Controller;
using Amazon.AWSToolkit.Lambda.Model;
using Amazon.AWSToolkit.Lambda.Nodes;
using Amazon.AWSToolkit.Lambda.WizardPages.PageControllers;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using log4net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ThirdParty.Json.LitJson;
using static Amazon.AWSToolkit.Lambda.Controller.UploadFunctionController;

namespace Amazon.AWSToolkit.Lambda.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for UploadFunctionDetailsPage.xaml
    /// </summary>
    public partial class UploadFunctionDetailsPage : INotifyPropertyChanged
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(UploadFunctionDetailsPage));

        public IAWSWizardPageController PageController { get; private set; }

        Dictionary<string, FunctionConfiguration> _existingFunctions = new Dictionary<string, FunctionConfiguration>();

        public DeploymentType DeploymentType { get; private set; }
        public UploadOriginator UploadOriginator { get; private set; }

        private string SeedFunctionName { get; set; }

        public UploadFunctionDetailsPage()
        {
            InitializeComponent();
            DataContext = this;
        }

        public UploadFunctionDetailsPage(IAWSWizardPageController pageController)
            : this()
        {
            PageController = pageController;
            var hostWizard = PageController.HostingWizard;

            DeploymentType 
                = (DeploymentType)hostWizard.CollectedProperties[UploadFunctionWizardProperties.DeploymentType];
            UploadOriginator
                = (UploadOriginator)hostWizard.CollectedProperties[UploadFunctionWizardProperties.UploadOriginator];

            this._ctlTypeName.DataContext = this;
            this._ctlMethodName.DataContext = this;

            this._ctlRuntime.ItemsSource = RuntimeOption.ALL_OPTIONS;

            this.ResetToDefaults();

            SetPanelsForOriginatorAndType(true);

            var userAccount = hostWizard[UploadFunctionWizardProperties.UserAccount] as AccountViewModel;
            var regionEndpoints = hostWizard[UploadFunctionWizardProperties.Region] as RegionEndPointsManager.RegionEndPoints;

            this._ctlAccountAndRegion.Initialize(userAccount, regionEndpoints, new string[] { LambdaRootViewMetaNode.LAMBDA_ENDPOINT_LOOKUP });
            this._ctlAccountAndRegion.PropertyChanged += _ctlAccountAndRegion_PropertyChanged;

            var buildConfiguration = hostWizard[UploadFunctionWizardProperties.Configuration] as string;
            if (!string.IsNullOrEmpty(buildConfiguration) && this._ctlConfigurationPicker.Items.Contains(buildConfiguration))
            {
                this.Configuration = buildConfiguration;
            }

            var targetFramework = hostWizard[UploadFunctionWizardProperties.Framework] as string;
            if (!string.IsNullOrEmpty(targetFramework) && this._ctlFrameworkPicker.Items.Contains(targetFramework))
            {
                this.Framework = targetFramework;
            }

            if (UploadOriginator == UploadOriginator.FromSourcePath)
                this.UpdateExistingFunctions();

            if (hostWizard.IsPropertySet(UploadFunctionWizardProperties.FunctionName))
            {
                this.SeedFunctionName = hostWizard.CollectedProperties[UploadFunctionWizardProperties.FunctionName] as string;
                this._ctlFunctionNameText.Text = this.SeedFunctionName;
                this._ctlFunctionNamePicker.Items.Add(this.SeedFunctionName);
                this._ctlFunctionNamePicker.SelectedValue = this.SeedFunctionName;
            }

            if (hostWizard.IsPropertySet(UploadFunctionWizardProperties.Handler))
            {
                Handler = hostWizard.CollectedProperties[UploadFunctionWizardProperties.Handler] as string;

                var tokens = this.Handler.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries);
                if(tokens.Length == 3)
                {
                    this.Assembly = tokens[0];
                    this.TypeName = tokens[1];
                    this.MethodName = tokens[2];
                }
            }

            if (hostWizard.IsPropertySet(UploadFunctionWizardProperties.SourcePath))
            {
                SourcePath = hostWizard.CollectedProperties[UploadFunctionWizardProperties.SourcePath] as string;
            }

            if (hostWizard.IsPropertySet(UploadFunctionWizardProperties.Description))
            {
                Description = hostWizard.CollectedProperties[UploadFunctionWizardProperties.Description] as string;
            }

            if (hostWizard.IsPropertySet(UploadFunctionWizardProperties.SuggestedMethods))
            {
                _suggestionCache = hostWizard.CollectedProperties[UploadFunctionWizardProperties.SuggestedMethods] as IDictionary<string, IList<string>>;
                if (_suggestionCache != null)
                {
                    var typeNames = _suggestionCache.Keys.ToArray();

                    // Sort so that files higher up the namespace come first.
                    Array.Sort(typeNames, (x, y) =>
                    {
                        var tokensX = x.Split('.').Length;
                        var tokensY = y.Split('.').Length;

                        if (tokensX < tokensY)
                            return -1;
                        else if (tokensY < tokensX)
                            return 1;

                        return string.Compare(x, y);
                    });

                    foreach (var type in typeNames)
                    {
                        SuggestedTypes.Add(type);
                    }
                }
            }

            if (hostWizard.IsPropertySet(UploadFunctionWizardProperties.Runtime))
            {
                Runtime = hostWizard[UploadFunctionWizardProperties.Runtime] as RuntimeOption;
            }

            this._ctlPersistSettings.IsChecked = true;
        }

        private void SetPanelsForOriginatorAndType(bool isFirstTimeConfig)
        {
            switch (UploadOriginator)
            {
                case UploadFunctionController.UploadOriginator.FromSourcePath:
                    this._ctlAccountPanel.Visibility = Visibility.Visible;
                    this._ctlSourcePanel.Visibility = Visibility.Collapsed;
                    this._ctlFunctionNamePicker.Visibility = Visibility.Visible;
                    this._ctlFunctionNameText.Visibility = Visibility.Collapsed;
                    break;
                case UploadFunctionController.UploadOriginator.FromAWSExplorer:
                    this._ctlAccountPanel.Visibility = Visibility.Collapsed;
                    this._ctlSourcePanel.Visibility = Visibility.Visible;
                    this._ctlFunctionNamePicker.Visibility = Visibility.Collapsed;
                    this._ctlFunctionNameText.Visibility = Visibility.Visible;
                    break;
                case UploadFunctionController.UploadOriginator.FromFunctionView:
                    this._ctlAccountPanel.Visibility = Visibility.Collapsed;
                    this._ctlSourcePanel.Visibility = Visibility.Visible;
                    this._ctlFunctionNamePicker.Visibility = Visibility.Collapsed;
                    this._ctlFunctionNameText.Visibility = Visibility.Visible;
                    this._ctlFunctionNameText.IsReadOnly = true;
                    break;
            }

            switch (DeploymentType)
            {
                case UploadFunctionController.DeploymentType.Generic:
                    this._ctlGenericHandlerPanel.Visibility = Visibility.Visible;
                    this._ctlNETCoreHandlerPanel.Visibility = Visibility.Collapsed;
                    this._ctlPersistPanel.Visibility = Visibility.Collapsed;
                    if (isFirstTimeConfig)
                    {
                        Runtime = RuntimeOption.NodeJS_v6_10;
                    }
                    break;
                case UploadFunctionController.DeploymentType.NETCore:
                    this._ctlNETCoreHandlerPanel.Visibility = Visibility.Visible;
                    this._ctlGenericHandlerPanel.Visibility = Visibility.Collapsed;
                    this._ctlPersistPanel.Visibility = Visibility.Visible;
                    if (isFirstTimeConfig)
                    {
                        Runtime = RuntimeOption.NetCore_v1_0;
                        InitializeControlForNETCore(PageController.HostingWizard[UploadFunctionWizardProperties.SourcePath] as string);
                        InitializeNETCoreFields();
                    }
                    break;
            }
        }

        IDictionary<string, IList<string>> _suggestionCache;
        public ObservableCollection<string> SuggestedTypes { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> SuggestedMethods { get; set; } = new ObservableCollection<string>();

        private void _ctlTypeName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suggestionCache == null)
                return;

            this.SuggestedMethods.Clear();
            IList<string> methods;
            if (_suggestionCache.TryGetValue(_ctlTypeName.Text, out methods))
            {
                foreach (var method in methods)
                {
                    this.SuggestedMethods.Add(method);
                }

                if(this.SuggestedMethods.Count == 1)
                {
                    this._ctlMethodName.Text = this.SuggestedMethods[0];
                }
            }
        }

        private void InitializeNETCoreFields()
        {
            this._ctlConfigurationPicker.Items.Add("Release");
            this._ctlConfigurationPicker.Items.Add("Debug");
            this.Configuration = "Release";

            var projectFrameworks = this.PageController.HostingWizard[UploadFunctionWizardProperties.ProjectTargetFrameworks] as IList<string>;
            if(projectFrameworks != null && projectFrameworks.Count > 0)
            {
                foreach(var framework in projectFrameworks)
                {
                    this._ctlFrameworkPicker.Items.Add(framework);
                }
            }
            else
            {
                this._ctlFrameworkPicker.Items.Add("netcoreapp2.0");
                this._ctlFrameworkPicker.Items.Add("netcoreapp1.0");
            }

            this._ctlFrameworkPicker.SelectedIndex = 0;
            this.Framework = this._ctlFrameworkPicker.Items[0].ToString();
        }

        public string FunctionName
        {
            get
            {
                switch (UploadOriginator)
                {
                    case UploadFunctionController.UploadOriginator.FromSourcePath:
                        return this._ctlFunctionNamePicker.Text;
                    default:
                        return this._ctlFunctionNameText.Text;
                }
            }
        }

        public bool SaveSettings
        {
            get { return this._ctlPersistSettings.IsChecked.GetValueOrDefault(); }
        }

        public AccountViewModel SelectedAccount
        {
            get
            {
                return _ctlAccountAndRegion.SelectedAccount;
            }
        }

        public RegionEndPointsManager.RegionEndPoints SelectedRegion
        {
            get
            {
                return _ctlAccountAndRegion.SelectedRegion;
            }
        }

        string _configuration;
        public string Configuration
        {
            get { return _configuration; }
            set
            {
                _configuration = value;
                NotifyPropertyChanged("Configuration");
            }
        }

        bool _inRuntimeFrameworkEvent;

        string _framework;
        public string Framework
        {
            get { return _framework; }
            set
            {
                _framework = value;
                if(!_inRuntimeFrameworkEvent)
                {
                    _inRuntimeFrameworkEvent = true;
                    try
                    {
                        NotifyPropertyChanged("Framework");
                        if(string.Equals(_framework, "netcoreapp2.0", StringComparison.OrdinalIgnoreCase))
                        {
                            _runtime = RuntimeOption.NetCore_v2_0;
                            this._ctlRuntime.SelectedItem = _runtime;
                        }
                        else if(string.Equals(_framework, "netcoreapp1.0", StringComparison.OrdinalIgnoreCase))
                        {
                            _runtime = RuntimeOption.NetCore_v1_0;
                            this._ctlRuntime.SelectedItem = _runtime;
                        }
                    }
                    finally
                    {
                        _inRuntimeFrameworkEvent = false;
                    }
                }
            }
        }

        RuntimeOption _runtime;
        public RuntimeOption Runtime
        {
            get { return _runtime; }
            set
            {
                _runtime = value;
                if (!_inRuntimeFrameworkEvent)
                {
                    _inRuntimeFrameworkEvent = true;
                    try
                    {
                        NotifyPropertyChanged("Runtime");
                        if (RuntimeOption.NetCore_v2_0 == _runtime && this._ctlFrameworkPicker.Items.Contains("netcoreapp2.0"))
                        {
                            _framework = "netcoreapp2.0";
                            this._ctlFrameworkPicker.SelectedItem = "netcoreapp2.0";
                        }
                        else if (RuntimeOption.NetCore_v1_0 == _runtime && this._ctlFrameworkPicker.Items.Contains("netcoreapp1.0"))
                        {
                            _framework = "netcoreapp1.0";
                            this._ctlFrameworkPicker.SelectedItem = "netcoreapp1.0";
                        }
                    }
                    finally
                    {
                        _inRuntimeFrameworkEvent = false;
                    }
                }
            }
        }

        public string RuntimeValue
        {
            get
            {
                if (Runtime == null)
                    return null;

                return Runtime.Value;
            }
        }

        string _sourcePath;
        public string SourcePath
        {
            get { return _sourcePath; }
            set
            {
                _sourcePath = value;
                NotifyPropertyChanged("SourcePath");
            }
        }

        string _description;
        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                NotifyPropertyChanged("Description");
            }
        }

        string _handler;
        public string Handler
        {
            get
            {
                return _handler;
            }
            set
            {
                _handler = value;
                NotifyPropertyChanged("Handler");
            }
        }

        public string FormattedHandler
        {
            get
            {
                if (DeploymentType == DeploymentType.NETCore)
                    return string.Format("{0}::{1}::{2}", Assembly, TypeName, MethodName);

                return _handler;
            }
        }

        string _assemblyName;
        public string Assembly
        {
            get { return _assemblyName; }
            set
            {
                _assemblyName = value;
                NotifyPropertyChanged("Assembly");
            }
        }

        string _typeName;
        public string TypeName
        {
            get { return _typeName; }
            set
            {
                _typeName = value;
                NotifyPropertyChanged("TypeName");
            }
        }

        string _methodName;
        public string MethodName
        {
            get { return _methodName; }
            set
            {
                _methodName = value;
                NotifyPropertyChanged("MethodName");
            }
        }

        void ResetToDefaults()
        {
            if (!string.IsNullOrEmpty(this.SeedFunctionName) && this._ctlFunctionNamePicker.Items.Contains(this.SeedFunctionName))
            {
                this._ctlFunctionNamePicker.SelectedValue = this.SeedFunctionName;
            }
            else
            {
                this._ctlFunctionNamePicker.SelectedValue = null;
            }
        }

        void _ctlAccountAndRegion_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (this._ctlAccountAndRegion.SelectedAccount == null || this._ctlAccountAndRegion.SelectedRegion == null)
                return;

            PageController.HostingWizard.SetProperty(UploadFunctionWizardProperties.UserAccount, this._ctlAccountAndRegion.SelectedAccount);
            PageController.HostingWizard.SetProperty(UploadFunctionWizardProperties.Region, this._ctlAccountAndRegion.SelectedRegion);

            this.UpdateExistingFunctions();
        }

        private bool _loadingExistingFunctions = false;
        private void UpdateExistingFunctions()
        {
            this._loadingExistingFunctions = true;
            this._existingFunctions.Clear();

            try
            {
                if (this._ctlAccountAndRegion.SelectedAccount == null || this._ctlAccountAndRegion.SelectedRegion == null)
                    return;

                using (var lambdaClient = this._ctlAccountAndRegion.SelectedAccount.CreateServiceClient<AmazonLambdaClient>(this._ctlAccountAndRegion.SelectedRegion.GetEndpoint(RegionEndPointsManager.LAMBDA_SERVICE_NAME)))
                {

                    var response = new ListFunctionsResponse();

                    do
                    {
                        response = lambdaClient.ListFunctions(new ListFunctionsRequest { Marker = response.NextMarker });
                        foreach (var function in response.Functions)
                        {
                            this._existingFunctions[function.FunctionName] = function;
                        }


                    } while (!string.IsNullOrEmpty(response.NextMarker));
                }

                ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke(() =>
                {
                    this._ctlFunctionNamePicker.Items.Clear();
                    if (!string.IsNullOrEmpty(this.SeedFunctionName))
                    {
                        this._ctlFunctionNamePicker.Items.Add(this.SeedFunctionName);
                    }

                    foreach (var functionName in this._existingFunctions.Keys.OrderBy(x => x.ToLowerInvariant()))
                    {
                        if (!string.Equals(functionName, this.SeedFunctionName, StringComparison.Ordinal))
                        {
                            this._ctlFunctionNamePicker.Items.Add(functionName);
                        }
                    }

                    this.ResetToDefaults();
                    this._loadingExistingFunctions = false;
                });
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
                if (!File.Exists(this.SourcePath) && !Directory.Exists(this.SourcePath))
                {
                    return false;
                }
                if (string.IsNullOrEmpty(this.FunctionName))
                {
                    return false;
                }

                if (DeploymentType == UploadFunctionController.DeploymentType.NETCore)
                {
                    if (string.IsNullOrEmpty(this.Assembly))
                    {
                        return false;
                    }
                    if (string.IsNullOrEmpty(this.TypeName))
                    {
                        return false;
                    }
                    if (string.IsNullOrEmpty(this.MethodName))
                    {
                        return false;
                    }

                }
                else
                {
                    if (string.IsNullOrEmpty(this.Handler))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private void BrowseDirectory_Click(object sender, RoutedEventArgs e)
        {
            string directory = DirectoryBrowserDlgHelper.ChooseDirectory(this, "Select a directory containing the code for the Lambda function. This directory will be zipped up and uploaded to Lambda.");
            if (string.IsNullOrEmpty(directory))
                return;

            this._ctlSource.Text = directory;
        }

        private void BrowseFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Title = "Select Lambda Function code";
            dlg.CheckPathExists = true;
            dlg.Multiselect = false;
            dlg.Filter = "Lamda Functions (*.js, *.zip)|*.js;*.zip|All Files (*.*)|*";

            if (!dlg.ShowDialog().GetValueOrDefault())
            {
                return;
            }

            this._ctlSource.Text = dlg.FileName;
        }

        private void _ctlFunctionName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loadingExistingFunctions)
                return;

            NotifyPropertyChanged("FunctionName");
        }

        private void _ctlFunctionName_TextChanged(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged("FunctionName");
        }

        private void _ctlFunctionNamePicker_DropDownClosed(object sender, EventArgs e)
        {
            FunctionConfiguration existingConfig;

            if (this._ctlFunctionNamePicker.SelectedValue != null
                    && this._existingFunctions.TryGetValue(this._ctlFunctionNamePicker.SelectedValue as string, out existingConfig))
            {
                Description = existingConfig.Description;

                if (this._ctlHandler.Visibility == Visibility.Visible)
                {
                    this.Handler = existingConfig.Handler;
                }
                else
                {
                    var handlerTokens = existingConfig.Handler.Split(new string[] { "::" }, StringSplitOptions.None);
                    if (handlerTokens.Length == 3)
                    {
                        Assembly = handlerTokens[0];
                        TypeName = handlerTokens[1];
                        MethodName = handlerTokens[2];
                    }
                }

                IAWSWizard hostWizard = PageController.HostingWizard;
                hostWizard.SetProperty(UploadFunctionWizardProperties.Role, existingConfig.Role);
                hostWizard.SetProperty(UploadFunctionWizardProperties.ManagedPolicy, null);
                hostWizard.SetProperty(UploadFunctionWizardProperties.MemorySize, existingConfig.MemorySize);
                hostWizard.SetProperty(UploadFunctionWizardProperties.Timeout, existingConfig.Timeout);


                List<EnvironmentVariable> variables = new List<EnvironmentVariable>();
                if(existingConfig?.Environment?.Variables != null)
                { 
                    foreach(var kvp in existingConfig?.Environment?.Variables)
                    {
                        variables.Add(new EnvironmentVariable {Variable = kvp.Key, Value = kvp.Value });
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



        private void InitializeControlForNETCore(string sourcePath)
        {
            if (sourcePath == null)
                return;

            try
            {
                var projectJsonPath = sourcePath;
                if (!projectJsonPath.EndsWith("project.json"))
                {
                    projectJsonPath = Path.Combine(projectJsonPath, "project.json");
                }

                string assemblyName = null;
                JsonData rootData = JsonMapper.ToObject(File.ReadAllText(projectJsonPath));

                JsonData frameworks = rootData["frameworks"];
                if (frameworks != null)
                {
                    JsonData netcoreapp1_0 = frameworks["netcoreapp1.0"];
                    if (netcoreapp1_0 != null)
                    {
                        assemblyName = GetOuputNameFromBuildOptions(netcoreapp1_0["buildOptions"]);
                    }
                }

                if (assemblyName == null)
                {
                    assemblyName = GetOuputNameFromBuildOptions(rootData["buildOptions"]);
                }

                if (assemblyName == null)
                {
                    assemblyName = Directory.GetParent(projectJsonPath).Name;
                }

                Assembly = assemblyName;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error searching for default assembly in " + sourcePath, e);
            }
        }

        private string GetOuputNameFromBuildOptions(JsonData buildOptions)
        {
            if (buildOptions == null)
                return null;

            JsonData outputName = buildOptions["outputName"];
            if (outputName == null)
                return null;
            return outputName.ToString();
        }

        private void _ctlRuntime_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var runtime = this._ctlRuntime.SelectedItem as RuntimeOption;
            if (runtime == null)
                return;

            DeploymentType = runtime.IsNetCore ? DeploymentType.NETCore : DeploymentType.Generic;
            SetPanelsForOriginatorAndType(false);
        }
    }
}
