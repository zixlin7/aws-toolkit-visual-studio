using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Amazon.AWSToolkit.EC2.Model;
using Microsoft.Win32;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

using Amazon.EC2.Model;
using Amazon.IdentityManagement.Model;
using System.Collections.ObjectModel;

namespace Amazon.AWSToolkit.EC2.LaunchWizard.PageUI
{
    /// <summary>
    /// Interaction logic for AMIOptionsPage.xaml
    /// </summary>
    public partial class AMIOptionsPage
    {
        const string USE_DEFAULT_LABEL = "Use default";

        public AMIOptionsPage()
        {
            DataContext = this;
            InstanceTypes = new ObservableCollection<InstanceType>();
            VpcSubnets = new ObservableCollection<VpcAndSubnetWrapper>();
            InstanceCount = 1;

            InitializeComponent();

            // switch on grouping for the instance type and vpc subnet dropdowns
            var instanceTypesView = (CollectionView)CollectionViewSource.GetDefaultView(_instanceTypeSelector.ItemsSource);
            var familyGroupDescription = new PropertyGroupDescription("HardwareFamily");
            instanceTypesView.GroupDescriptions.Add(familyGroupDescription);

            var vpcSubnetsView = (CollectionView)CollectionViewSource.GetDefaultView(_vpcSubnets.ItemsSource);
            var vpcGroupDescription = new PropertyGroupDescription("VpcGroupingHeader");
            vpcSubnetsView.GroupDescriptions.Add(vpcGroupDescription);
        }

        public AMIOptionsPage(IAWSWizardPageController controller)
            : this()
        {
            this.PageController = controller;
        }

        public IAWSWizardPageController PageController { get; set; }

        public bool HasValidationErrors
        {
            get
            {
                bool hasErrors = Validation.GetHasError(_instanceCount);
                if (!hasErrors && UserDataIsFile)
                    hasErrors = Validation.GetHasError(_userDataFile);

                return hasErrors;
            }
        }

        public string SelectedInstanceTypeID
        {
            get
            {
                InstanceType selectedType = SelectedInstanceType;
                if (selectedType != null)
                    return selectedType.Id;
                else
                    return string.Empty;
            }
        }

        public ObservableCollection<InstanceType> InstanceTypes { get; private set; }

        public InstanceType SelectedInstanceType
        {
            get
            {
                if (IsInitialized)
                {
                    var selectedType = _instanceTypeSelector.SelectedItem;
                    if (selectedType != null)
                        return selectedType as InstanceType;
                }
                
                return null;
            }
        }

        public void SetInstanceTypes(IEnumerable<InstanceType> instanceTypes)
        {
            InstanceTypes.Clear();
            if (instanceTypes != null)
            {
                foreach (var i in instanceTypes)
                {
                    InstanceTypes.Add(i);
                }

                this._instanceTypeSelector.SelectedItem = InstanceTypes[0];
                this._instanceTypeSelector.IsEnabled = true;
            }
            else
                this._instanceTypeSelector.IsEnabled = false;
        }

        public int InstanceCount { get; set; }

        public IEnumerable<AvailabilityZone> Zones
        {
            set
            {
                var zones = new List<string> {USE_DEFAULT_LABEL};
                zones.AddRange(value.Select(zone => zone.ZoneName));
                _availabilityZone.ItemsSource = zones;
                _availabilityZone.SelectedIndex = 0;

                _availabilityZone.Cursor = Cursors.Arrow;
            }
        }

        public ObservableCollection<VpcAndSubnetWrapper> VpcSubnets { get; private set; }

        public void SetVpcSubnets(ICollection<Vpc> vpcs, ICollection<Subnet> subnets, bool isVpcOnlyEnvironment)
        {
            VpcSubnets.Clear();

            VpcAndSubnetWrapper defaultSelection = null;
            if (vpcs != null)
            {
                // because this page has a button to enable launch-into-vpc, we don't need a 'no vpc' option
                // in the subnets list
                foreach (var v in vpcs)
                {
                    if (isVpcOnlyEnvironment && v.IsDefault)
                    {
                        // inject a 'No Preference' subnet option and preselect it, to match what the
                        // console does. This selection will not specify a subnet id on the resulting
                        // RunInstances command and allow the service to pick the appropriate subnet.
                        defaultSelection = new VpcAndSubnetWrapper(v, SubnetWrapper.NoPreferenceSubnetPseudoId);
                        VpcSubnets.Add(defaultSelection);
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
                            VpcSubnets.Add(new VpcAndSubnetWrapper(v, s));
                        }
                    }
                }
            }

            this._ctlLaunchVPC.IsEnabled = VpcSubnets.Count > 0;
            if (defaultSelection != null)
                _vpcSubnets.SelectedItem = defaultSelection;

            this._ctlLaunchEC2.IsChecked = !isVpcOnlyEnvironment;
            this._ctlLaunchVPC.IsChecked = isVpcOnlyEnvironment;

            _vpcSubnets.Cursor = Cursors.Arrow;
        }

