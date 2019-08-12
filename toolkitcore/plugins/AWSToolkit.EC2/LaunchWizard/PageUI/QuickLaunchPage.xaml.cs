using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

using Amazon.AWSToolkit.EC2.View.Components;
using Amazon.AWSToolkit.EC2.Model;

using Amazon.EC2.Model;
using Amazon.IdentityManagement.Model;

namespace Amazon.AWSToolkit.EC2.LaunchWizard.PageUI
{
    /// <summary>
    /// Interaction logic for QuickLaunchPage.xaml
    /// </summary>
    public partial class QuickLaunchPage : INotifyPropertyChanged
    {
        public static readonly string uiProperty_Ami = "ami";
        public static readonly string uiProperty_KeyPair = "keypair";
        public static readonly string uiProperty_SecurityGroup = "securitygroup";
        public static readonly string uiProperty_VpcSubnet = "vpcSubnet";
        public static readonly string uiProperty_InstanceType = "instancetype";
        public static readonly string uiProperty_InstanceName = "instancename";

        private readonly List<EBSVolumeType> _volumeTypes = new List<EBSVolumeType>();

        public QuickLaunchPage()
        {
            AvailableSecurityGroups = new ObservableCollection<SecurityGroupWrapper>();
            AvailableVpcSubnets = new ObservableCollection<VpcAndSubnetWrapper>();
            IAMInstanceProfiles = new ObservableCollection<InstanceProfile>();
            InstanceTypes = new ObservableCollection<InstanceType>();

            DataContext = this;

            // to create a piops volume, user needs to employ advanced mode of the wizard
            // as we're short on space to add the field on quick launch
            foreach (var vol in EBSVolumeTypes.AllVolumeTypes.Where(vol => !vol.IsPiopsVolume))
            {
                _volumeTypes.Add(vol);
            }

            InitializeComponent();

            // switch on grouping for the instance type and vpc subnet dropdowns
            var instanceTypesView = (CollectionView)CollectionViewSource.GetDefaultView(_instanceTypeSelector.ItemsSource);
            var familyGroupDescription = new PropertyGroupDescription("HardwareFamily");
            instanceTypesView.GroupDescriptions.Add(familyGroupDescription);

            var vpcSubnetsView = (CollectionView)CollectionViewSource.GetDefaultView(_vpcSubnets.ItemsSource);
            var vpcGroupDescription = new PropertyGroupDescription("VpcGroupingHeader");
            vpcSubnetsView.GroupDescriptions.Add(vpcGroupDescription);

            _amiSelector.PropertyChanged += _amiSelector_AMISelectionChanged;

            // seems natural to default to our own platform!
            _amiSelector.PlatformFilter = QuickLaunchAMIListControl.PlatformType.Windows;
            SelectedVolumeType = _volumeTypes[0];
            NotifyPropertyChanged("SelectedVolumeType");
        }

        public QuickLaunchPage(IAWSWizardPageController controller)
            : this()
        {
            PageController = controller;
        }

        public IAWSWizardPageController PageController { get; set; }

        public void SetAvailableKeyPairs(ICollection<string> existingKeyPairs, ICollection<string> keyPairsStoredInToolkit, string autoSelectPair)
        {
            _keyPairs.SetExistingKeyPairs(existingKeyPairs, keyPairsStoredInToolkit, autoSelectPair);
            _keyPairs.Cursor = Cursors.Arrow;
        }

        public ObservableCollection<SecurityGroupWrapper> AvailableSecurityGroups { get; }

        public void SetAvailableSecurityGroups(ICollection<SecurityGroup> existingGroups, string autoSelectGroup)
        {
            AvailableSecurityGroups.Clear();
            foreach (var sg in existingGroups)
            {
                AvailableSecurityGroups.Add(new SecurityGroupWrapper(sg));
            }

            if (string.IsNullOrEmpty(autoSelectGroup))
            {
                if (existingGroups.Count == 1)
                    _securityGroups.SelectedIndex = 0;
                else
                {
                    // preselect 'default' group so at least something is selected
                    var defaultGroup 
                        = AvailableSecurityGroups.FirstOrDefault((wrapper) => wrapper.DisplayName.Equals("default", StringComparison.OrdinalIgnoreCase));
                    if (defaultGroup != null)
                        _securityGroups.SelectedItem = defaultGroup;
                }
            }
            else
            {
                var preselectedGroup = AvailableSecurityGroups.FirstOrDefault((wrapper) => string.Compare(wrapper.DisplayName, autoSelectGroup, true) == 0);
                if (preselectedGroup != null)
                    _securityGroups.SelectedItem = preselectedGroup;
            }

            _securityGroups.Cursor = Cursors.Arrow;

            NotifyPropertyChanged("HasSelectedAMI");
            NotifyPropertyChanged("HasSecurityGroupsAndAMI");
        }

