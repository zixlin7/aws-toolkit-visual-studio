using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.ComponentModel;

using Amazon.RDS.Model;
using Amazon.AWSToolkit.RDS.Model;

namespace Amazon.AWSToolkit.RDS.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for ModifyDBInstancePage.xaml
    /// </summary>
    public partial class ModifyDBInstancePage : INotifyPropertyChanged
    {
        const string AllocatedStorageLabelFormat = "(Minimum: {0} GB, Maximum {1} GB)";
        const string PASSWORD_MISMATCH = "Passwords do not match";

        readonly ObservableCollection<DBEngineVersion> _engineVersions = new ObservableCollection<DBEngineVersion>();
        readonly ObservableCollection<SelectableGroup<SecurityGroupInfo>> _securityGroups = new ObservableCollection<SelectableGroup<SecurityGroupInfo>>();
        readonly ObservableCollection<DBParameterGroup> _parameterGroups = new ObservableCollection<DBParameterGroup>();

        public const string propkey_EngineVersion = "engineVersion";
        public const string propkey_InstanceClass = "instanceClass";

        public ModifyDBInstancePage()
        {
            DataContext = this;
            InitializeComponent();
        }

        public DBInstanceWrapper InstanceWrapper { get; set; }
        
        DBEngineMeta EngineMeta { get; set; }

        public void RefreshContent()
        {
            EngineMeta = RDSServiceMeta.Instance.MetaForEngine(InstanceWrapper.Engine);

            _instanceClass.ItemsSource = EngineMeta.SupportedInstanceClasses;
            foreach (var instanceClass in EngineMeta.SupportedInstanceClasses)
            {
                if (string.Compare(instanceClass.Id, 
                                   InstanceWrapper.DBInstanceClass, 
                                   StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    _instanceClass.SelectedItem = instanceClass;
                    break;
                }
            }

            // minimum allowed on running instance is the current allocation, not the engine minimum
            _lblStorageLimits.Text = string.Format(AllocatedStorageLabelFormat, InstanceWrapper.NativeInstance.AllocatedStorage, EngineMeta.MaxStorageAlloc);
            _allocatedStorage.Text = InstanceWrapper.NativeInstance.AllocatedStorage.ToString();

            _txtMasterUserPassword.MaxLength = EngineMeta.MaxMasterPwdNameLength;

            _btnMultiAZDeployment.IsChecked = InstanceWrapper.MultiAZ;
            _btnMultiAZDeployment.IsEnabled = EngineMeta.SupportsMultiAZ;

            _passwordValidationMsg.Text = string.Empty;
        }

        public ObservableCollection<DBEngineVersion> EngineVersions { get { return _engineVersions; } }

        public void SetAvailableEngineVersions(IEnumerable<DBEngineVersion> engineVersions)
        {
            _engineVersions.Clear();
            foreach (var v in engineVersions)
            {
                _engineVersions.Add(v);
            }

            // preselect current engine
            foreach (var v in engineVersions)
            {
                if (string.Compare(InstanceWrapper.NativeInstance.EngineVersion,
                                    v.EngineVersion,
                                    StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    _dbEngineVersions.SelectedItem = v;
                    break;
                }
            }
        }

        public ObservableCollection<SelectableGroup<SecurityGroupInfo>> SecurityGroups { get { return _securityGroups; } }

        public List<SecurityGroupInfo> SelectedSecurityGroups
        {
            get
            {
                return (from g in _securityGroups where g.IsSelected select g.InnerObject).ToList();
            }
        }

        internal void SetSecurityGroups(IEnumerable<DBSecurityGroup> securityGroups)
        {
            _securityGroups.Clear();
            if (securityGroups != null)
            {
                var inUse = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (InstanceWrapper.NativeInstance.VpcSecurityGroups != null)
                {
                    foreach (var group in InstanceWrapper.NativeInstance.DBSecurityGroups)
                    {
                        inUse.Add(group.DBSecurityGroupName);
                    }
                }

                foreach (var sg in securityGroups)
                {
                    var groupInfo = new SecurityGroupInfo
                    {
                        Name = sg.DBSecurityGroupName,
                        Description = sg.DBSecurityGroupDescription
                    };
                    var selectableGroup = new SelectableGroup<SecurityGroupInfo>(groupInfo);
                    if (inUse.Contains(groupInfo.Name))
                        selectableGroup.IsSelected = true;
                    _securityGroups.Add(selectableGroup);
                }
            }

            _dbSecurityGroups.IsEnabled = _securityGroups.Count() > 0;
        }

        internal void SetSecurityGroups(IEnumerable<Amazon.EC2.Model.SecurityGroup> securityGroups)
        {
            _securityGroups.Clear();
            if (securityGroups != null)
            {
                var inUse = new HashSet<string>();
                if (InstanceWrapper.NativeInstance.VpcSecurityGroups != null)
                {
                    foreach (var group in InstanceWrapper.NativeInstance.VpcSecurityGroups)
                    {
                        inUse.Add(group.VpcSecurityGroupId);
                    }
                }

                foreach (var sg in securityGroups)
                {
                    var groupInfo = new SecurityGroupInfo
                    {
                        Id = sg.GroupId,
                        Description = sg.Description
                    };
                    var selectableGroup = new SelectableGroup<SecurityGroupInfo>(groupInfo);
                    if (inUse.Contains(groupInfo.Id))
                        selectableGroup.IsSelected = true;
                    _securityGroups.Add(selectableGroup);
                }
            }

            _dbSecurityGroups.IsEnabled = _securityGroups.Count() > 0;
        }

        public ObservableCollection<DBParameterGroup> ParameterGroups {  get { return _parameterGroups; } }

        public void SetAvailableParameterGroups(IEnumerable<DBParameterGroup> parameterGroups)
        {
            _parameterGroups.Clear();

            string preselectGroupName = null;
            if (InstanceWrapper.NativeInstance.DBParameterGroups != null
                    && InstanceWrapper.NativeInstance.DBParameterGroups.Count == 1)
                preselectGroupName = InstanceWrapper.NativeInstance.DBParameterGroups[0].DBParameterGroupName;

            DBParameterGroup selectedGroup = null;
            foreach (var group in parameterGroups)
            {
                _parameterGroups.Add(group);
                if (preselectGroupName != null 
                        && string.Compare(preselectGroupName, group.DBParameterGroupName, StringComparison.InvariantCultureIgnoreCase) == 0)
                    selectedGroup = group;
            }

            if (selectedGroup != null)
                _dbParamGroups.SelectedItem = selectedGroup;
            _dbParamGroups.IsEnabled = _parameterGroups.Any();
        }

        public DBEngineVersion SelectedVersion
        {
            get
            {
                if (!IsInitialized)
                    return null;

                return this._dbEngineVersions.SelectedItem as DBEngineVersion;
            }
        }

        public DBInstanceClass InstanceClass
        {
            get { return this._instanceClass.SelectedItem as DBInstanceClass; }
        }

        public bool IsMultiAZ
        {
            get { return this._btnMultiAZDeployment.IsEnabled && this._btnMultiAZDeployment.IsChecked == true; }
        }

        public bool AutoUpgradeMinorVersions
        {
            get { return this._btnAutoUpgradeMinorVersions.IsChecked == true; }
        }

        public int Storage
        {
            get
            {
                int storage;
                if (!string.IsNullOrEmpty(_allocatedStorage.Text)
                        && int.TryParse(_allocatedStorage.Text, out storage))
                {
                    // minimum allowed on running instance is the current allocation, not the engine minimum
                    if (storage >= InstanceWrapper.NativeInstance.AllocatedStorage && storage < EngineMeta.MaxStorageAlloc)
                        return storage;
                }

                return -1;
            }
        }

        public bool IsPasswordValid
        {
            get
            {
                return PasswordAndConfirmationMatch(_txtMasterUserPassword.Password, _txtPasswordConfirmation.Password) == true
                        && string.IsNullOrEmpty(PasswordMeetsEngineRequirements(_txtMasterUserPassword.Password));
            }
        }

        public string MasterUserPassword
        {
            get { return _txtMasterUserPassword.Password; }
        }

        // return null on no selection so we can detect 'no change/don't care' scenario
        public string DBParameterGroup
        {
            get
            {
                if (_dbParamGroups.SelectedItem != null)
                    return (_dbParamGroups.SelectedItem as DBParameterGroup).DBParameterGroupName;
                else
                    return null;
            }
        }

        private void _dbEngineVersions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged(propkey_EngineVersion);
        }

        private void _instanceClass_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged(propkey_InstanceClass);
        }

        private void _allocatedStorage_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // may be paste of a string, so try and convert rather than use IsDigit
            int num;
            if (!int.TryParse(e.Text, out num))
                e.Handled = true;
        }

        private void _allocatedStorage_TextChanged(object sender, TextChangedEventArgs e)
        {
            var fore = FindResource("awsDefaultControlForegroundBrushKey") as SolidColorBrush;
            int num;
            if (int.TryParse(_allocatedStorage.Text, out num))
            {
                // minimum allowed on running instance is the current allocation, not the engine minimum
                if (num < InstanceWrapper.NativeInstance.AllocatedStorage || num > EngineMeta.MaxStorageAlloc)
                    fore = Brushes.Red;
            }
            else
                fore = Brushes.Red;
            _lblStorageLimits.Foreground = fore;

            NotifyPropertyChanged("storage");
        }

        private void OnPasswordFieldsChanged(object sender, RoutedEventArgs e)
        {
            bool? testResult = PasswordAndConfirmationMatch(_txtMasterUserPassword.Password, _txtPasswordConfirmation.Password);

            if (testResult == false)
                _passwordValidationMsg.Text = PASSWORD_MISMATCH;
            else
            {
                if (!string.IsNullOrEmpty(_txtMasterUserPassword.Password))
                    _passwordValidationMsg.Text = PasswordMeetsEngineRequirements(_txtMasterUserPassword.Password);
                else
                    _passwordValidationMsg.Text = string.Empty;
            }
            
            NotifyPropertyChanged("password");
        }

        internal bool? PasswordAndConfirmationMatch(string password, string confirmation)
        {
            if (!string.IsNullOrEmpty(_txtMasterUserPassword.Password) && !string.IsNullOrEmpty(_txtPasswordConfirmation.Password))
                return string.Compare(_txtMasterUserPassword.Password, _txtPasswordConfirmation.Password, false) == 0;
            else
                return null;
        }

        private string PasswordMeetsEngineRequirements(string password)
        {
            string validationMsg = string.Empty;

            if (EngineMeta.IsSqlServer)
            {
                if (password.Length < 8 || password.Length > 16)
                    validationMsg = "Password must contain 8 to 16 alphanumeric characters";
            }

            return validationMsg;
        }
    }
}
