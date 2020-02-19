using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.RDS.Controller;
using Amazon.AWSToolkit.RDS.Model;
using Amazon.AWSToolkit.Util;
using Amazon.EC2.Model;
using Amazon.RDS.Model;
using EC2AvailabilityZone = Amazon.EC2.Model.AvailabilityZone;
using System.Text;

namespace Amazon.AWSToolkit.RDS.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for LaunchDBInstanceAdvancedSettingsPage.xaml
    /// </summary>
    public partial class LaunchDBInstanceAdvancedSettingsPage :  INotifyPropertyChanged
    {
        public const string CreateNewDbSubnetGroupText = "Create new DB Subnet Group";
        public const string DefaultDbSubnetGroupText = "default";

        public const string NoPreferenceAvailablityZoneText = "No Preference";

        readonly List<DBSubnetGroupWrapper> _allDBSubnetGroups = new List<DBSubnetGroupWrapper>();
        readonly HashSet<string> _vpcsWithDBSubnets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        readonly ObservableCollection<VPCWrapper> _vpcList = new ObservableCollection<VPCWrapper>();
        readonly ObservableCollection<DBSubnetGroupWrapper> _dbSubnetGroupList = new ObservableCollection<DBSubnetGroupWrapper>();
        readonly ObservableCollection<EC2AvailabilityZone> _availabilityZoneList = new ObservableCollection<EC2AvailabilityZone>();
        readonly ObservableCollection<SelectableGroup<SecurityGroupInfo>> _securityGroups = new ObservableCollection<SelectableGroup<SecurityGroupInfo>>();
        readonly ObservableCollection<DBParameterGroup> _dbParameterGroupList = new ObservableCollection<DBParameterGroup>();
        readonly ObservableCollection<string> _optionGroupList = new ObservableCollection<string>();

        private string _cidrEstimate = "";
        const string AddCurrentCidrTextFormat = "Add current IP (estimate {0}) to the selected security group(s)";

        const string PortRangeHintformat = "Port range: {0}-{1}";

        public LaunchDBInstanceAdvancedSettingsPage()
        {
            DataContext = this;
            InitializeComponent();
        }

        public void RefreshContent(string dbEngine, bool isMultiAzDeployment, bool isVpcOnly)
        {
            EngineMeta = RDSServiceMeta.Instance.MetaForEngine(dbEngine);

            _ctlDatabaseName.MaxLength = EngineMeta.MaxDBNameLength;
            _ctlDatabaseName.IsEnabled = !EngineMeta.IsSqlServer;

            if (EngineMeta.IsOracle)
                NameHint = "The Oracle System ID (SID) of the created DB instance.";
            else if (EngineMeta.IsSqlServer)
                NameHint = "Database name is not applicable for SQL Server instances.";
            else
                NameHint = "If no database name is specified then no initial database will be created in the DB instance.";
            OnPropertyChanged("NameHint");

            DatabasePort = EngineMeta.DefaultPort.ToString(CultureInfo.InvariantCulture);
            OnPropertyChanged("DatabasePort");

            PortHint = string.Format(PortRangeHintformat, EngineMeta.MinPort, EngineMeta.MaxPort);
            OnPropertyChanged("PortHint");

            var currentIp = IPAddressUtil.DetermineIPFromExternalSource();
            if (!string.IsNullOrEmpty(currentIp))
            {
                _cidrEstimate = currentIp;
                OnPropertyChanged("AddCurrentCidrEstimate");
            }

            SetAvailableVpcsAndDbSubnetGroups(null, null, isVpcOnly);

            _securityGroups.Clear();
            SetAvailabilityZoneList(null);
            SetDbParameterGroupList(null);
        }

        private DBEngineMeta EngineMeta { get; set; }

        #region Network and Security Settings

        public bool LaunchingIntoVpc => SelectedVpc != null && LaunchDBInstanceController.IsLaunchingIntoVPC(SelectedVpc.VpcId);

        public string NameHint { get; set; }
        public string PortHint { get; set; }

        public VPCWrapper SelectedVpc { get; set; }
        public ObservableCollection<VPCWrapper> VpcList => _vpcList;


        DBSubnetGroupWrapper _selectedDBSubnetGroupWrapper;
        public DBSubnetGroupWrapper SelectedDbSubnetGroup
        {
            get
            {
                return this._selectedDBSubnetGroupWrapper;
            }
            set
            {
                this._selectedDBSubnetGroupWrapper = value;
                OnPropertyChanged("SelectedDbSubnetGroup");
            }
        }

        public ObservableCollection<DBSubnetGroupWrapper> DbSubnetGroupList => _dbSubnetGroupList;

        public bool CreateNewDBSubnetGroup
        {
            get
            {
                var selectedGroup = SelectedDbSubnetGroup;
                if (selectedGroup == null)
                    return false;

                return selectedGroup.DBSubnetGroupIdentifier.Equals(CreateNewDbSubnetGroupText, StringComparison.Ordinal)
                       || selectedGroup.DBSubnetGroupIdentifier.Equals(DefaultDbSubnetGroupText, StringComparison.Ordinal);
            }
        }

        public List<SecurityGroupInfo> SelectedSecurityGroups => (from g in _securityGroups where g.IsSelected select g.InnerObject).ToList();

        public bool PubliclyAccessible { get; set; }
        public EC2AvailabilityZone SelectedAvailabilityZone { get; set; }
        public ObservableCollection<EC2AvailabilityZone> AvailabilityZoneList => _availabilityZoneList;

        public bool NoPreferenceAvailabilityZone
        {
            get
            {
                if (SelectedAvailabilityZone == null)
                    return false;

                return SelectedAvailabilityZone.ZoneName.Equals(NoPreferenceAvailablityZoneText, StringComparison.Ordinal);
            }
        }

        public bool CreateNewSecurityGroup => _ctlCreateNewSecurityGroup.IsChecked.GetValueOrDefault();
        public ObservableCollection<SelectableGroup<SecurityGroupInfo>> SecurityGroups => _securityGroups;
        public bool AddCidrToSecurityGroups { get; set; }

        public string AddCurrentCidrEstimate => string.Format(AddCurrentCidrTextFormat, _cidrEstimate);

        public string NewSecurityGroupTooltip =>
            string.Format("A security group allowing your current IP address ({0}) to connect to your instance will be created. This will make it easier for you to connect to the instance and configure it.",
                IPAddressUtil.DetermineIPFromExternalSource());

        #endregion

        #region Database Options
        public string DatabaseName { get; set; }
        public string DatabasePort { get; set; }
        public string DbParameterGroup { get; set; }
        public ObservableCollection<DBParameterGroup> DbParameterGroupList => _dbParameterGroupList;
        public string OptionGroup { get; set; }
        public ObservableCollection<string> OptionGroupList => _optionGroupList;
        public bool EnableEncryption { get; set; }

        #endregion

        public bool SecurityGroupSelectionPermitted => !_ctlCreateNewSecurityGroup.IsChecked.GetValueOrDefault();

        internal void SetAvailableVpcsAndDbSubnetGroups(IEnumerable<Vpc> vpcs, IEnumerable<DBSubnetGroup> dbSubnetGroups, bool isVpcOnly)
        {
            // process the subnet groups first, so we can build the set of eligible VPCs (those
            // that have db subnet groups defined)
            _allDBSubnetGroups.Clear();
            _vpcsWithDBSubnets.Clear();
            if (dbSubnetGroups != null)
            {
                foreach (var dbSubnetGroup in dbSubnetGroups)
                {
                    if (!string.IsNullOrEmpty(dbSubnetGroup.VpcId))
                        _vpcsWithDBSubnets.Add(dbSubnetGroup.VpcId);
                    _allDBSubnetGroups.Add(new DBSubnetGroupWrapper(dbSubnetGroup));
                }
            }

            SetVpcList(vpcs, isVpcOnly);

            if (_vpcList.Count != 0)
            {
                // if we're in a vpc only env, follow the console wizard and
                // preselect the default vpc
                VPCWrapper selectedVpc = null;
                if (isVpcOnly)
                {
                    foreach (var v in _vpcList.Where(v => v.NativeVPC.IsDefault))
                    {
                        selectedVpc = v;
                        break;
                    }
                }
                if (selectedVpc == null)
                    selectedVpc = _vpcList[0];

                SelectedVpc = selectedVpc;      
                OnPropertyChanged("SelectedVpc");
            }
        }

        internal void SetVpcList(IEnumerable<Vpc> vpcs, bool isVpcOnly)
        {
            _vpcList.Clear();

            if (!isVpcOnly)
                _vpcList.Add(new VPCWrapper(new Vpc { VpcId = VPCWrapper.NotInVpcPseudoId }));

            _vpcList.Add(new VPCWrapper(new Vpc { VpcId = VPCWrapper.CreateNewVpcPseudoId }));

            if (vpcs != null)
            {
                foreach (var vpc in vpcs)
                {
                    // if we're in default vpc land, allow the default vpc into the list even
                    // if it doesn't have an associated subnet group - we'll later select 
                    // 'default' into the subnet group control
                    if (_vpcsWithDBSubnets.Contains(vpc.VpcId) || (vpc.IsDefault && isVpcOnly))
                        _vpcList.Add(new VPCWrapper(vpc));
                }
            }

            OnPropertyChanged("VpcList");
        }

        internal void SetDbSubnetGroupList(IEnumerable<DBSubnetGroup> dbSubnetGroups)
        {
            _dbSubnetGroupList.Clear();

            if (dbSubnetGroups != null)
            {
                foreach (var dbSubnetGroup in dbSubnetGroups)
                {
                    _dbSubnetGroupList.Add(new DBSubnetGroupWrapper(dbSubnetGroup));
                }
            }

            OnPropertyChanged("DbSubnetGroupList");

            SelectedDbSubnetGroup = _dbSubnetGroupList.Count == 1 ? 
                                        _dbSubnetGroupList[0] : 
                                        _dbSubnetGroupList.FirstOrDefault(x => string.Equals(x.Name, DefaultDbSubnetGroupText, StringComparison.InvariantCultureIgnoreCase));

            OnPropertyChanged("SelectedDbSubnetGroup");
        }

        internal void SetAvailabilityZoneList(IEnumerable<EC2AvailabilityZone> zones)
        {
            _availabilityZoneList.Clear();
            _availabilityZoneList.Add(new EC2AvailabilityZone { ZoneName = NoPreferenceAvailablityZoneText });

            if (zones != null)
            {
                foreach (var zone in zones)
                {
                    _availabilityZoneList.Add(zone);
                }
            }

            OnPropertyChanged("AvailabilityZoneList");

            SelectedAvailabilityZone = _availabilityZoneList[0];
            OnPropertyChanged("SelectedAvailabilityZone");
        }

        internal void ClearSecurityGroups()
        {
            _securityGroups.Clear();
            OnPropertyChanged("SecurityGroups");
        }

        internal void SetSecurityGroups(IEnumerable<DBSecurityGroup> securityGroups)
        {
            _securityGroups.Clear();
            if (securityGroups != null)
            {
                foreach (var sg in securityGroups)
                {
                    var selectableGroup = new SecurityGroupInfo
                    {
                        Name = sg.DBSecurityGroupName,
                        Description = sg.DBSecurityGroupDescription
                    };
                    _securityGroups.Add(new SelectableGroup<SecurityGroupInfo>(selectableGroup));
                }
            }

            OnPropertyChanged("SecurityGroups");
        }

        internal void SetSecurityGroups(IEnumerable<SecurityGroup> securityGroups)
        {
            _securityGroups.Clear();
            if (securityGroups != null)
            {
                foreach (var sg in securityGroups)
                {
                    var selectableGroup = new SecurityGroupInfo
                    {
                        Id = sg.GroupId,
                        Description = sg.Description
                    };
                    _securityGroups.Add(new SelectableGroup<SecurityGroupInfo>(selectableGroup));
                }
            }

            OnPropertyChanged("SecurityGroups");
        }

        internal void SetDbParameterGroupList(IEnumerable<DBParameterGroup> parameterGroups)
        {
            _dbParameterGroupList.Clear();
            if (parameterGroups != null)
            {
                foreach (var pg in parameterGroups)
                {
                    _dbParameterGroupList.Add(pg);
                }
            }

            OnPropertyChanged("DBParameterGroupList");
        }

        internal void SetOptionGroupList(IEnumerable<string> optionGroups)
        {
            _optionGroupList.Clear();
            if (optionGroups != null)
            {
                foreach (var og in optionGroups)
                {
                    _optionGroupList.Add(og);
                }
            }

            OnPropertyChanged("OptionGroupList");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void _ctlVpcList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsInitialized || e.AddedItems.Count == 0)
                return;

            var selectedVpc = e.AddedItems[0] as VPCWrapper;
            if (selectedVpc == null)
                return;

            switch (selectedVpc.VpcId)
            {
                case VPCWrapper.NotInVpcPseudoId:
                    {
                        _ctlCreateNewSecurityGroup.IsEnabled = true;
                        _ctlCreateNewSecurityGroup.IsChecked = false;
                    }
                    break;       // subnet fields will auto-hide

                case VPCWrapper.CreateNewVpcPseudoId:
                    {
                        var subnetGroupsForVpc = new List<DBSubnetGroup>
                        {
                            new DBSubnetGroup { DBSubnetGroupName = LaunchDBInstanceAdvancedSettingsPage.CreateNewDbSubnetGroupText }
                        };
                        SetDbSubnetGroupList(subnetGroupsForVpc);
                        _ctlCreateNewSecurityGroup.IsChecked = true;
                        _ctlCreateNewSecurityGroup.IsEnabled = false;
                    }
                    break;

                default:
                    {
                        var subnetGroupsForVpc = _allDBSubnetGroups
                                .Where(subnetGroup => subnetGroup.NativeSubnetGroup.VpcId.Equals(selectedVpc.VpcId, StringComparison.OrdinalIgnoreCase))
                                .Select(subnetGroup => subnetGroup.NativeSubnetGroup)
                                .ToList();
                        if (subnetGroupsForVpc.Count == 0)
                        {
                            subnetGroupsForVpc.Add(new DBSubnetGroup { DBSubnetGroupName = LaunchDBInstanceAdvancedSettingsPage.DefaultDbSubnetGroupText });
                        }
                        SetDbSubnetGroupList(subnetGroupsForVpc);
                        _ctlCreateNewSecurityGroup.IsEnabled = true;
                        _ctlCreateNewSecurityGroup.IsChecked = false;
                    }
                    break;
            }

            ToggleGroupsAndCidrOptions();

            OnPropertyChanged("SelectedVpc");
            OnPropertyChanged("LaunchingIntoVpc");
        }

        private void _ctlCreateNewSecurityGroup_OnClick(object sender, RoutedEventArgs e)
        {
            ToggleGroupsAndCidrOptions();
        }

        private void ToggleGroupsAndCidrOptions()
        {
            var createNewGroup = _ctlCreateNewSecurityGroup.IsChecked.GetValueOrDefault();
            _ctlSecurityGroupsList.IsEnabled = !createNewGroup;
            _ctlAddCurrentCidr.IsEnabled = !createNewGroup;
            _ctlAddCurrentCidr.IsChecked = createNewGroup;
        }
    }

    // Used to handle multi-select of security groups in an items collection control
    // and to deal with different RDS/EC2-VPC security group types. All we care about
    // is the id/name
    public class SecurityGroupInfo
    {
        public string Name { get; set; }

        // only set group id for VPC mode
        public string Id { get; set; }

        public string Description { get; set; }

        public string DisplayName
        {
            get
            {
                var sb = new StringBuilder();
                if (string.IsNullOrEmpty(Id))
                    sb.Append(Name);
                else
                    sb.AppendFormat("[VPC] {0}", Id);

                if (!string.IsNullOrEmpty(Description))
                    sb.AppendFormat(" {0}", Description);

                return sb.ToString();
            }
        }
    }

    // Used to handle multi-select of security groups in a combo box
    public class SelectableGroup<T>
    {
        public bool IsSelected { get; set; }
        public T InnerObject { get; set; }

        public SelectableGroup(T innerObject)
            : this(innerObject, false)
        {
        }

        public SelectableGroup(T innerObject, bool isSelected)
        {
            this.InnerObject = innerObject;
            this.IsSelected = isSelected;
        }
    }
}
