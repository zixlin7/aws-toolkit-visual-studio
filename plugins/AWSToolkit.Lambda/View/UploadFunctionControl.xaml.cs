using System;
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

        public UploadFunctionControl(UploadFunctionController controller, Dictionary<string, string> seedValues)
        {
            this._controller = controller;
            InitializeComponent();
            FillAdvanceSettingsComboBoxes();
            this.ResetToDefaults();

            switch(controller.Mode)
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

            var navigator = ToolkitFactory.Instance.Navigator;
            this._ctlAccountAndRegion.Initialize(navigator.SelectedAccount, navigator.SelectedRegionEndPoints, new string[] { LambdaRootViewMetaNode.LAMBDA_ENDPOINT_LOOKUP });
            this._ctlAccountAndRegion.PropertyChanged += _ctlAccountAndRegion_PropertyChanged;

            this.IAMPicker.RoleFilter = x =>
            {
                if (string.IsNullOrEmpty(x.AssumeRolePolicyDocument))
                    return false;

                try
                {
                    var policy = Policy.FromJson(HttpUtility.UrlDecode(x.AssumeRolePolicyDocument));
                    foreach(var statement in policy.Statements)
                    {
                        if(statement.Actions.Contains(new ActionIdentifier("sts:AssumeRole")) &&
                            statement.Principals.Contains(new Principal("Service", "lambda.amazonaws.com")))
                        {
                            return true;
                        }
                    }
                    return x.AssumeRolePolicyDocument.Contains("lambda.amazonaws.com");
                }
                catch(Exception e)
                {
                    LOGGER.ErrorFormat("Error parsing assume role document: {0} Error: {1}", x.AssumeRolePolicyDocument, e.Message);
                    return false;
                }
            };


            this.UpdateIAMPicker();

            if(this._controller.Mode == UploadFunctionController.UploadMode.FromSourcePath)
                this.UpdateExistingFunctions();

            if(seedValues.ContainsKey(LambdaContants.SeedFileName))
            {
                this._ctlFileName.Text = seedValues[LambdaContants.SeedFileName];
            }

            if (seedValues.ContainsKey(LambdaContants.SeedFunctionName))
            {
                this._ctlFunctionNameText.Text = seedValues[LambdaContants.SeedFunctionName];
                this._ctlFunctionNamePicker.SelectedValue = seedValues[LambdaContants.SeedFunctionName];
            }

            if (seedValues.ContainsKey(LambdaContants.SeedHandler))
            {
                this._ctlHandler.Text = seedValues[LambdaContants.SeedHandler];
            }
            else
            {
                this._ctlHandler.Text = LambdaContants.DefaultHandlerName;
            }

            if(seedValues.ContainsKey(LambdaContants.SeedSourcePath))
            {
                this._ctlSource.Text = seedValues[LambdaContants.SeedSourcePath];
            }

            if (seedValues.ContainsKey(LambdaContants.SeedDescription))
            {
                this._ctlDescription.Text = seedValues[LambdaContants.SeedDescription];
            }

            if (seedValues.ContainsKey(LambdaContants.SeedTimeout))
            {
                this._ctlTimeout.Text = seedValues[LambdaContants.SeedTimeout];
            }

            if (seedValues.ContainsKey(LambdaContants.SeedMemory))
            {
                this._ctlMemory.Text = seedValues[LambdaContants.SeedMemory];
            }

            if (seedValues.ContainsKey(LambdaContants.SeedIAMRole))
            {
                this._ctlIAMPicker.SelectExistingRole(seedValues[LambdaContants.SeedIAMRole]);
            }
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

        public string SourcePath
        {
            get { return this._ctlSource.Text; }
        }

        public string Description
        {
            get { return this._ctlDescription.Text; }
        }

        public string FileName
        {
            get { return this._ctlFileName.Text; }
        }

        public string Handler
        {
            get { return this._ctlHandler.Text; }
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
            this._ctlTimeout.Items.Clear();

            foreach (var value in LambdaUtilities.GetValidMemorySizes())
            {
                this._ctlMemory.Items.Add(value.ToString());
            }
            foreach (var value in LambdaUtilities.GetValidValuesForTimeout())
            {
                this._ctlTimeout.Items.Add(value.ToString());
            }
        }

        void ResetToDefaults()
        {
            this._ctlFunctionNamePicker.SelectedValue = null;
            this._ctlMemory.SelectedIndex = 1;
            this._ctlTimeout.SelectedIndex = 9;
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

            var iamClient = this._ctlAccountAndRegion.SelectedAccount.CreateServiceClient<AmazonIdentityManagementServiceClient>(this._ctlAccountAndRegion.SelectedRegion);
            this._ctlIAMPicker.Initialize(iamClient, IAMCapabilityPicker.IAMMode.Roles);
        }

        private void UpdateExistingFunctions()
        {
            this._existingFunctions.Clear();

            try
            {
                if (this._ctlAccountAndRegion.SelectedAccount == null || this._ctlAccountAndRegion.SelectedRegion == null)
                    return;

                var lambdaCient = this._ctlAccountAndRegion.SelectedAccount.CreateServiceClient<AmazonLambdaClient>(this._ctlAccountAndRegion.SelectedRegion);

                lambdaCient.BeginListFunctions(new ListFunctionsRequest(), this.UpdateExistingFunctionsCallback, lambdaCient);
            }
            catch(Exception e)
            {
                LOGGER.Error("Error refreshing existing lambda functions.", e);
            }
        }

        private void UpdateExistingFunctionsCallback(IAsyncResult result)
        {
            try
            {
                var lambdaCient = result.AsyncState as IAmazonLambda;

                ListFunctionsResponse response = lambdaCient.EndListFunctions(result);

                foreach (var function in response.Functions)
                {
                    this._existingFunctions[function.FunctionName] = function;
                }

                ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke((Action)(() =>
                {
                    this._ctlFunctionNamePicker.Items.Clear();
                    foreach (var functionName in this._existingFunctions.Keys.OrderBy(x => x.ToLowerInvariant()))
                    {
                        this._ctlFunctionNamePicker.Items.Add(functionName);
                    }
                    
                    this.ResetToDefaults();
                }));
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing existing lambda functions from callback.", e);
            }
        }

        private void _ctlFunctionName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FunctionConfiguration existingConfig;

            if (this._ctlFunctionNamePicker.SelectedValue != null &&
                this._existingFunctions.TryGetValue(this._ctlFunctionNamePicker.SelectedValue as string, out existingConfig))
            {
                this._ctlIAMPicker.SelectExistingRole(existingConfig.Role);

                this._ctlDescription.Text = existingConfig.Description;
                this._ctlMemory.Text = existingConfig.MemorySize.ToString();
                this._ctlTimeout.Text = existingConfig.Timeout.ToString();
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
            if (string.IsNullOrEmpty(this.FileName))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("File name is required.");
                return false;
            }
            if (string.IsNullOrEmpty(this.Handler))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("The javascript handler is required.");
                return false;
            }
            if(this.Timeout < 1 && this.Timeout >= 60)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Timeout must be between 1 and 60 seconds.");
                return false;
            }
            if (this.Memory < 64 && this.Memory >= 1024)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Memory must be between 64 and 1024 seconds.");
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

            var startupFile = LambdaUtilities.DetermineStartupFromPath(directory);
            if (!string.IsNullOrEmpty(startupFile))
                this._ctlFileName.Text = startupFile;
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

            var startupFile = LambdaUtilities.DetermineStartupFromPath(dlg.FileName);
            if (!string.IsNullOrEmpty(startupFile))
                this._ctlFileName.Text = startupFile;
        }
    }
}
