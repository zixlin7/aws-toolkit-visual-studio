using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.Components;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.EC2.Model;
using Amazon.KeyManagementService.Model;
using log4net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading.Tasks;
using Amazon.IdentityManagement.Model;
using Amazon.AWSToolkit.Lambda.Model;
using System.Net;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Lambda.View;
using Amazon.AWSToolkit.Regions;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.Runtime;
using Amazon.Common.DotNetCli.Tools;

using LambdaTelemetryUtils = Amazon.AWSToolkit.Lambda.Util.LambdaTelemetryUtils;
using Amazon.AWSToolkit.Credentials.Core;

namespace Amazon.AWSToolkit.Lambda.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for UploadFunctionAdvancedPage.xaml
    /// </summary>
    public partial class UploadFunctionAdvancedPage : BaseAWSUserControl, INotifyPropertyChanged
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(UploadFunctionAdvancedPage));

        private IAWSWizardPageController PageController { get; set; }
        private readonly ITelemetryLogger _telemetryLogger;

        public UploadFunctionAdvancedPage()
        {
            DataContext = this;

            EnvironmentVariables = new ObservableCollection<EnvironmentVariable>();

            InitializeComponent();

            _ctlTimeoutRange.Text = string.Format("({0} - {1})", LambdaUtilities.MIN_TIMEOUT, LambdaUtilities.MAX_TIMEOUT);
        }

        public UploadFunctionAdvancedPage(IAWSWizardPageController pageController, ITelemetryLogger telemetryLogger)
            : this()
        {
            PageController = pageController;
            _telemetryLogger = telemetryLogger;
            var hostWizard = pageController.HostingWizard;

            var memorySizes = LambdaUtilities.GetValidMemorySizes();
            foreach (var value in memorySizes)
            {
                this._ctlMemory.Items.Add(value);
            }

            IAMPicker.PropertyChanged += AttemptTrustedPolicyCleanup;
            IAMPicker.PropertyChanged += ForwardEmbeddedControlPropertyChanged;
            IAMPicker.RoleFilter = RolePolicyFilter.AssumeRoleServicePrincipalSelector;

            if (hostWizard.IsPropertySet(UploadFunctionWizardProperties.MemorySize))
                _ctlMemory.SelectedValue = hostWizard[UploadFunctionWizardProperties.MemorySize] as string;
            else
                _ctlMemory.SelectedValue = memorySizes.Length > 2 ? memorySizes[2] : memorySizes[0];

            if (hostWizard.IsPropertySet(UploadFunctionWizardProperties.Timeout))
                _ctlTimeout.Text = hostWizard[UploadFunctionWizardProperties.Timeout] as string;
            else
                _ctlTimeout.Text = "10";

            _ctlSecurityGroups.PropertyChanged += ForwardEmbeddedControlPropertyChanged;
            _ctlVpcSubnets.PropertyChanged += ForwardEmbeddedControlPropertyChanged;
            _ctlKMSKey.PropertyChanged += ForwardEmbeddedControlPropertyChanged;
            _ctlDLQ.PropertyChanged += ForwardEmbeddedControlPropertyChanged;

            if (hostWizard.IsPropertySet(UploadFunctionWizardProperties.TracingMode))
            {
                var defaultValue = _ctlTimeout.Text = hostWizard[UploadFunctionWizardProperties.TracingMode] as string;
                this._ctlEnableActiveTracing.IsChecked = string.Equals(defaultValue, Amazon.Lambda.TracingMode.Active, StringComparison.OrdinalIgnoreCase);
            }
        }

        public ObservableCollection<EnvironmentVariable> EnvironmentVariables { get; }

        public ObservableCollection<SelectableItem<SecurityGroupWrapper>> AvailableSecurityGroups => _ctlSecurityGroups.AvailableSecurityGroups;

        public void SetAvailableSecurityGroups(IEnumerable<SecurityGroup> existingGroups, string autoSelectGroup, string[] seedSecurityGroupsIds)
        {
            _ctlSecurityGroups.SetAvailableSecurityGroups(existingGroups, autoSelectGroup, seedSecurityGroupsIds);
        }

        public ObservableCollection<SelectableItem<VpcAndSubnetWrapper>> AvailableVpcSubnets => _ctlVpcSubnets.AvailableVpcSubnets;

        public void SetAvailableVpcSubnets(IEnumerable<Vpc> vpcs, IEnumerable<Subnet> subnets, string[] seedSubnetIds)
        {
            var defaultSelection = _ctlVpcSubnets.SetAvailableVpcSubnets(vpcs, subnets, seedSubnetIds);
            if (defaultSelection == null)
                _ctlSecurityGroups.SetAvailableSecurityGroups(null, null, null);               
        }

        public void SetAvailableKMSKeys(IEnumerable<KeyListEntry> keys, IEnumerable<AliasListEntry> aliases, string defaultArn)
        {
            KeyListEntry defaultSelection = null;
            if (!string.IsNullOrEmpty(defaultArn))
            {
                foreach (var k in keys)
                {
                    if (k.KeyArn.Equals(defaultArn))
                    {
                        defaultSelection = k;
                        break;
                    }
                }
            }

            if (defaultSelection == null)
                defaultSelection = KeyAndAliasWrapper.LambdaDefaultKMSKey.Key;

            _ctlKMSKey.SetAvailableKMSKeys(keys, 
                                        aliases, 
                                        new KeyAndAliasWrapper[] { KeyAndAliasWrapper.LambdaDefaultKMSKey }, 
                                        defaultSelection);
        }

        public void SetAvailableDLQTargets(IList<string> topicArns, IList<string> queueArns)
        {
            string defaultSelection = null;
            if (PageController.HostingWizard.IsPropertySet(UploadFunctionWizardProperties.DeadLetterTargetArn))
                defaultSelection = PageController.HostingWizard[UploadFunctionWizardProperties.DeadLetterTargetArn] as string;

            _ctlDLQ.SetAvailableDLQTargets(topicArns, queueArns, defaultSelection);
        }

        public string SelectedDLQTargetArn => this._ctlDLQ.SelectedArn;

        public bool IsEnableActiveTracing => this._ctlEnableActiveTracing.IsChecked.GetValueOrDefault();

        public IEnumerable<SubnetWrapper> SelectedSubnets => _ctlVpcSubnets.SelectedSubnets;

        public IEnumerable<SecurityGroupWrapper> SelectedSecurityGroups => _ctlSecurityGroups.SelectedSecurityGroups;

        public bool SubnetsSpanVPCs => _ctlVpcSubnets.SubnetsSpanVPCs;

        public void SetXRayAvailability(bool xrayIsAvailable)
        {
            // hide rather than collapse to avoid disturbing layout
            _ctlXRayOptionsPanel.Visibility = xrayIsAvailable ? Visibility.Visible : Visibility.Hidden;
        }

        private void ForwardEmbeddedControlPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged(e.PropertyName);
        }

        private void AttemptTrustedPolicyCleanup(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            AccountViewModel account = null;
            ToolkitRegion region = null;
            Role selectedRole = null;
            string metricEventState = null;
            string stepName = "start";

            try
            {
                selectedRole = this.IAMPicker.SelectedRole;
                if (selectedRole == null)
                    return;

                var assumeRolePolicy = WebUtility.UrlDecode(selectedRole.AssumeRolePolicyDocument);
                if (!LambdaUtilities.DoesAssumeRolePolicyDocumentContainsInvalidAccounts(assumeRolePolicy))
                    return;

                stepName = "hasInvalidAccount";
                var cleanTrustPolicy = LambdaUtilities.RemoveInvalidAccountsFromAssumeRolePolicyDocument(assumeRolePolicy);
                stepName = "cleanedPolicy";

                var confirmControl = new ConfirmRoleCleanupControl(selectedRole.RoleName, assumeRolePolicy, cleanTrustPolicy);
                if (!ToolkitFactory.Instance.ShellProvider.ShowModal(confirmControl, MessageBoxButton.YesNo))
                {
                    metricEventState = "Skipped";
                    return;
                }
                stepName = "userConfirmed";

                account = PageController.HostingWizard.GetSelectedAccount(UploadFunctionWizardProperties.UserAccount);
                region = PageController.HostingWizard.GetSelectedRegion(UploadFunctionWizardProperties.Region);

                if (account == null || region == null)
                {
                    return;
                }

                using (var iamClient = account.CreateServiceClient<Amazon.IdentityManagement.AmazonIdentityManagementServiceClient>(region))
                {
                    stepName = "createdClient";

                    iamClient.UpdateAssumeRolePolicy(new IdentityManagement.Model.UpdateAssumeRolePolicyRequest
                    {
                        RoleName = selectedRole.RoleName,
                        PolicyDocument = cleanTrustPolicy
                    });
                    stepName = "calledIam";

                    selectedRole.AssumeRolePolicyDocument = WebUtility.UrlEncode(cleanTrustPolicy);

                    metricEventState = "Success";
                }
                stepName = "completed";
            }
            catch (Exception ex)
            {
                metricEventState = "Error-";
                if (ex is AmazonServiceException)
                    metricEventState += ((AmazonServiceException)ex).StatusCode + "-" + ((AmazonServiceException)ex).ErrorCode + "-" + stepName;
                else
                    metricEventState += ex.GetType().FullName + "-" + stepName;

                if (selectedRole != null)
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError(
                        "Error attempting to fix the trust policy for IAM Role " + selectedRole.RoleName + ". To manually fix the trust policy log on to " +
                        "the AWS Web Console and navigate to the IAM role. In the \"Trust Relationships\" tab remove all " +
                        "trust entities except for the principal \"lambda.amazonaws.com\".");
                }

                
                LOGGER.Error("Error attempting to clean up IAM Role trusted policy", ex);
            }
            finally
            {
                if (!string.IsNullOrEmpty(metricEventState))
                {
                    LambdaTelemetryUtils.RecordLambdaIamRoleCleanup(
                        _telemetryLogger,
                        string.Equals(metricEventState, "Success") ? Result.Succeeded : Result.Failed,
                        metricEventState,
                        account?.ToolkitContext.ServiceClientManager.GetAccountId(new AwsConnectionSettings(account?.Identifier, region)),
                        region?.Id);
                }
            }
        }

        public void RefreshPageContent()
        {
            var hostWizard = PageController.HostingWizard;

            if (hostWizard.IsPropertySet(UploadFunctionWizardProperties.MemorySize))
            {
                _ctlMemory.SelectedValue = hostWizard[UploadFunctionWizardProperties.MemorySize];
            }

            if (hostWizard.IsPropertySet(UploadFunctionWizardProperties.Timeout))
            {
                _ctlTimeout.Text = ((int)hostWizard[UploadFunctionWizardProperties.Timeout]).ToString();
            }

            if (hostWizard.IsPropertySet(UploadFunctionWizardProperties.EnvironmentVariables))
            {
                if (hostWizard[UploadFunctionWizardProperties.EnvironmentVariables] is ICollection<EnvironmentVariable> variables)
                {
                    EnvironmentVariables.Clear();
                    foreach (var v in variables)
                    {
                        EnvironmentVariables.Add(v);
                    }
                }
            }

            string role = null;
            if (hostWizard.IsPropertySet(UploadFunctionWizardProperties.Role))
            {
                role = hostWizard[UploadFunctionWizardProperties.Role] as string;
            }

            IntializeIAMPickerForAccountAsync(role);
        }

        private IAMRolePicker IAMPicker => this._ctlIAMRolePicker;

        public Amazon.IdentityManagement.Model.Role SelectedRole
        {
            get
            {
                if (IAMPicker == null)
                    return null;

                return IAMPicker.SelectedRole;
            }
        }

        public Amazon.IdentityManagement.Model.ManagedPolicy SelectedManagedPolicy
        {
            get
            {
                if (IAMPicker == null || IAMPicker.SelectedManagedPolicy == null)
                    return null;

                return IAMPicker.SelectedManagedPolicy;
            }
        }

        public int Memory => _ctlMemory != null ? (int)_ctlMemory.SelectedValue : -1;


        public int Timeout => _ctlTimeout != null ? int.Parse(_ctlTimeout.Text) : -1;

        public ICollection<EnvironmentVariable> SelectedEnvironmentVariables
        {
            get
            {
                var variables = new List<EnvironmentVariable>();
                foreach(var env in this.EnvironmentVariables)
                {
                    if (!string.IsNullOrWhiteSpace(env.Variable))
                        variables.Add(env);
                }
                return variables;
            }
        }

        public KeyListEntry SelectedKMSKey => _ctlKMSKey.SelectedKey;

        private void IntializeIAMPickerForAccountAsync(string selectedRole)
        {
            // could check here if we're already bound to this a/c and region
            var account = PageController.HostingWizard.GetSelectedAccount(UploadFunctionWizardProperties.UserAccount);
            var region = PageController.HostingWizard.GetSelectedRegion(UploadFunctionWizardProperties.Region);

            if (account == null || region == null)
            {
                return;
            }

            using (var iamClient = account.CreateServiceClient<Amazon.IdentityManagement.AmazonIdentityManagementServiceClient>(region))
            {
                var promptInfo = new RoleHelper.PromptRoleInfo
                {
                    AssumeRolePrincipal = Amazon.Common.DotNetCli.Tools.Constants.LAMBDA_PRINCIPAL,
                    AWSManagedPolicyNamePrefix = Amazon.Lambda.Tools.LambdaConstants.AWS_LAMBDA_MANAGED_POLICY_PREFIX,
                    KnownManagedPolicyDescription = Amazon.Lambda.Tools.LambdaConstants.KNOWN_MANAGED_POLICY_DESCRIPTIONS
                };

                var taskRole = RoleHelper.FindExistingRolesAsync(iamClient, promptInfo.AssumeRolePrincipal, int.MaxValue);
                var taskPolicies = RoleHelper.FindManagedPoliciesAsync(iamClient, promptInfo, RoleHelper.DEFAULT_ITEM_MAX);
                IList<Amazon.IdentityManagement.Model.Role> roles = null;
                IList<Amazon.IdentityManagement.Model.ManagedPolicy> policies = null;

                var errorMessages = new List<string>();
                try
                {
                    Task.WaitAll(taskRole, taskPolicies);
                    roles = taskRole.Result;
                }
                catch(AggregateException e)
                {
                    foreach(var inner in e.InnerExceptions)
                    {
                        if(!(inner is AggregateException))
                        {
                            errorMessages.Add(inner.Message);
                        }
                    }

                }
                catch(Exception e)
                {
                    errorMessages.Add(e.Message);
                }

                if(taskRole.IsCompleted && taskRole .Exception == null)
                {
                    roles = taskRole.Result;
                }
                if (taskPolicies.IsCompleted && taskPolicies.Exception == null)
                {
                    policies = taskPolicies.Result;
                }

                if (roles != null)
                {
                    this._ctlIAMRolePicker.Initialize(roles, policies, selectedRole);
                }
                else
                {
                    var finalErrorMessage = "Failed to retrieve list of IAM roles and policies. Your profile must have the permissions iam:ListRoles and iam:ListPolicies.";

                    foreach(var message in errorMessages)
                    {
                        finalErrorMessage += $"\n\n  {message}";
                    }

                    ToolkitFactory.Instance.ShellProvider.ShowError("Loading Roles Error", finalErrorMessage);
                }
            }
        }

        private void PreviewIntTextInput(object sender, TextCompositionEventArgs e)
        {
            int i;
            e.Handled = !int.TryParse(e.Text, out i);
        }

        /// <summary>
        /// Indicates that the user has completed all required fields, but not necessarily
        /// with full validity - this is called to enable the Next/Upload buttons. When the
        /// user clicks one of those, then we'll issue any additional messaging for their
        /// consideration.
        /// </summary>
        public bool AllRequiredFieldsAreSet
        {
            get
            {
                if (this.IAMPicker.SelectedRole == null && this.IAMPicker.SelectedManagedPolicy == null)
                {
                    return false;
                }

                var subnetSelections = _ctlVpcSubnets.SelectedSubnets;
                if (subnetSelections.Any())
                {
                    // do an additional check that we're still not pending the user
                    // correcting subnet selections that span vpcs
                    if (_ctlVpcSubnets.SubnetsSpanVPCs)
                        return false;

                    // if no security groups are selected, then we can't go yet
                    if (!_ctlSecurityGroups.SelectedSecurityGroups.Any())
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Called when the user presses Next/Upload, validates the field settings and displays
        /// warnings if any need to be fixed or re-considered. This is only called on forward
        /// navigation, so we know the required fields have been set by this point.
        /// </summary>
        public bool IsValid => true;

        private void AddVariable_Click(object sender, RoutedEventArgs e)
        {
            EnvironmentVariables.Add(new EnvironmentVariable());
            // todo: usability tweak here - put focus into the new key cell...
        }

        private void RemoveVariable_Click(object sender, RoutedEventArgs e)
        {
            EnvironmentVariable cellData = _ctlEnvironmentVariables.CurrentCell.Item as EnvironmentVariable;
            for (int i = EnvironmentVariables.Count - 1; i >= 0; i--)
            {
                if (string.Compare(EnvironmentVariables[i].Variable, cellData.Variable, true) == 0)
                {
                    EnvironmentVariables.RemoveAt(i);
                    NotifyPropertyChanged("EnvironmentVariables");
                    return;
                }
            }
        }

        // used to trap attempts to create a duplicate variable
        private void _ctlEnvironmentVariables_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Cancel)
                return;

            TextBox editBox = e.EditingElement as TextBox;
            if (editBox == null)
            {
                LOGGER.ErrorFormat("Expected but did not receive TextBox EditingElement type for CellEditEnding event at row {0} column {1}; cannot validate for dupes.",
                                    e.Row.GetIndex(), e.Column.DisplayIndex);
                return;
            }

            string pendingEntry = editBox.Text;

            EnvironmentVariable cellData = _ctlEnvironmentVariables.CurrentCell.Item as EnvironmentVariable;
            if (cellData != null)
            {
                foreach (EnvironmentVariable ev in EnvironmentVariables)
                {
                    if (ev != cellData && string.Compare(ev.Variable, pendingEntry, true) == 0)
                    {
                        e.Cancel = true;
                        MessageBox.Show(string.Format("A value already exists for variable '{0}'.", pendingEntry),
                                        "Duplicate Variable", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            NotifyPropertyChanged("EnvironmentVariables");
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            var runtime = PageController.HostingWizard[UploadFunctionWizardProperties.Runtime] as string;

            if (runtime != null && runtime.StartsWith("dotnetcore", StringComparison.OrdinalIgnoreCase))
                Amazon.AWSToolkit.Utility.LaunchXRayHelp(true);
            else
                Amazon.AWSToolkit.Utility.LaunchXRayHelp(false);
        }
    }

    public class EnvironmentVariable
    {
        public string Variable { get; set; }
        public string Value { get; set; }
    }
}
