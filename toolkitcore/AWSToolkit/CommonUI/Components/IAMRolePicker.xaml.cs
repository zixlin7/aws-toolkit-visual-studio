using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;

using log4net;
using Amazon.AWSToolkit.Account;
using System.Threading.Tasks;

namespace Amazon.AWSToolkit.CommonUI.Components
{
    /// <summary>
    /// Interaction logic for IAMRolePicker.xaml
    /// </summary>
    public partial class IAMRolePicker : UserControl
    {
        const string AWS_MANANGED_POLICY_ARN_PREFIX = "arn:aws:iam::aws:policy";

        static readonly ILog LOGGER = LogManager.GetLogger(typeof(IAMRolePicker));


        TextBlock _ctlComboSelectedDisplay;
        string _existingRoleValue;

        IList<Role> _availableRoles;
        IList<ManagedPolicy> _availableManagedPolicies;


        public event PropertyChangedEventHandler PropertyChanged;

        public IAMRolePicker()
        {
            InitializeComponent();
            this._ctlCombo.Loaded += _ctlCombo_Loaded;
        }

        void _ctlCombo_Loaded(object sender, RoutedEventArgs e)
        {
            var contentPresenter = this._ctlCombo.Template.FindName("ContentSite", this._ctlCombo) as ContentPresenter;
            if (contentPresenter != null)
            {
                this._ctlComboSelectedDisplay = contentPresenter.ContentTemplate.FindName("_ctlComboSelectedDisplay", contentPresenter) as TextBlock;
                if(this._ctlCombo.SelectedItem != null)
                {
                    FormatDisplayValue();
                }
            }
        }

        public Func<Role, string, bool> RoleFilter
        {
            get;
            set;
        }

        public void Initialize(IList<Role> availableRoles, IList<ManagedPolicy> availableManagedPolicies, string selectedRole)
        {
            this._availableManagedPolicies = availableManagedPolicies ?? new List<ManagedPolicy>();
            this._availableRoles = availableRoles ?? new List<Role>();

            this.PopulateComboBox(selectedRole);
        }

        public void SelectExistingRole(string role)
        {
            this._existingRoleValue = role;

            if (role == null)
            {
                this._ctlCombo.SelectedItem = null;
            }
            else
            {
                IAMPickerItem selectedItem = null;
                foreach (IAMPickerItem item in this._ctlCombo.Items)
                {
                    if (item.ExistingRole != null && IsRoleMatch(item.ExistingRole, role))
                    {
                        selectedItem = item;
                        break;
                    }
                }

                if (selectedItem == null)
                {
                    var tokens = role.Split('/');
                    var roleName = tokens[tokens.Length - 1];
                    selectedItem = new IAMPickerItem {ExistingRole = new Role { RoleName = roleName, Arn = role }, IsSelected = true};

                    this._ctlCombo.Items.Insert(1, selectedItem);
                }

                this._ctlCombo.SelectedItem = selectedItem;
            }
        }

        private bool IsRoleMatch(Role role, string identity)
        {
            if (string.IsNullOrEmpty(identity))
                return false;

            if (string.Equals(role.RoleName, identity, StringComparison.InvariantCultureIgnoreCase))
                return true;
            if (string.Equals(role.Arn, identity, StringComparison.InvariantCultureIgnoreCase))
                return true;

            return false;
        }

