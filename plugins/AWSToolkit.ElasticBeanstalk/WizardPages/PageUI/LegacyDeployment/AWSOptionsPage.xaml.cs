using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
using System.Text.RegularExpressions;

using AWSDeployment;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.Components;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageWorkers;
using Amazon.AWSToolkit.EC2;

using Amazon.ElasticBeanstalk.Model;

using log4net;
using Amazon.IdentityManagement.Model;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.LegacyDeployment
{
    /// <summary>
    /// Interaction logic for AWSOptionsPage.xaml
    /// </summary>
    internal partial class AWSOptionsPage : INotifyPropertyChanged
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(AWSOptionsPage));

        public AWSOptionsPage()
        {
            DataContext = this;
            InstanceTypes = new ObservableCollection<InstanceType>();

            InitializeComponent();

            // switch on grouping for the instance type dropdown
            var instanceTypesView = (CollectionView)CollectionViewSource.GetDefaultView(_instanceTypesSelector.ItemsSource);
            var familyGroupDescription = new PropertyGroupDescription("HardwareFamily");
            instanceTypesView.GroupDescriptions.Add(familyGroupDescription);

            this._keyPairSelector.OnKeyPairSelectionChanged += new EventHandler<EventArgs>(KeyPairSelectionChanged);
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
                if (this._solutionStack.SelectedItem != null)
                    return this._solutionStack.SelectedItem as string;
                else
                    return string.Empty;
            }
        }

        public string CustomAMIID
        {
            get { return this._customAMI.Text.Trim(); }
            set { this._customAMI.Text = value; }
        }

        public string SelectedInstanceTypeID
        {
            get
            {
                InstanceType instanceType = this._instanceTypesSelector.SelectedItem as InstanceType;
                if (instanceType != null)
                    return instanceType.Id;
                else
                    return string.Empty;
            }
        }

        public InstanceType SelectedInstanceType
        {
            get
            {
                return this._instanceTypesSelector.SelectedItem as InstanceType;
            }
        }

        public string SelectedInstanceProfile
        {
            get
            {
                if (_role.IsEnabled && _role.SelectedItem != null)
                {
                    if (_role.SelectedIndex == 0) 
                        return BeanstalkParameters.DefaultRoleName;
                    else
                        return _role.SelectedItem as string;
                }

                return null;
            }
        }

        public void SetInstanceProfiles(IEnumerable<InstanceProfile> profiles, string autoSelect)
        {
            var instanceProfiles = new List<string>();

            instanceProfiles.Add(string.Format("Use the default role ({0})", BeanstalkParameters.DefaultRoleName));
            foreach (var ip in profiles)
            {
                if (ip.InstanceProfileName != BeanstalkParameters.DefaultRoleName)
                    instanceProfiles.Add(ip.InstanceProfileName);
            }

            _role.ItemsSource = instanceProfiles;
            _role.SelectedIndex = 0;
        }

        public bool LaunchIntoVPC
        {
            get
            {
                return this._launchIntoVPC.IsChecked.GetValueOrDefault();
            }
        }

        public void QueryKeyPairSelection(out string keypairName, out bool createNew)
        {
            keypairName = _keyPairSelector.SelectedKeyPairName;
            createNew = !_keyPairSelector.IsExistingKeyPairSelected;
        }

        public bool HasKeyPairSelection
        {
            get
            {
                return !string.IsNullOrEmpty(_keyPairSelector.SelectedKeyPairName);
            }
        }

        public void SetSolutionStacks(IEnumerable<string> stacks, string autoSelectStack)
        {
            if (stacks != null)
            {
                _solutionStack.ItemsSource = stacks;
                if (!string.IsNullOrEmpty(autoSelectStack))
                    _solutionStack.SelectedItem = autoSelectStack;
                else
                    _solutionStack.SelectedIndex = 0;
            }
        }

        public ObservableCollection<InstanceType> InstanceTypes { get; private set; }

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
            EC2ServiceMeta serviceMeta = EC2ServiceMeta.Instance;
            foreach (string sizeId in instanceTypes)
            {
                InstanceType type = serviceMeta.ById(sizeId);
                if (type != null)
                    InstanceTypes.Add(type);
                else
                    LOGGER.ErrorFormat("Unable to find instance type {0} in available service metadata", sizeId);
            }

            if (InstanceTypes.Count > 0)
                _instanceTypesSelector.SelectedIndex = 0;
            _instanceTypesSelector.Cursor = Cursors.Arrow;
        }

        public void SetExistingKeyPairs(ICollection<string> existingKeyPairs, ICollection<string> keyPairsStoredInToolkit, string autoSelectPair)
        {
            _keyPairSelector.SetExistingKeyPairs(existingKeyPairs, keyPairsStoredInToolkit, autoSelectPair);
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
            StringBuilder lbl = new StringBuilder("Key pair");

            if (string.IsNullOrEmpty(_customAMI.Text))
                lbl.Append(" *");
            lbl.Append(":");

            KeyPairLabel.Content = lbl.ToString();
            NotifyPropertyChanged("customami");
        }

        private void SetVPCState()
        {
            var isLegacy = BeanstalkDeploymentEngine.IsLegacyContainer(this._solutionStack.SelectedItem as string);
            this._launchIntoVPC.IsEnabled = !isLegacy;
            this._role.IsEnabled = !isLegacy;

            if (isLegacy)
            {
                this._launchIntoVPC.IsChecked = false;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.OriginalString));
            e.Handled = true;
        }

        private void _launchIntoVPC_Click(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged("vpc");
        }
    }
}
