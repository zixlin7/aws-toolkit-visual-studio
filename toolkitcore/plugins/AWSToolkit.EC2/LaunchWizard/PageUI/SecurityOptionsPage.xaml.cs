using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.LaunchWizard.PageUI
{
    /// <summary>
    /// Interaction logic for SecurityOptionsPage.xaml
    /// </summary>
    public partial class SecurityOptionsPage : INotifyPropertyChanged
    {
        public static readonly string uiProperty_KeyPairOption = "keyPairOption";
        public static readonly string uiProperty_KeyPair = "keyPair";
        public static readonly string uiProperty_KeyPairName = "keyPairName";
        public static readonly string uiProperty_SecurityGroupOption = "securityGroupOption";
        public static readonly string uiProperty_SecurityGroup = "securityGroup";

        public enum KeyPairSelectionMode
        {
            noPair,
            createPair,
            usePair
        }

        public SecurityOptionsPage()
        {
            InitializeComponent();
            _securityGroupEditor.PropertyChanged += new PropertyChangedEventHandler(securityEditorPropertyChanged);
        }

        public SecurityOptionsPage(IAWSWizardPageController controller)
            : this()
        {
            this.PageController = controller;
        }

        public IAWSWizardPageController PageController { get; set; }

        public void SetExistingKeyPairs(ICollection<string> keypairNames, ICollection<string> keyPairsStoredInToolkit)
        {
            this._existingKeyPairs.ItemsSource = keypairNames;
            if (keypairNames == null || keypairNames.Count == 0)
                this._createKeyPair.IsChecked = true;
            else
            {
                this._existingKeyPairs.SelectedIndex = 0;
                this._existingKeyPair.IsChecked = true;
            }
            this._existingKeyPairs.Cursor = Cursors.Arrow;
        }

        public void GetSelectedKeyPairOptions(out KeyPairSelectionMode keyPairMode, out string pairName)
        {
            if (this._noKeyPair.IsChecked == true)
            {
                keyPairMode = KeyPairSelectionMode.noPair;
                pairName = string.Empty;
            }
            else
            {
                if (this._createKeyPair.IsChecked == true)
                    keyPairMode = KeyPairSelectionMode.createPair;
                else
                    keyPairMode = KeyPairSelectionMode.usePair;
                pairName = keyPairMode == KeyPairSelectionMode.createPair ? this._newKeyPairName.Text : this._existingKeyPairs.SelectedItem as string;
            }
        }

        public ICollection<SecurityGroup> ExistingGroups
        {
            set
            {
                if (value == null || !value.Any())
                {
                    _existingSecurityGroups.ItemsSource = null;
                    _existingSecurityGroup.IsEnabled = false;
                    _existingSecurityGroups.IsEnabled = false;
                    _createSecurityGroup.IsChecked = true;
                }
                else
                {
                    var wrapperCol = value.Select(@group => new SecurityGroupWrapper(@group)).ToList();

                    _existingSecurityGroups.ItemsSource = wrapperCol;
                    _existingSecurityGroup.IsChecked = true;
                    _existingSecurityGroups.IsEnabled = true;
                    _existingSecurityGroups.Cursor = Cursors.Arrow;
                }
            }
        }

        public bool CreateSecurityGroup => this._createSecurityGroup.IsChecked == true;

        public EmbeddedSecurityGroupControl GroupEditor => this._securityGroupEditor;

        public ICollection<SecurityGroupWrapper> SelectedGroups
        {
            get
            {
                if (this.CreateSecurityGroup)
                    return null;

                List<SecurityGroupWrapper> selectedGroups = new List<SecurityGroupWrapper>();
                foreach (SecurityGroupWrapper groupWrapper in this._existingSecurityGroups.SelectedItems)
                {
                    selectedGroups.Add(groupWrapper);
                }

                return selectedGroups;
            }
        }

        public bool IsValidToMoveOffPage
        {
            get
            {
                bool isValid = false;

                if (this._existingKeyPair.IsChecked == true)
                    isValid = this._existingKeyPairs.SelectedItem != null;
                else
                    if (this._createKeyPair.IsChecked == true)
                    {
                        isValid = this._newKeyPairName.Text.Length > 0;
                        if (isValid)
                        {
                            // need to find a way to do this via a validation rule so we can take advantage of adorners
                            // to alert user to the problem...
                            ICollection<string> existingNames 
                                = this._existingKeyPairs.ItemsSource as ICollection<string>;
                            if (existingNames.Contains(this._newKeyPairName.Text, StringComparer.CurrentCultureIgnoreCase))
                                isValid = false;
                        }
                    }
                    else
                        isValid = true;

                if (isValid)
                {
                    if (this._existingSecurityGroup.IsChecked == true)
                        isValid = this._existingSecurityGroups.SelectedItem != null;
                    else
                    {
                        // group name and desc are mandatory in ec2 api; don't think it makes sense to
                        // have a group with no rules either
                        isValid = this._securityGroupEditor.GroupName.Length > 0
                            && this._securityGroupEditor.GroupDescription.Length > 0
                            && this._securityGroupEditor.GroupPermissions.Count > 0;
                        if (isValid)
                        {
                            // need to find a way to do this via a validation rule so we can take advantage of adorners
                            // to alert user to the problem...
                            ICollection<SecurityGroupWrapper> existingGroups 
                                = this._existingSecurityGroups.ItemsSource as ICollection<SecurityGroupWrapper>;
                            string newGroupName = this._securityGroupEditor.GroupName;
                            if (existingGroups.Any<SecurityGroupWrapper>((W) => { return string.Compare(W.DisplayName, newGroupName, true) == 0; }))
                                isValid = false;
                        }
                    }
                }

                return isValid;
            }
        }

        public bool AllowKeyPairCreation
        {
            set
            {
                _createKeyPair.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                _newKeyPairName.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }
                                                                               
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            if (this._existingSecurityGroups.Items.Count > 0)
                this._existingSecurityGroup.IsChecked = true;
            else
                this._createSecurityGroup.IsChecked = true;
        }

        // one the key pair radio buttons has been checked; notify page controller to
        // check for page transition validity
        private void KeyPairOption_Checked(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged(uiProperty_KeyPairOption);
        }

        void ExistingKeyPairs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged(uiProperty_KeyPair);
        }

        void NewKeyPairName_TextChanged(object sender, TextChangedEventArgs e)
        {
            NotifyPropertyChanged(uiProperty_KeyPairName);
        }

        private void SecurityGroupOption_Checked(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged(uiProperty_SecurityGroupOption);
        }

        private void ExistingSecurityGroups_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged(uiProperty_SecurityGroup);
        }

        void securityEditorPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged(e.PropertyName);
        }
    }
}
