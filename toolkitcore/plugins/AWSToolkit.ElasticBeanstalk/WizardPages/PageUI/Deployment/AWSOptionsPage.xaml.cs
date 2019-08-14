using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.EC2;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.LegacyDeployment;
using AWSDeployment;
using log4net;
using System.Windows.Data;
using Amazon.ElasticLoadBalancingV2;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment
{
    /// <summary>
    /// Interaction logic for AWSOptionsPage.xaml
    /// </summary>
    public partial class AWSOptionsPage : INotifyPropertyChanged
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(AWSOptionsPage));
        TextBlock _rdsGroupsListComboSelectedDisplay;

        public AWSOptionsPage()
        {
            DataContext = this;

            SingleInstanceEnvironment = true;
            InstanceTypes = new ObservableCollection<InstanceType>();
            SecurityGroups = new ObservableCollection<SelectableGroup<SecurityGroupInfo>>();

            InitializeComponent();

            // switch on grouping for the instance type dropdown
            var instanceTypesView = (CollectionView)CollectionViewSource.GetDefaultView(_instanceTypesSelector.ItemsSource);
            var familyGroupDescription = new PropertyGroupDescription("HardwareFamily");
            instanceTypesView.GroupDescriptions.Add(familyGroupDescription);

            this._keyPairSelector.OnKeyPairSelectionChanged += KeyPairSelectionChanged;
        }

        public AWSOptionsPage(IAWSWizardPageController controller)
            : this()
        {
            PageController = controller;
        }

        public IAWSWizardPageController PageController { get; set; }

        public string SolutionStack
        {
            get
            {
                if (_solutionStack.SelectedItem != null)
                    return _solutionStack.SelectedItem as string;
                return string.Empty;
            }
        }

        public string CustomAMIID { get; set; }

        public string SelectedInstanceTypeID
        {
            get
            {
                var instanceType = this._instanceTypesSelector.SelectedItem as InstanceType;
                return instanceType != null ? instanceType.Id : string.Empty;
            }
        }

        public InstanceType SelectedInstanceType => this._instanceTypesSelector.SelectedItem as InstanceType;

        public ObservableCollection<InstanceType> InstanceTypes { get; }

        public bool SingleInstanceEnvironment { get; set; }

        public bool EnableRollingDeployments { get; set; }

        public bool UseNonDefaultVpc => _useNonDefaultVpc.IsChecked.GetValueOrDefault();

        public void QueryKeyPairSelection(out string keypairName, out bool createNew)
        {
            keypairName = _keyPairSelector.SelectedKeyPairName;
            createNew = !_keyPairSelector.IsExistingKeyPairSelected;
        }

        public bool HasKeyPairSelection => !string.IsNullOrEmpty(_keyPairSelector.SelectedKeyPairName);

        public void SetSolutionStacks(IEnumerable<string> stacks, string autoSelectStack)
        {
            if (stacks != null)
            {
                _solutionStack.ItemsSource = stacks;
                if (!string.IsNullOrEmpty(autoSelectStack) && stacks.Contains(autoSelectStack))
                    _solutionStack.SelectedItem = autoSelectStack;
                else
                    _solutionStack.SelectedIndex = 0;
            }
        }


        public void SetInstanceTypes(IEnumerable<string> instanceTypes)
        {
            InstanceTypes.Clear();

            if (instanceTypes == null)
            {
                _instanceTypesSelector.Cursor = Cursors.Wait;
                return;
            }

            // getting service meta and using instance to lookup avoids empty-instance bloat that 
            // can occur if we instead loop on static FindById() method on InstanceType when EC2ServiceMeta has 
            // failed to load content (each call then generates an empty service meta instance)
            var serviceMeta = EC2ServiceMeta.Instance;
            foreach (var sizeId in instanceTypes)
            {
                var type = serviceMeta.ById(sizeId);
                if (type != null)
                    InstanceTypes.Add(type);
                else
                    LOGGER.ErrorFormat("Unable to find instance type {0} in available service metadata", sizeId);
            }

            if (InstanceTypes.Count > 0)
            {
                _instanceTypesSelector.SelectedIndex = 0;
                MakeSureSelectedInstanceTypeMeetsVPC();
            }
            _instanceTypesSelector.Cursor = Cursors.Arrow;
        }

        public void SetExistingKeyPairs(ICollection<string> existingKeyPairs, ICollection<string> keyPairsStoredInToolkit, string autoSelectPair)
        {
            _keyPairSelector.SetExistingKeyPairs(existingKeyPairs, keyPairsStoredInToolkit, autoSelectPair);
        }

        /// <summary>
        /// Set true if the user and/or region is 'vpc by default'
        /// </summary>
        internal bool VpcOnlyMode { get; set; }

        internal bool HasDefaultVpc => !string.IsNullOrEmpty(DefaultVpcId);

        internal string DefaultVpcId { get; private set; }

        /// <summary>
        /// Set to the total number of vpcs available to the user in the selected region. This
        /// governs whether we allow access to the subsequent vpc page in the wizard via the
        /// 'use non-default vpc' button
        /// </summary>
        internal int AvailableVpcCount { get; private set; }

        public void SetVpcAvailability(string defaultVpcId, int availableVpcCount)
        {
            DefaultVpcId = defaultVpcId;
            AvailableVpcCount = availableVpcCount;

            this.Dispatcher.BeginInvoke((Action)(() =>
            {
                MakeSureSelectedInstanceTypeMeetsVPC();
                SetVPCState();
            }));
        }

        private void MakeSureSelectedInstanceTypeMeetsVPC()
        {
            if (!this.HasDefaultVpc)
            {
                var selected = _instanceTypesSelector.SelectedItem as InstanceType;
                if (selected != null && selected.RequiresVPC)
                {
                    InstanceType t1Micro = null;
                    foreach(InstanceType type in _instanceTypesSelector.Items)
                    {
                        if(string.Equals(type.Id, Amazon.EC2.InstanceType.T1Micro.Value, StringComparison.InvariantCultureIgnoreCase))
                        {
                            t1Micro = type;
                            break;
                        }
                    }

                    // Fallback to t1Micro if the account does not have a default VPC
                    if (t1Micro != null)
                    {
                        this._instanceTypesSelector.SelectedItem = t1Micro;
                    }
                    // Extra fallback just in case t1.micro disappears.
                    else
                    {
                        foreach (InstanceType type in _instanceTypesSelector.Items)
                        {
                            if (!type.RequiresVPC)
                            {
                                this._instanceTypesSelector.SelectedItem = type;
                                break;
                            }
                        }
                    }
                }
            }
        }

        public ObservableCollection<SelectableGroup<SecurityGroupInfo>> SecurityGroups { get; set; }

        internal void SetSecurityGroupsAndInstances(IEnumerable<SelectableGroup<SecurityGroupInfo>> groupsAndInstances)
        {
            SecurityGroups.Clear();
            foreach (var gi in groupsAndInstances)
            {
                SecurityGroups.Add(gi);                
            }

            _rdsGroupsListCombo.IsEnabled = SecurityGroups.Any();
        }

        public List<SecurityGroupInfo> SelectedSecurityGroups => (from @group in this.SecurityGroups where @group.IsSelected select @group.InnerObject).ToList();

        // Extra data used for new Beanstalk wizard and VPC support, where we only want to open specific ports
        // in the RDS VPC security groups. This gives the referencing db instances for a given selected security group.
        // RDS instances in a VPC will contain the necessary port info in the db instance name in the UI.
        public Dictionary<string, List<int>> VPCGroupsAndReferencingDBInstances
        {
            get
            {
                var d = new Dictionary<string, List<int>>();
                foreach (var g in this.SecurityGroups)
                {
                    if (!g.IsSelected || !g.InnerObject.IsVPCGroup)
                        continue;

                    var ports = new HashSet<int>();
                    foreach (var dbi in g.ReferencingDBInstances.Split(','))
                    {
                        var portSuffixStart = dbi.IndexOf("(port", StringComparison.Ordinal);
                        if (portSuffixStart < 0)
                            continue;

                        var portEnd = dbi.Length - 1;
                        var portStart = portSuffixStart + 6;
                        var port = dbi.Substring(portStart, portEnd - portStart);
                        ports.Add(int.Parse(port));
                    }
                    d.Add(g.InnerObject.Id, ports.ToList());
                }

                return d;
            }
        }


        private void _solutionStack_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsInitialized)
                return;

            NotifyPropertyChanged("solutionstack");
            SetVPCState();
        }

        void KeyPairSelectionChanged(object sender, EventArgs e)
        {
            NotifyPropertyChanged("keypair");
        }

        private void _customAMI_LostFocus(object sender, RoutedEventArgs e)
        {
            var lbl = new StringBuilder("Key pair");

            if (string.IsNullOrEmpty(_customAMI.Text))
                lbl.Append(" *");
            lbl.Append(":");

            KeyPairLabel.Content = lbl.ToString();
            NotifyPropertyChanged("customami");
        }

        private void SetVPCState()
        {
            if (this._solutionStack.SelectedItem == null)
                return;

            var isLegacy = BeanstalkDeploymentEngine.IsLegacyContainer(this._solutionStack.SelectedItem as string);
            if (isLegacy)
                _useNonDefaultVpc.IsChecked = false;
            _useNonDefaultVpc.IsEnabled = !isLegacy;
        }

        private void _useNonDefaultVpc_Click(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged("vpc");
        }

        private void _rdsGroupsListCombo_OnDropDownClosed(object sender, EventArgs e)
        {
            var names = new StringBuilder();
            foreach (var group in SecurityGroups)
            {
                if (group.IsSelected)
                {
                    if (names.Length > 0)
                        names.Append(",");
                    names.Append(group.InnerObject.DisplayName);
                }
            }

            _rdsGroupsListComboSelectedDisplay.Text = names.ToString();
        }

        private void _loadBalancerList_OnDropDownClosed(object sender, EventArgs e)
        {
            var selected = _loadBalancerList.SelectedItem as string;

            // If application, we must set VPC, so check that box and make it not editable
            if (LoadBalancerTypeEnum.Application.Value.Equals(selected))
            {
                _useNonDefaultVpc.IsChecked = true;
                _useNonDefaultVpc.IsEnabled = false;
            }
            else
            {
                _useNonDefaultVpc.IsEnabled = true;
            }
        }


        void _loadBalancerListCombo_Loaded(object sender, RoutedEventArgs e)
        {
            _loadBalancerList.ItemsSource = new List<string>{"classic", LoadBalancerTypeEnum.Application, LoadBalancerTypeEnum.Network};
        }

        void _rdsGroupsListCombo_Loaded(object sender, RoutedEventArgs e)
        {
            var contentPresenter = this._rdsGroupsListCombo.Template.FindName("ContentSite", this._rdsGroupsListCombo) as ContentPresenter;
            if (contentPresenter != null)
            {
                this._rdsGroupsListComboSelectedDisplay = contentPresenter.ContentTemplate.FindName("_rdsGroupsListComboSelectedDisplay", contentPresenter) as TextBlock;
            }
        }

        private void _singleInstanceMode_Checked(object sender, RoutedEventArgs e)
        {
            if (this._singleInstanceMode.IsChecked.GetValueOrDefault() && this._rollingDeploymentInstanceMode != null)
            {
                this._rollingDeploymentInstanceMode.IsChecked = false;
            }

            if (_loadBalancerList != null)
            {
                _loadBalancerList.IsEnabled = false;
                _loadBalancerList.SelectedItem = null;
                _useNonDefaultVpc.IsEnabled = true;
            }
        }

        private void _singleInstanceMode_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_loadBalancerList != null)
            {
                _loadBalancerList.IsEnabled = true;
                _loadBalancerList.SelectedItem = "classic";
            }
        }
    }
}