        public string SelectedZone
        {
            get 
            {
                if (_availabilityZone.SelectedIndex == 0) // "Use default"
                    return string.Empty;

                return _availabilityZone.SelectedItem != null ? _availabilityZone.SelectedItem as string : string.Empty; 
            }
        }

        public Model.SubnetWrapper SelectedSubnet
        {
            get
            {
                if (_vpcSubnets.SelectedItem == null)
                    return null;

                return (_vpcSubnets.SelectedItem as VpcAndSubnetWrapper).Subnet;
            }
        }

        public bool LaunchIntoVPC
        {
            get
            {
                return this._ctlLaunchVPC.IsChecked.GetValueOrDefault();
            }
        }

        public ICollection<InstanceProfile> IamProfiles
        {
            set
            {
                List<InstanceProfile> iamProfiles = new List<InstanceProfile>();
                iamProfiles.Add(new InstanceProfile
                {
                    InstanceProfileName = "None",
                    Arn = string.Empty
                });
                foreach (var profile in value)
                {
                    iamProfiles.Add(profile);
                }

                _iamProfile.ItemsSource = iamProfiles;
                _iamProfile.SelectedIndex = 0;
                _iamProfile.Cursor = Cursors.Arrow;
            }
        }

        public InstanceProfile SelectedIamProfile
        {
            get
            {
                if (_iamProfile.SelectedIndex == 0)
                    return null;
                return _iamProfile.SelectedItem != null ? _iamProfile.SelectedItem as InstanceProfile : null;
            }
        }

        public ICollection<string> KernelIDs
        {
            set
            {
                List<string> kernelIDs = new List<string>();
                kernelIDs.Add(USE_DEFAULT_LABEL);
                kernelIDs.AddRange(value);

                _kernelID.ItemsSource = kernelIDs;
                _kernelID.SelectedIndex = 0;

                _kernelID.Cursor = Cursors.Arrow;
            }
        }

        public string SelectedKernelID
        {
            get 
            {
                if (_kernelID.SelectedIndex == 0) // "Use default"
                    return string.Empty;

                return _kernelID.SelectedItem != null ? _kernelID.SelectedItem as string : string.Empty; 
            }
        }

        public string SelectedRamDiskID
        {
            get 
            {
                if (_ramdiskID.SelectedIndex == 0) // "Use default"
                    return string.Empty;

                return _ramdiskID.SelectedItem != null ? _ramdiskID.SelectedItem as string : string.Empty; 
            }
        }

        public ICollection<string> RamDiskIDs
        {
            set
            {
                List<string> ramDiskIDs = new List<string>();
                ramDiskIDs.Add(USE_DEFAULT_LABEL);
                ramDiskIDs.AddRange(value);

                _ramdiskID.ItemsSource = ramDiskIDs;
                _ramdiskID.SelectedIndex = 0;

                _ramdiskID.Cursor = Cursors.Arrow;
            }
        }

        public bool EnableMonitoring
        {
            get { return _monitoring.IsChecked == true; }
        }

        public bool PreventTermination
        {
            get { return _terminationProtection.IsChecked == true; }
        }

        public string UserData
        {
            get 
            {
                if (UserDataIsFile)
                    return UserDataFilename;
                else
                    return _userDataText.Text;
            }
        }

        public string UserDataFilename { get; set; }

        public bool UserDataIsFile
        {
            get { return _userDataAsFile.IsChecked == true; }
        }

        public bool UserDataEncoded
        {
            get { return _base64.IsChecked == true; }
        }

        public string ShutdownBehavior
        {
            get { return (_shutdownBehaviour.SelectedItem as ComboBoxItem).Tag as string; }
        }

        private void _userDataAsText_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized)
                return;

            _userDataAsFileFields.Visibility = Visibility.Collapsed;
            _userDataText.Visibility = Visibility.Visible;
        }

        private void _userDataAsFile_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized)
                return;

            _userDataText.Visibility = Visibility.Collapsed;
            _userDataAsFileFields.Visibility = Visibility.Visible;
        }

        private void _userDataFileBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.CheckPathExists = true;
            dlg.CheckFileExists = true;
            dlg.Filter = "All files (*.*)|*.*";
            if (dlg.ShowDialog() == true)
                _userDataFile.Text = dlg.FileName;
        }

        private void OnValidationError(object sender, ValidationErrorEventArgs e)
        {
            PageController.TestForwardTransitionEnablement();
        }

    }

    class InstanceCountValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            int count = -1;
            if (!Int32.TryParse(value.ToString(), out count) || count <= 0)
                return new ValidationResult(false, "Instance count must be numeric and greater than zero.");

            return new ValidationResult(true, null);
        }
    }

    class UserDataFileValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            // disabled until I figure out how to turn this off when 'as text' selected; currently the
            // error adorner does not go away and sits atop the 'as text' field
            //string filename = value.ToString();
            //if (!System.IO.File.Exists(filename))
            //    return new ValidationResult(false, "File must exist.");

            return new ValidationResult(true, null);
        }
    }
}