        public ObservableCollection<VpcAndSubnetWrapper> AvailableVpcSubnets { get; }

        public void SetAvailableVpcSubnets(ICollection<Vpc> vpcs, ICollection<Subnet> subnets, bool isVpcOnlyEnvironment)
        {
            AvailableVpcSubnets.Clear();

            if (vpcs != null)
            {
                if (vpcs.Any())
                {
                    VpcAndSubnetWrapper defaultSelection = null;

                    // add a 'no vpc usage' default so that the user can change their mind
                    // if they initially select a subnet 
                    if (!isVpcOnlyEnvironment)
                    {
                        defaultSelection = new VpcAndSubnetWrapper(VPCWrapper.NotInVpcPseudoId, SubnetWrapper.NoVpcSubnetPseudoId);
                        AvailableVpcSubnets.Add(defaultSelection);
                    }

                    foreach (var v in vpcs)
                    {
                        if (isVpcOnlyEnvironment && v.IsDefault)
                        {
                            // inject a 'No Preference' subnet option and preselect it, to match what the
                            // console does. This selection will not specify a subnet id on the resulting
                            // RunInstances command and allow the service to pick the appropriate subnet.
                            defaultSelection = new VpcAndSubnetWrapper(v, SubnetWrapper.NoPreferenceSubnetPseudoId);
                            AvailableVpcSubnets.Add(defaultSelection);
                        }

                        var sortedSubnets = new SortedList<string, List<Subnet>>();
                        foreach (var s in subnets)
                        {
                            if (s.VpcId == v.VpcId)
                            {
                                // it's possible to have multiple subnets in same availability zone, if the user
                                // has set up public/private nets
                                var zone = s.AvailabilityZone;
                                List<Subnet> zoneSubnets;
                                if (sortedSubnets.ContainsKey(zone))
                                    zoneSubnets = sortedSubnets[zone];
                                else
                                {
                                    zoneSubnets = new List<Subnet>();
                                    sortedSubnets.Add(zone, zoneSubnets);
                                }

                                zoneSubnets.Add(s);
                            }
                        }

                        foreach (var k in sortedSubnets.Keys)
                        {
                            var zoneSubnets = sortedSubnets[k];
                            foreach (var s in zoneSubnets)
                            {
                                AvailableVpcSubnets.Add(new VpcAndSubnetWrapper(v, s));
                            }
                        }
                    }

                    if (defaultSelection != null)
                        _vpcSubnets.SelectedItem = defaultSelection;

                    _vpcSubnets.IsEnabled = true;
                }
                else
                {
                    _vpcSubnets.IsEnabled = false;
                }
            }

            _vpcSubnets.Cursor = Cursors.Arrow;
        }

        public SubnetWrapper SelectedSubnet
        {
            get
            {
                if (_vpcSubnets.SelectedItem == null)
                    return null;

                return (_vpcSubnets.SelectedItem as VpcAndSubnetWrapper).Subnet;
            }
        }

        public IEnumerable<EC2QuickLaunchImage> Images
        {
            set
            {
                _amiSelector.Images = value;
                _amiSelector.Cursor = Cursors.Arrow;
                foreach (var image in value)
                {
                    image.PropertyChanged += image_PropertyChanged;
                }
            }
        }

        public string InstanceName { get; set; }

        public IEnumerable<EBSVolumeType> VolumeTypes => _volumeTypes;

        public int VolumeSize { get; set; }

        public EBSVolumeType SelectedVolumeType { get; set; }