        private void PopulateComboBox(string selectedRole)
        {
            try
            { 
                ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((Action)(() =>
                {
                    this._ctlCombo.Items.Clear();

                    this._ctlCombo.Items.Add(new IAMPickerItem() { Description = "Existing IAM Roles" });
                    foreach (var item in this._availableRoles)
                    {
                        var pickerItem = new IAMPickerItem() { ExistingRole = item };
                        this._ctlCombo.Items.Add(pickerItem);
                    }

                    var awsManaged = _availableManagedPolicies.Where(x => x.Arn.StartsWith(AWS_MANANGED_POLICY_ARN_PREFIX));
                    if(awsManaged.Count() > 0)
                    {
                        this._ctlCombo.Items.Add(new IAMPickerItem() { Description = "New Role Based on AWS Managed Policy" });
                        foreach (var policy in awsManaged)
                        {
                            this._ctlCombo.Items.Add(new IAMPickerItem() { Policy = policy });
                        }
                    }

                    var accountManaged = _availableManagedPolicies.Where(x => !x.Arn.StartsWith(AWS_MANANGED_POLICY_ARN_PREFIX));
                    if (accountManaged.Count() > 0)
                    {
                        this._ctlCombo.Items.Add(new IAMPickerItem() { Description = "New Role Based on Customer Managed Policy" });
                        foreach (var policy in accountManaged)
                        {

                            this._ctlCombo.Items.Add(new IAMPickerItem() { Policy = policy });
                        }
                    }

                    FormatDisplayValue();

                    if (selectedRole != null)
                    {
                        this.SelectExistingRole(selectedRole);
                    }
                }));
            }
            catch (Exception e)
            {
                LOGGER.Error("Error loading iam entities", e);
            }
        }

        public Role SelectedRole
        {
            get
            {
                var item = this._ctlCombo.SelectedItem as IAMPickerItem;
                if (item == null)
                    return null;

                return item.ExistingRole;
            }
        }


        public ManagedPolicy SelectedManagedPolicy
        {
            get
            {
                var item = this._ctlCombo.SelectedItem as IAMPickerItem;
                if (item == null)
                    return null;

                return item.Policy;
            }
        }

        private void _ctlCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FormatDisplayValue();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RoleSelection"));
        }

        private void FormatDisplayValue()
        {
            if (this._ctlComboSelectedDisplay == null) return;

            if (this._ctlCombo.SelectedItem != null)
            {
                if (((IAMPickerItem)this._ctlCombo.SelectedItem).ExistingRole != null)
                {
                    this._ctlComboSelectedDisplay.Text = "Existing role: " + ((IAMPickerItem)this._ctlCombo.SelectedItem).ExistingRole.RoleName;
                }
                if (((IAMPickerItem)this._ctlCombo.SelectedItem).Policy != null)
                {
                    var policy = ((IAMPickerItem)this._ctlCombo.SelectedItem).Policy;
                    if(policy.Arn.StartsWith(AWS_MANANGED_POLICY_ARN_PREFIX))
                        this._ctlComboSelectedDisplay.Text =  "New role based on AWS managed policy: " + policy.PolicyName;
                    else 
                        this._ctlComboSelectedDisplay.Text = "New role based on customer managed policy: " + policy.PolicyName;
                }
            }
            else
            {
                this._ctlComboSelectedDisplay.Text = string.Empty;
            }
        }



        #region Supporting Classes


        public class IAMPickerChangedEventArgs
        {
            public IAMPickerItem PickedItem { get; set; }
        }

        public class IAMPickerItem : INotifyPropertyChanged
        {


            public IAMPickerItem()
            {
            }

            #region INotifyPropertyChanged
            public event PropertyChangedEventHandler PropertyChanged;

