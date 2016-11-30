using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


using Amazon.Auth.AccessControlPolicy;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.Components;
using Amazon.AWSToolkit.Lambda.Controller;
using Amazon.AWSToolkit.Lambda.Nodes;

using Amazon.IdentityManagement;
using Amazon.Lambda;
using Amazon.Lambda.Model;

using log4net;

namespace Amazon.AWSToolkit.Lambda.View
{
    /// <summary>
    /// Interaction logic for UploadFunctionControl.xaml
    /// </summary>
    public partial class UploadFunctionControl : BaseAWSControl
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(UploadFunctionControl));

        UploadFunctionController _controller;

        Dictionary<string, FunctionConfiguration> _existingFunctions = new Dictionary<string, FunctionConfiguration>();
        UploadFunctionController.DeploymentType _deploymentType;

        public UploadFunctionControl(UploadFunctionController controller, Dictionary<string, object> seedValues, UploadFunctionController.DeploymentType deploymentType)
        {
            this._controller = controller;
            this._deploymentType = deploymentType;
            InitializeComponent();

            this._ctlTypeName.DataContext = this;
            this._ctlMethodName.DataContext = this;

            FillAdvanceSettingsComboBoxes();
            this.ResetToDefaults();

            switch (controller.Mode)
            {
                case UploadFunctionController.UploadMode.FromSourcePath:
                    this._ctlAccountPanel.Visibility = Visibility.Visible;
                    this._ctlSourcePanel.Visibility = Visibility.Collapsed;
                    this._ctlFunctionNamePicker.Visibility = Visibility.Visible;
                    this._ctlFunctionNameText.Visibility = Visibility.Collapsed;
                    break;
                case UploadFunctionController.UploadMode.FromAWSExplorer:
                    this._ctlAccountPanel.Visibility = Visibility.Collapsed;
                    this._ctlSourcePanel.Visibility = Visibility.Visible;
                    this._ctlFunctionNamePicker.Visibility = Visibility.Collapsed;
                    this._ctlFunctionNameText.Visibility = Visibility.Visible;
                    break;
                case UploadFunctionController.UploadMode.FromFunctionView:
                    this._ctlAccountPanel.Visibility = Visibility.Collapsed;
                    this._ctlSourcePanel.Visibility = Visibility.Visible;
                    this._ctlFunctionNamePicker.Visibility = Visibility.Collapsed;
                    this._ctlFunctionNameText.Visibility = Visibility.Visible;
                    this._ctlFunctionNameText.IsReadOnly = true;
                    this._ctlOpenView.Visibility = System.Windows.Visibility.Collapsed;
                    break;
            }

            switch(deploymentType)
            {
                case UploadFunctionController.DeploymentType.Generic:
                    this._ctlGenericHandlerPanel.Visibility = Visibility.Visible;
                    this._ctlNETCoreHandlerPanel.Visibility = Visibility.Collapsed;
                    break;
                case UploadFunctionController.DeploymentType.NETCore:
                    this._ctlNETCoreHandlerPanel.Visibility = Visibility.Visible;
                    this._ctlGenericHandlerPanel.Visibility = Visibility.Collapsed;
                    InitializeNETCoreFields();
                    break;
            }

            var navigator = ToolkitFactory.Instance.Navigator;
            this._ctlAccountAndRegion.Initialize(navigator.SelectedAccount, navigator.SelectedRegionEndPoints, new string[] { LambdaRootViewMetaNode.LAMBDA_ENDPOINT_LOOKUP });
            this._ctlAccountAndRegion.PropertyChanged += _ctlAccountAndRegion_PropertyChanged;

            this.IAMPicker.RoleFilter = RolePolicyFilter.AssumeRoleServicePrincipalSelector;

            this.UpdateIAMPicker();

            if (this._controller.Mode == UploadFunctionController.UploadMode.FromSourcePath)
                this.UpdateExistingFunctions();


            if (seedValues.ContainsKey(LambdaConstants.SeedFunctionName))
            {
                this._ctlFunctionNameText.Text = seedValues[LambdaConstants.SeedFunctionName] as string;
                this._ctlFunctionNamePicker.SelectedValue = seedValues[LambdaConstants.SeedFunctionName];
            }

            if (seedValues.ContainsKey(LambdaConstants.SeedHandler))
            {
                this._ctlHandler.Text = seedValues[LambdaConstants.SeedHandler] as string;
            }

            if (seedValues.ContainsKey(LambdaConstants.SeedSourcePath))
            {
                this._ctlSource.Text = seedValues[LambdaConstants.SeedSourcePath] as string;
            }

            if (seedValues.ContainsKey(LambdaConstants.SeedDescription))
            {
                this._ctlDescription.Text = seedValues[LambdaConstants.SeedDescription] as string;
            }

            if (seedValues.ContainsKey(LambdaConstants.SeedTimeout))
            {
                this._ctlTimeout.Text = seedValues[LambdaConstants.SeedTimeout] as string;
            }

            if (seedValues.ContainsKey(LambdaConstants.SeedMemory))
            {
                this._ctlMemory.Text = seedValues[LambdaConstants.SeedMemory] as string;
            }

            if (seedValues.ContainsKey(LambdaConstants.SeedIAMRole))
            {
                this._ctlIAMPicker.SelectExistingRole(seedValues[LambdaConstants.SeedIAMRole] as string);
            }

            if (seedValues.ContainsKey(LambdaConstants.SeedSuggestedMethods))
            {
                _suggestionCache = seedValues[LambdaConstants.SeedSuggestedMethods] as IDictionary<string, IList<string>>;
                if(_suggestionCache != null)
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

                    foreach(var type in typeNames)
                    {
                        SuggestedTypes.Add(type);
                    }
                }
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
            if(_suggestionCache.TryGetValue(_ctlTypeName.Text, out methods))
            {                
                foreach(var method in methods)
                {
                    this.SuggestedMethods.Add(method);
                }
            }
        }


        private void InitializeNETCoreFields()
        {
            this._ctlConfigurationPicker.Items.Add("Release");
            this._ctlConfigurationPicker.Items.Add("Debug");

            this._ctlFrameworkPicker.Items.Add("netcoreapp1.0");
        }


        public override string Title
        {
            get
            {
                if (this._controller.Mode == UploadFunctionController.UploadMode.FromAWSExplorer)
                    return "Create new Lambda Function";
                else if (this._controller.Mode == UploadFunctionController.UploadMode.FromFunctionView)
                    return "Update Lambda Function";

                return "Upload Lambda Function";
            }
        }

        public string FunctionName
        {
            get 
            {
                switch(this._controller.Mode)
                {
                    case UploadFunctionController.UploadMode.FromSourcePath:
                        return this._ctlFunctionNamePicker.Text;
                    default:
                        return this._ctlFunctionNameText.Text;
                }
            }
        }

        public string Configuration
        {
            get { return this._ctlConfigurationPicker.Text; }
        }

        public string Framework
        {
            get { return this._ctlFrameworkPicker.Text; }
        }

        public string SourcePath
        {
            get { return this._ctlSource.Text; }
        }

        public string Description
        {
            get { return this._ctlDescription.Text; }
        }



        public string Handler
        {
            get
            {
                if (this._deploymentType == UploadFunctionController.DeploymentType.NETCore)
                    return string.Format("{0}::{1}::{2}", this._ctlAssemblyName.Text, this._ctlTypeName.Text, this._ctlMethodName.Text);

                return this._ctlHandler.Text;
            }
        }

        public string Assembly
        {
            get { return this._ctlAssemblyName.Text; }
            set { this._ctlAssemblyName.Text = value; }
        }

        public int Memory
        {
            get 
            {
                int value;
                if (int.TryParse(this._ctlMemory.Text, out value))
                    return value;

                return 0;
            }
        }

        public IAMCapabilityPicker IAMPicker
        {
            get { return this._ctlIAMPicker; }
        }

        public int Timeout
        {
            get
            {
                int value;
                if (int.TryParse(this._ctlTimeout.Text, out value))
                    return value;

                return 0;
            }
        }

        public bool OpenView
        {
            get { return this._ctlOpenView.IsChecked.GetValueOrDefault(); }
        }

        void FillAdvanceSettingsComboBoxes()
        {
            this._ctlMemory.Items.Clear();

            foreach (var value in LambdaUtilities.GetValidMemorySizes())
            {
                this._ctlMemory.Items.Add(value.ToString());
            }
        }

        void ResetToDefaults()
        {
            this._ctlFunctionNamePicker.SelectedValue = null;
            this._ctlMemory.SelectedIndex = 2;
            this._ctlTimeout.Text = "10";
            this._ctlIAMPicker.SelectExistingRole(null);
        }

        void _ctlAccountAndRegion_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.UpdateIAMPicker();
            this.UpdateExistingFunctions();
        }

        private void UpdateIAMPicker()
        {
            if (this._ctlAccountAndRegion.SelectedAccount == null || this._ctlAccountAndRegion.SelectedRegion == null)
                return;

            this._ctlIAMPicker.Initialize(this._ctlAccountAndRegion.SelectedAccount, this._ctlAccountAndRegion.SelectedRegion, IAMCapabilityPicker.IAMMode.Roles);
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

                var lambdaClient = this._ctlAccountAndRegion.SelectedAccount.CreateServiceClient<AmazonLambdaClient>(this._ctlAccountAndRegion.SelectedRegion.GetEndpoint(RegionEndPointsManager.LAMBDA_SERVICE_NAME));

                lambdaClient.ListFunctionsAsync(new ListFunctionsRequest()).ContinueWith(task =>
                {
                    if (task.Exception == null)
                    {
                        ListFunctionsResponse response = task.Result;
                        foreach (var function in response.Functions)
                        {
                            this._existingFunctions[function.FunctionName] = function;
                        }

                        ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke(() =>
                        {
                            this._ctlFunctionNamePicker.Items.Clear();
                            foreach (var functionName in this._existingFunctions.Keys.OrderBy(x => x.ToLowerInvariant()))
                            {
                                this._ctlFunctionNamePicker.Items.Add(functionName);
                            }

                            this.ResetToDefaults();
                            this._loadingExistingFunctions = false;
                            lambdaClient.Dispose();
                        });
                    }
                    else
                    {
                        LOGGER.Error("Error refreshing existing lambda functions with ListFunctionsAsync.", task.Exception);
                    }
                });
            }
            catch(Exception e)
            {
                LOGGER.Error("Error refreshing existing lambda functions.", e);
            }
        }

        private void _ctlFunctionName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loadingExistingFunctions)
                return;

            FunctionConfiguration existingConfig;

            if (this._ctlFunctionNamePicker.SelectedValue != null &&
                this._existingFunctions.TryGetValue(this._ctlFunctionNamePicker.SelectedValue as string, out existingConfig))
            {
                this._ctlIAMPicker.SelectExistingRole(existingConfig.Role);

                this._ctlDescription.Text = existingConfig.Description;
                this._ctlMemory.Text = existingConfig.MemorySize.ToString();
                this._ctlTimeout.Text = existingConfig.Timeout.ToString();

                var handlerTokens = existingConfig.Handler.Split(new string[] { "::" }, StringSplitOptions.None);
                if(handlerTokens.Length == 3)
                {
                    this._ctlAssemblyName.Text = handlerTokens[0];
                    this._ctlTypeName.Text = handlerTokens[1];
                    this._ctlMethodName.Text = handlerTokens[2];
                }
            }
        }

        public override bool Validated()
        {
            if(!File.Exists(this._ctlSource.Text) && !Directory.Exists(this._ctlSource.Text))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Source location must be set.");
                return false;
            }
            if(string.IsNullOrEmpty(this.FunctionName))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Function Name is required.");
                return false;
            }

            if (this._deploymentType == UploadFunctionController.DeploymentType.NETCore)
            {
                if (string.IsNullOrEmpty(this._ctlAssemblyName.Text))
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Assembly name is required.");
                    return false;
                }
                if (string.IsNullOrEmpty(this._ctlTypeName.Text))
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Type name is required.");
                    return false;
                }
                if (string.IsNullOrEmpty(this._ctlMethodName.Text))
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Method name is required.");
                    return false;
                }

            }
            else
            {
                if (string.IsNullOrEmpty(this.Handler))
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("The handler field is required.");
                    return false;
                }
            }

            if (this.Timeout < LambdaUtilities.MIN_TIMEOUT && this.Timeout >= LambdaUtilities.MAX_TIMEOUT)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError(string.Format("Timeout must be between {0} and {1} seconds.", LambdaUtilities.MIN_TIMEOUT, LambdaUtilities.MAX_TIMEOUT));
                return false;
            }
            if (this.Memory < LambdaUtilities.MIN_MEMORY_SIZE && this.Memory >= LambdaUtilities.MAX_MEMORY_SIZE)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError(string.Format("Memory must be between {0} and {1} megabytes.", LambdaUtilities.MIN_MEMORY_SIZE, LambdaUtilities.MAX_TIMEOUT));
                return false;
            }
            if(this.IAMPicker.SelectedRole == null && 
                (this.IAMPicker.SelectedPolicyTemplates == null || this.IAMPicker.SelectedPolicyTemplates.Length == 0))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("The IAM Role is required.");
                return false;
            }

            if (this.IAMPicker.SelectedPolicyTemplates != null && this.IAMPicker.SelectedPolicyTemplates.Length > 0)
            {
                var cloudWatchTemplate = this.IAMPicker.SelectedPolicyTemplates.FirstOrDefault(x => string.Equals(x.Name, IAMCapabilityPicker.CLOUDWATCH_TEMPLATE));
                if (cloudWatchTemplate == null)
                {
                    string message = "You have choosen to create a new IAM Role but have choosen not to add permissions to CloudWatch. " +
                        "Enabling CloudWatch is recommended to enable monitoring of the usage of your function. It also allows debug " +
                        "information to be written to CloudWatch Logs.\r\n\r\nAre you sure want to continue without enabling CloudWatch permissions?";

                    if (!ToolkitFactory.Instance.ShellProvider.Confirm("CloudWatch Permissions", message))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public override bool OnCommit()
        {
            if (this._ctlAccountAndRegion.SelectedAccount == null || this._ctlAccountAndRegion.SelectedRegion == null)
                return false;

            try
            {
                var host = FindHost<OkCancelDialogHost>();
                if (host != null)
                    host.IsOkEnabled = false;

                this._controller.UploadFunction(this._ctlAccountAndRegion.SelectedAccount, this._ctlAccountAndRegion.SelectedRegion);
            }
            catch(Exception e)
            {
                LOGGER.Error("Error uploading function: " + e.Message, e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Failed Upload", "Error uploading function: " + e.Message);
            }

            return false;
        }

        public void UploadFunctionAsyncCompleteSuccess(UploadFunctionController.UploadFunctionState uploadState)
        {
            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke((Action)(() =>
            {
                var navigator = ToolkitFactory.Instance.Navigator;
                if (navigator.SelectedAccount != uploadState.Account)
                    navigator.UpdateAccountSelection(new Guid(uploadState.Account.SettingsUniqueKey), false);
                if (navigator.SelectedRegionEndPoints != uploadState.Region)
                    navigator.UpdateRegionSelection(uploadState.Region);

                var lambdaNode = uploadState.Account.FindSingleChild<LambdaRootViewModel>(false);
                lambdaNode.Refresh(false);

                if (uploadState.OpenView && this._controller.Mode != UploadFunctionController.UploadMode.FromFunctionView)
                {
                    var funcNode = lambdaNode.Children.FirstOrDefault(x => x.Name == uploadState.Request.FunctionName) as LambdaFunctionViewModel;
                    if (funcNode != null)
                    {
                        var metaNode = funcNode.MetaNode as LambdaFunctionViewMetaNode;
                        metaNode.OnOpen(funcNode);
                    }
                }

                var host = FindHost<OkCancelDialogHost>();
                if (host == null)
                    return;

                host.Close(true);
            }));
        }

        public void UploadFunctionAsyncCompleteError(string message)
        {
            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke((Action)(() =>
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error Uploading", message);

                var host = FindHost<OkCancelDialogHost>();
                if (host == null)
                    return;
                host.IsOkEnabled = true;
            }));
        }
        public void ReportOnUploadStatus(string message)
        {
            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke((Action)(() =>
            {
                ToolkitFactory.Instance.ShellProvider.UpdateStatus(message);
                ToolkitFactory.Instance.ShellProvider.OutputToHostConsole(message, true);
            }));
        }

        private void BrowseDirectory_Click(object sender, RoutedEventArgs e)
        {
            string directory = DirectoryBrowserDlgHelper.ChooseDirectory(this, "Select a directory containing the code for the Lambda function. This directory will be zipped up and uploaded to Lambda.");
            if (string.IsNullOrEmpty(directory))
                return;

            this._ctlSource.Text = directory;

            //var startupFile = LambdaUtilities.DetermineStartupFromPath(directory);
            //if (!string.IsNullOrEmpty(startupFile))
            //    this._ctlFileName.Text = startupFile;
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

            //var startupFile = LambdaUtilities.DetermineStartupFromPath(dlg.FileName);
            //if (!string.IsNullOrEmpty(startupFile))
            //    this._ctlFileName.Text = startupFile;
        }

        private void PreviewIntTextInput(object sender, TextCompositionEventArgs e)
        {
            int i;
            e.Handled = !int.TryParse(e.Text, out i);
        }
    }
}