        void image_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == EC2QuickLaunchImage.uiProperty_Bitness)
            {
                var image = sender as EC2QuickLaunchImage;
                if (image == null)
                    return;

                if (_amiSelector.SelectedAMI == image)
                    NotifyPropertyChanged(uiProperty_Ami);
                else
                    _amiSelector.SelectedAMI = image; // This is to handle the case that changing the inner radio button doesn't select the row which we want.
            }
        }

        public string SelectedAMIID => _amiSelector.SelectedAMIID;

        public EC2QuickLaunchImage SelectedAMI
        {
            get => _amiSelector.SelectedAMI;
            set => _amiSelector.SelectedAMI = value;
        }

        public bool HasSelectedAMI => _amiSelector != null && SelectedAMI != null;

        public bool HasSecurityGroupsAndAMI => HasSelectedAMI && _securityGroups.HasItems;

        public ObservableCollection<InstanceType> InstanceTypes { get; }

        public void SetInstanceTypes(IEnumerable<InstanceType> instanceTypes)
        {
            InstanceTypes.Clear();
            if (instanceTypes != null)
            {
                foreach (var i in instanceTypes)
                {
                    InstanceTypes.Add(i);
                }

                if (InstanceTypes.Count() != 0)
                    _instanceTypeSelector.SelectedItem = InstanceTypes[0];
            }
        }

        public InstanceType SelectedInstanceType => _instanceTypeSelector.SelectedItem as InstanceType;

        public string SelectedKeyPairName => _keyPairs.SelectedKeyPairName;

        public bool IsExistingKeyPairNameSelected => _keyPairs.IsExistingKeyPairSelected;

        public SecurityGroupWrapper SelectedSecurityGroup => _securityGroups.SelectedItem as SecurityGroupWrapper;

        public bool InstanceNameIsValid
        {
            get
            {
                if (string.IsNullOrEmpty(_instanceName.Text))
                    return true;

                return !_instanceName.Text.StartsWith("aws:", StringComparison.InvariantCultureIgnoreCase);
            }
        }

        public bool AllowFiltering
        {
            get => _amiSelector.AllowFiltering;
            set => _amiSelector.AllowFiltering = value;
        }

        public bool AllowCreateKeyPairSelection
        {
            set => _keyPairs.AllowCreateKeyPairSelection = value;
        }

        public ObservableCollection<InstanceProfile> IAMInstanceProfiles { get; }

        public void SetIAMInstanceProfiles(ICollection<InstanceProfile> profiles)
        {
            IAMInstanceProfiles.Clear();

            IAMInstanceProfiles.Add(new InstanceProfile
            {
                InstanceProfileName = "None",
                Arn = string.Empty
            });
            foreach (var p in profiles)
            {
                IAMInstanceProfiles.Add(p);
            }

            _iamProfile.SelectedIndex = 0;
            _iamProfile.Cursor = Cursors.Arrow;
        }

        public InstanceProfile SelectedInstanceProfile
        {
            get
            {
                if (_iamProfile.SelectedIndex == 0)
                    return null;
                return _iamProfile.SelectedItem != null ? _iamProfile.SelectedItem as InstanceProfile : null;
            }
        }

        public void SetVolumeSizeForSelectedAMI()
        {
            if (SelectedAMI == null)
                return;

            // if we got an image size recorded in the quicklaunch backing file, use it
            // otherwise fall back to using a hard-coded default for the volume type
            // on the selected platform
            int minSize = SelectedAMI.TotalImageSize;
            if (minSize <= 0)
                minSize = SelectedVolumeType.MinimumSizeForPlatform(SelectedAMI.Platform);

            VolumeSize = minSize;
            NotifyPropertyChanged("VolumeSize");
        }

        private void _securityGroups_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged(uiProperty_SecurityGroup);
        }
        private void _vpcSubnets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged(uiProperty_VpcSubnet);
        }

        void _amiSelector_AMISelectionChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged(uiProperty_Ami);

            // these fire to enable/disable all controls in the options panel
            NotifyPropertyChanged("HasSelectedAMI");
            NotifyPropertyChanged("HasSecurityGroupsAndAMI");
        }

        private void _instanceTypeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged(uiProperty_InstanceType);
        }

        private void _instanceName_TextChanged(object sender, TextChangedEventArgs e)
        {
            //_instanceNameValidationFailIcon.Visibility = InstanceNameIsValid ? Visibility.Collapsed : Visibility.Visible;
            //NotifyPropertyChanged(uiProperty_InstanceName);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _launchOptionsPanel.MaxWidth = _launchOptionsPanel.ActualWidth;
        }

        private void VolumeType_OnSelected(object sender, RoutedEventArgs e)
        {
            SetVolumeSizeForSelectedAMI();
        }
    }

}