            // Create the OnPropertyChanged method to raise the event 
            protected void OnPropertyChanged(string name)
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs(name));
                }
            }
            #endregion

            public bool IsSelectable
            {
                get
                {
                    return string.IsNullOrEmpty(this.Description);
                }
            }

            bool _isSelected;
            public bool IsSelected
            {
                get { return this._isSelected; }
                set
                {
                    this._isSelected = value;
                    OnPropertyChanged("IsSelected");
                }
            }

            public string DisplayName
            {
                get
                {
                    if (this.Policy != null)
                        return this.Policy.PolicyName;
                    if (this.ExistingRole != null)
                        return this.ExistingRole.RoleName;

                    return this.Description;
                }
            }

            public string DropDownItemDisplayName
            {
                get
                {
                    if (this.Policy != null)
                    {
                        var description = AttemptToGetPolicyDescription(this.Policy.Arn);
                        if (!string.IsNullOrEmpty(description))
                            return string.Format("{0} ({1})", this.Policy.PolicyName, description);
                        return this.Policy.PolicyName;
                    }
                    if (this.ExistingRole != null)
                        return this.ExistingRole.RoleName;

                    return this.Description;
                }
            }


            public Role ExistingRole
            {
                get;
                set;
            }

            public ManagedPolicy Policy
            {
                get;
                set;
            }

            public string Description
            {
                get;
                set;
            }

            public int ItemType
            {
                get
                {
                    if (!string.IsNullOrEmpty(this.Description))
                        return 1;
                    if (this.Policy != null)
                        return 2;

                    return 3;
                }
            }

            static readonly Dictionary<string, string> KNOWN_MANAGED_POLICY_DESCRIPTIONS = new Dictionary<string, string>
            {
                {"arn:aws:iam::aws:policy/PowerUserAccess","Provides full access to AWS services and resources, but does not allow management of users and groups."},
                {"arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole","Provides write permissions to CloudWatch Logs."},
                {"arn:aws:iam::aws:policy/service-role/AWSLambdaDynamoDBExecutionRole","Provides list and read access to DynamoDB streams and write permissions to CloudWatch Logs."},
                {"arn:aws:iam::aws:policy/AWSLambdaExecute","Provides Put, Get access to S3 and full access to CloudWatch Logs."},
                {"arn:aws:iam::aws:policy/AWSLambdaFullAccess","Provides full access to Lambda, S3, DynamoDB, CloudWatch Metrics and Logs."},
                {"arn:aws:iam::aws:policy/AWSLambdaInvocation-DynamoDB","Provides read access to DynamoDB Streams."},
                {"arn:aws:iam::aws:policy/service-role/AWSLambdaKinesisExecutionRole","Provides list and read access to Kinesis streams and write permissions to CloudWatch Logs."},
                {"arn:aws:iam::aws:policy/AWSLambdaReadOnlyAccess","Provides read only access to Lambda, S3, DynamoDB, CloudWatch Metrics and Logs."},
                {"arn:aws:iam::aws:policy/service-role/AWSLambdaRole","Default policy for AWS Lambda service role."},
                {"arn:aws:iam::aws:policy/service-role/AWSLambdaSQSQueueExecutionRole","Provides receive message, delete message, and read attribute access to SQS queues, and write permissions to CloudWatch logs."},
                {"arn:aws:iam::aws:policy/service-role/AWSCodeDeployRoleForLambda","Provides CodeDeploy service access to perform a Lambda deployment on your behalf."},
                {"arn:aws:iam::aws:policy/service-role/AWSLambdaENIManagementAccess","Provides minimum permissions for a Lambda function to manage ENIs (create, describe, delete) used by a VPC-enabled Lambda Function."},
                {"arn:aws:iam::aws:policy/AWSDeepLensLambdaFunctionAccessPolicy","This policy specifies permissions required by DeepLens Administrative lambda functions that run on a DeepLens device"},
                {"arn:aws:iam::aws:policy/service-role/AWSLambdaVPCAccessExecutionRole","Provides minimum permissions for a Lambda function to execute while accessing a resource within a VPC"}
            };

            /// <summary>
            /// Because description does not come back in the list policy operation cache known lambda policy descriptions to 
            /// help users understand which role to pick.
            /// </summary>
            /// <param name="policyArn"></param>
            /// <returns></returns>
            public string AttemptToGetPolicyDescription(string policyArn)
            {
                string content;
                if (!KNOWN_MANAGED_POLICY_DESCRIPTIONS.TryGetValue(policyArn, out content))
                    return null;

                return content;
            }
        }    
 
        #endregion

    }
}
