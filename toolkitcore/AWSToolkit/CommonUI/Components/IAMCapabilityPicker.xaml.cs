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
    /// Interaction logic for IAMCapabilityPicker.xaml
    /// </summary>
    public partial class IAMCapabilityPicker : UserControl
    {
        public const string CLOUDWATCH_TEMPLATE = "CloudWatch Full Access";

        static readonly ILog LOGGER = LogManager.GetLogger(typeof(IAMCapabilityPicker));


        public enum IAMMode { InstanceProfiles, Roles };

        IAmazonIdentityManagementService _iamClient;
        RegionEndPointsManager.RegionEndPoints _region;
        TextBlock _ctlComboSelectedDisplay;
        HashSet<PolicyTemplate> _selectedPolicyTemplates = new HashSet<PolicyTemplate>();
        string[] _serviceSpecificProfiles;
        IAMMode _iamMode = IAMMode.InstanceProfiles;
        string _existingRoleValue;

        public event PropertyChangedEventHandler PropertyChanged;

        public IAMCapabilityPicker()
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
            }
        }

        public Func<Role, string, bool> RoleFilter
        {
            get;
            set;
        }

        public void Initialize(AccountViewModel account, RegionEndPointsManager.RegionEndPoints region, IAMMode iamMode, params string[] serviceSpecificProfiles)
        {
            this._iamMode = iamMode;
            this._region = region;
            this._iamClient = account.CreateServiceClient<AmazonIdentityManagementServiceClient>(region);

            this._serviceSpecificProfiles = serviceSpecificProfiles;


            if(this._iamMode == IAMMode.InstanceProfiles)
            {
                this._iamClient.ListInstanceProfilesAsync(new ListInstanceProfilesRequest()).ContinueWith(task =>
                {
                    if (task.Exception == null)
                    {
                        IList<IAMEntity> entities = new List<IAMEntity>();
                        foreach (var profile in task.Result.InstanceProfiles)
                        {
                            entities.Add(new IAMEntity(profile.InstanceProfileName, profile.Arn));
                        }

                        PopulateComboBox(entities);
                    }
                    else
                        LOGGER.Error("Error loading instance profiles", task.Exception);
                });
            }
            else if(this._iamMode == IAMMode.Roles)
            {
                this._iamClient.ListRolesAsync(new ListRolesRequest()).ContinueWith(task =>
                {
                    if (task.Exception == null)
                    {
                        IList<IAMEntity> entities = new List<IAMEntity>();
                        var lambdaPrincipal = _region.GetPrincipalForAssumeRole(RegionEndPointsManager.LAMBDA_SERVICE_NAME);
                        foreach (var role in task.Result.Roles)
                        {
                            if (this.RoleFilter == null || this.RoleFilter(role, lambdaPrincipal))
                                entities.Add(new IAMEntity(role.RoleName, role.Arn));
                        }

                        PopulateComboBox(entities);
                    }
                    else
                        LOGGER.Error("Error loading instance profiles", task.Exception);
                });
            }
        }

        public void SelectExistingRole(string role)
        {
            this._existingRoleValue = role;

            this._selectedPolicyTemplates.Clear();

            if (role == null)
            {
                this._ctlCombo.SelectedItem = null;
            }
            else
            {
                foreach (IAMPickerItem item in this._ctlCombo.Items)
                {
                    if (item.ExistingRole != null && item.ExistingRole.IsMatch(role))
                    {
                        this._ctlCombo.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        private void PopulateComboBox(IEnumerable<IAMEntity> iamEntities)
        {
            try
            {
                var templates = GetPolicyTemplates();
                ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
                {
                    this._ctlCombo.Items.Clear();
                    this._selectedPolicyTemplates.Clear();

                    this._ctlCombo.Items.Add(new IAMPickerItem() { Description = "Role Templates" });
                    foreach (var template in templates)
                    {
                        this._ctlCombo.Items.Add(new IAMPickerItem() { Template = template });
                    }

                    this._ctlCombo.Items.Add(new IAMPickerItem() { Description = "Existing IAM Roles" });

                    IAMPickerItem defaultItem = null;
                    if(this._serviceSpecificProfiles != null)
                    {
                        foreach(var profileName in this._serviceSpecificProfiles)
                        {
                            var profile = new IAMEntity() { Name = profileName };
                            var pickerItem = new IAMPickerItem() { ExistingRole = profile };
                            if (defaultItem == null)
                                defaultItem = pickerItem;

                            this._ctlCombo.Items.Add(pickerItem);
                        }
                    }

                    foreach (var item in iamEntities)
                    {
                        if(this._serviceSpecificProfiles != null && 
                            this._serviceSpecificProfiles.FirstOrDefault(x => string.Equals(x, item.Name)) != null)
                            continue;

                        var pickerItem = new IAMPickerItem() { ExistingRole = item };
                        this._ctlCombo.Items.Add(pickerItem);

                        if (item.IsMatch(this._existingRoleValue))
                            defaultItem = pickerItem;
                    }

                    FormatDisplayValue();

                    this._ctlCombo.SelectedItem = defaultItem;
                }));
            }
            catch (Exception e)
            {
                LOGGER.Error("Error loading iam entities", e);
            }
        }

        public IAMEntity SelectedRole
        {
            get
            {
                var item = this._ctlCombo.SelectedItem as IAMPickerItem;
                if (item == null)
                    return null;

                return item.ExistingRole;
            }
        }


        public PolicyTemplate[] SelectedPolicyTemplates
        {
            get
            {
                var templates = _selectedPolicyTemplates.OrderBy((x) => x.Name).ToArray();
                if (templates.Length == 0)
                    return null;

                return templates;
            }
        }



        private string GetPolicyTemplatesXML()
        {
            string content = S3FileFetcher.Instance.GetFileContent("IAMPolicyTemplates.xml", S3FileFetcher.CacheMode.PerInstance);
            return content;
        }

        public PolicyTemplate[] GetPolicyTemplates()
        {
            try
            {
                var doc = XDocument.Parse(GetPolicyTemplatesXML());

                IEnumerable<PolicyTemplate> templates =
                    from item in doc.Descendants("templates").Descendants("template")
                    select new PolicyTemplate
                    {
                        Name = item.Element("name") != null ? item.Element("name").Value : string.Empty,
                        Description = item.Element("description") != null ? item.Element("description").Value : string.Empty,
                        Body = item.Element("body") != null ? item.Element("body").Value.Trim() : string.Empty
                    };
                return templates.ToArray();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error loading policy templates", e);
                return new PolicyTemplate[0];
            }
        }

        private void CheckBox_Clicked(object sender, RoutedEventArgs e)
        {
            if (this._ctlCombo.SelectedItem != null && ((IAMPickerItem)this._ctlCombo.SelectedItem).ExistingRole != null)
                this._ctlCombo.SelectedItem = null;

            var cb = e.Source as CheckBox;
            var picker = cb.DataContext as IAMPickerItem;

            if (cb.IsChecked.GetValueOrDefault())
                _selectedPolicyTemplates.Add(picker.Template);
            else
                _selectedPolicyTemplates.Remove(picker.Template);

            FormatDisplayValue();

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RoleSelection"));
        }

        private void _ctlCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this._ctlCombo.SelectedItem != null)
            {
                var pickedItem = (IAMPickerItem)this._ctlCombo.SelectedItem;
                if (pickedItem.ExistingRole != null)
                {
                    foreach (IAMPickerItem item in this._ctlCombo.Items)
                    {
                        item.IsSelected = false;
                    }
                    this._selectedPolicyTemplates.Clear();
                }
                if (pickedItem.Template != null)
                {
                    if (!this._selectedPolicyTemplates.Contains(pickedItem.Template))
                    {
                        pickedItem.IsSelected = true;
                        _selectedPolicyTemplates.Add(pickedItem.Template);
                    }
                }
            }
            FormatDisplayValue();

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RoleSelection"));
        }

        private void FormatDisplayValue()
        {
            if (this._ctlComboSelectedDisplay == null) return;

            if (this._ctlCombo.SelectedItem != null && ((IAMPickerItem)this._ctlCombo.SelectedItem).ExistingRole != null)
            {
                this._ctlComboSelectedDisplay.Text = ((IAMPickerItem)this._ctlCombo.SelectedItem).ExistingRole.Name;
            }
            else
            {
                var sb = new StringBuilder();
                foreach (var template in _selectedPolicyTemplates.OrderBy((x) => x.Name))
                {
                    if (sb.Length > 0)
                        sb.Append(", ");
                    sb.Append(template.Name);
                }

                this._ctlComboSelectedDisplay.Text = sb.ToString();
            }
        }



        #region Supporting Classes

        public class PolicyTemplate
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Body { get; set; }

            public string IAMCompatibleName
            {
                get
                {
                    return this.Name.Replace(' ', '-');
                }
            }
        }

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
                    if (this.Template != null)
                        return this.Template.Name;
                    if (this.ExistingRole != null)
                        return this.ExistingRole.Name;

                    return this.Description;
                }
            }

            public IAMEntity ExistingRole
            {
                get;
                set;
            }

            public PolicyTemplate Template
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
                    if (this.Template != null)
                        return 2;

                    return 3;
                }
            }            
        }

        public class IAMEntity
        {
            public IAMEntity() { }

            public IAMEntity(string name, string arn)
            {
                this.Name = name;
                this.Arn = arn;
            }

            public string Name { get; set; }
            public string Arn { get; set; }

            public bool IsMatch(string identity)
            {
                if (string.IsNullOrEmpty(identity))
                    return false;

                if (string.Equals(this.Name, identity, StringComparison.InvariantCultureIgnoreCase))
                    return true;
                if (string.Equals(this.Arn, identity, StringComparison.InvariantCultureIgnoreCase))
                    return true;

                return false;
            }
        }

        #endregion

    }
}
