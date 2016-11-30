using System;
using System.Collections.Generic;
using System.ComponentModel;
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

using Amazon.EC2.Model;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

using Amazon.AWSToolkit.EC2;
using Amazon.AWSToolkit.CloudFormation.Model;
using Amazon.AWSToolkit.CloudFormation.WizardPages.PageWorkers;

using Amazon.AWSToolkit.SNS.Nodes;

using System.Collections.ObjectModel;

namespace Amazon.AWSToolkit.CloudFormation.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for AWSOptionsPage.xaml
    /// </summary>
    public partial class AWSOptionsPage : INotifyPropertyChanged
    {
        // property names used with NotifyPropertyChanged
        public static readonly string uiProperty_InstanceType = "instancetype";
        public static readonly string uiProperty_CustomAMIID = "customamiid";
        public static readonly string uiProperty_KeyPair = "keypair";
        public static readonly string uiProperty_CreationTimeout = "creationtimeout";
        public static readonly string uiProperty_RollbackOnFailure = "rollbackonfailure";
        public static readonly string uiProperty_StackName = "stackname";
        public static readonly string uiProperty_SecurityGroup = "securitygroup";

        enum StackNameValidating
        {
            validationRequired,
            validationPending,
            validationDone_OK,
            validationDone_Failed
        }
        StackNameValidating _stackNameValidating;

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

        public AWSOptionsPage(IAWSWizardPageController pageController)
            : this()
        {
            PageController = pageController;
            LoadTopicList();
        }

        public IAWSWizardPageController PageController { get; set; }

        public ObservableCollection<InstanceType> InstanceTypes { get; private set; }

        public string SelectedInstanceTypeID
        {
            get
            {
                var instanceType = this._instanceTypesSelector.SelectedItem as InstanceType;
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

        /// <summary>
        /// Loads the instance types selector with the supplied collection and optionally sets the default
        /// type
        /// </summary>
        /// <param name="instanceTypes"></param>
        /// <param name="defaultInstanceTypeID">EC2 ID ('t1.micro') of the type to select or null/empty string to select first in set</param>
        public void SetInstanceTypes(IEnumerable<InstanceType> instanceTypes, string defaultInstanceTypeID)
        {
            InstanceTypes.Clear();

            if (instanceTypes != null)
            {
                foreach (var i in instanceTypes)
                {
                    InstanceTypes.Add(i);
                }

                if (!string.IsNullOrEmpty(defaultInstanceTypeID))
                {
                    foreach (var t in instanceTypes)
                    {
                        if (string.Compare(t.Id, defaultInstanceTypeID, true) == 0)
                        {
                            _instanceTypesSelector.SelectedItem = t;
                            break;
                        }
                    }
                }
                else
                {
                    if (InstanceTypes.Count() > 0)
                        _instanceTypesSelector.SelectedItem = InstanceTypes.ElementAt(0);
                }
            }

            NotifyPropertyChanged(uiProperty_InstanceType);
        }
        
        public void SetContainers(IEnumerable<ToolkitAMIManifest.ContainerAMI> containers, string defaultContainerName)
        {
            _defaultAMIs.ItemsSource = containers;
            foreach (var c in containers)
            {
                if (string.Compare(c.ContainerName, defaultContainerName, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    _defaultAMIs.SelectedItem = c;
                    break;
                }
            }
        }

        public ToolkitAMIManifest.ContainerAMI SelectedContainer
        {
            get
            {
                return _useDefaultAMI.IsChecked == true ? _defaultAMIs.SelectedItem as ToolkitAMIManifest.ContainerAMI : null;
            }    
        }

        public string CustomAMIID 
        {
            get { return !string.IsNullOrEmpty(_customAMI.Text) ? _customAMI.Text.Trim() : string.Empty; }
            set { _customAMI.Text = value; } 
        }

        string _stackNameValue;
        public string StackName 
        {
            get { return _stackNameValue; }
            set
            {
                _stackNameValue = value;
                _stackNameValidating = StackNameValidating.validationRequired;
                ValidateStackName();
                NotifyPropertyChanged(uiProperty_StackName);
            }
        }

        public bool IsStackNameValid
        {
            get
            {
                return ValidateStackName();
            }
        }

        public string SNSTopic
        {
            get { return _snsTopic.Text; }
        }

        public string SecurityGroupName
        {
            get
            {
                if (_securityGroups.SelectedItem != null)
                {
                    var sg = _securityGroups.SelectedItem as SecurityGroup;
                    return sg.GroupName;
                }

                return string.Empty;
            }
        }

        public void SetExistingSecurityGroups(IEnumerable<SecurityGroup> existingGroups, string autoSelectGroup)
        {
            _securityGroups.ItemsSource = existingGroups;
            if (existingGroups.Count<SecurityGroup>() != 0)
            {
                if (string.IsNullOrEmpty(autoSelectGroup))
                {
                    if (existingGroups.Count<SecurityGroup>() == 1)
                        _securityGroups.SelectedIndex = 0;
                    else
                    {
                        // try and preselect 'default' group so at least something is selected
                        var defaultGroup
                                = existingGroups.First<SecurityGroup>((group) => string.Compare(group.GroupName, "default", true) == 0);
                        if (defaultGroup != null)
                            _securityGroups.SelectedItem = defaultGroup;
                    }
                }
                else
                {
                    var preselectedGroup
                            = existingGroups.First<SecurityGroup>((group) => string.Compare(group.GroupName, autoSelectGroup, true) == 0);
                    if (preselectedGroup != null)
                        _securityGroups.SelectedItem = preselectedGroup;
                }
            }
            _securityGroups.Cursor = Cursors.Arrow;
        }

        bool ValidateStackName()
        {
            switch (_stackNameValidating)
            {
                case StackNameValidating.validationPending:
                    return SetInvalidStackName("Validating stack name/existence...");

                case StackNameValidating.validationRequired:
                    {
                        if (string.IsNullOrEmpty(StackName))
                            return SetInvalidStackName("A name must be supplied");

                        if (!SelectTemplateModel.IsValidStackName(StackName))
                            return SetInvalidStackName("The stack name must contain only letters, numbers, dashes and start with an alpha character.");

                        _stackNameValidating = StackNameValidating.validationPending;
                        new QueryExistingStackNameWorker(PageController.HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] as AccountViewModel,
                                                         StackName,
                                                         PageController.HostingWizard.Logger,
                                                         new QueryExistingStackNameWorker.DataAvailableCallback(OnQueryExistingStackNameCompleted));
                        return SetInvalidStackName("Validating stack name/existence...");
                    }

                case StackNameValidating.validationDone_Failed:
                    return false;

                default:
                    return true;
            }
        }

        bool SetInvalidStackName(string message)
        {
            _stackNameValidationMessage.Text = message;
            _validateOKImg.Visibility = Visibility.Collapsed;
            _validateFailImg.Visibility = Visibility.Visible;
            _stackNameValidating = StackNameValidating.validationDone_Failed;
            return false;
        }

        bool SetValidStackName(string message)
        {
            _stackNameValidationMessage.Text = message;
            _validateOKImg.Visibility = Visibility.Visible;
            _validateFailImg.Visibility = Visibility.Collapsed;
            _stackNameValidating = StackNameValidating.validationDone_OK;
            return true;
        }

        string _creationTimeoutValue = "None";
        public string CreationTimeout
        {
            get 
            {
                if (_creationTimeoutValue == "None")
                    return this._creationTimeoutValue;
                else
                    return this._creationTimeoutValue.Substring(0, this._creationTimeoutValue.IndexOf(' '));
            }
            set
            {
                this._creationTimeoutValue = value;
                NotifyPropertyChanged(uiProperty_CreationTimeout);
            }
        }

        bool _rollbackOnFailure = true;
        public bool RollbackOnFailure
        {
            get { return this._rollbackOnFailure; }
            set
            {
                this._rollbackOnFailure = value;
                NotifyPropertyChanged(uiProperty_RollbackOnFailure);
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

        public void SetExistingKeyPairs(ICollection<string> existingKeyPairs, ICollection<string> keyPairsStoredInToolkit, string autoSelectPair)
        {
            _keyPairSelector.SetExistingKeyPairs(existingKeyPairs, keyPairsStoredInToolkit, autoSelectPair);
        }

        void KeyPairSelectionChanged(object sender, EventArgs e)
        {
            NotifyPropertyChanged(uiProperty_KeyPair);
        }

        void _customAMI_LostFocus(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged(uiProperty_CustomAMIID);
        }

        void OnQueryExistingStackNameCompleted(bool stackNameInUse)
        {
            if (stackNameInUse)
                SetInvalidStackName("A stack with this name exists");
            else
                SetValidStackName("Stack name is valid and available");
            NotifyPropertyChanged(uiProperty_StackName);
        }

        private void LoadTopicList()
        {
            ISNSRootViewModel model = ToolkitFactory.Instance.Navigator.SelectedAccount.AccountViewModel.FindSingleChild<ISNSRootViewModel>(false);
            foreach (var child in model.Children)
            {
                var topic = child as ISNSTopicViewModel;
                if (topic == null)
                    continue;

                this._snsTopic.Items.Add(topic.TopicArn);
            }
        }

        private void OnCreateTopicClick(object sender, RoutedEventArgs e)
        {
            ISNSRootViewModel model = ToolkitFactory.Instance.Navigator.SelectedAccount.AccountViewModel.FindSingleChild<ISNSRootViewModel>(false);
            ISNSRootViewMetaNode meta = model.MetaNode as ISNSRootViewMetaNode;
            var results = meta.OnCreateTopic(model);

            if (results.Success)
            {
                string topicArn = results.Parameters["CreatedTopic"] as string;
                this._snsTopic.Items.Add(topicArn);
                this._snsTopic.Text = topicArn;
                model.AddTopic(topicArn);
            }
        }

        private void _securityGroups_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged(uiProperty_SecurityGroup);
        }
    }
}
