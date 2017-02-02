using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


using log4net;
using Amazon.AWSToolkit.Account;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;

namespace Amazon.AWSToolkit.CommonUI.Components
{
    /// <summary>
    /// Interaction logic for RegisteredProfilesPicker.xaml
    /// </summary>
    public partial class RegisteredProfilesPicker : UserControl
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(RegisteredProfilesPicker));

        TextBlock _ctlComboSelectedDisplay;

        public event PropertyChangedEventHandler PropertyChanged;

        public RegisteredProfilesPicker()
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
                if (this._ctlCombo.SelectedItem != null)
                {
                    FormatDisplayValue();
                }
            }
        }

        private void _ctlCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FormatDisplayValue();

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AccountSelection"));
        }

        public AccountViewModel SelectedAccount
        {
            get
            {
                var item = this._ctlCombo.SelectedItem as ProfileComboItem;
                return item?.Account;
            }
            set
            {
                if (value == null)
                {
                    this._ctlCombo.SelectedItem = null;
                    return;
                }

                foreach (ProfileComboItem comboItem in this._ctlCombo.Items)
                {
                    if (comboItem.Type != ProfileComboItem.ItemType.Profile)
                        continue;

                    if (string.Equals(comboItem.Account.SettingsUniqueKey, value.SettingsUniqueKey))
                    {
                        this._ctlCombo.SelectedItem = comboItem;
                        break;
                    }
                }
            }
        }

        public void PopulateComboBox(IList<AccountViewModel> accounts)
        {
            var netSdkAccounts = new List<ProfileComboItem>();
            var sharedCredentialsAccounts = new List<ProfileComboItem>();

            foreach(var account in accounts)
            {
                if(account.ProfileStore is NetSDKCredentialsFile)
                {
                    netSdkAccounts.Add(new ProfileComboItem(ProfileComboItem.ItemType.Profile, account.DisplayName, account));
                }
                else if(account.ProfileStore is SharedCredentialsFile)
                {
                    sharedCredentialsAccounts.Add(new ProfileComboItem(ProfileComboItem.ItemType.Profile, account.DisplayName, account));
                }
            }

            var combinedList = new List<ProfileComboItem>();

            bool useCategories = netSdkAccounts.Count > 0 && sharedCredentialsAccounts.Count > 0;

            if(netSdkAccounts.Count > 0)
            {
                if(useCategories)
                {
                    combinedList.Add(new ProfileComboItem(ProfileComboItem.ItemType.Category, ".NET Credentials", null));
                }
                combinedList.AddRange(netSdkAccounts);
            }

            if (netSdkAccounts.Count > 0)
            {
                if (useCategories)
                {
                    combinedList.Add(new ProfileComboItem(ProfileComboItem.ItemType.Category, "Shared Credentials", null));
                }
                combinedList.AddRange(sharedCredentialsAccounts);
            }

            this._ctlCombo.ItemsSource = combinedList;
        }

        private void FormatDisplayValue()
        {
            if (this._ctlComboSelectedDisplay == null)
                return;

            if(this._ctlCombo.SelectedItem != null)
            {
                this._ctlComboSelectedDisplay.Text = this._ctlCombo.SelectedItem.ToString();
            }
            else
            {
                this._ctlComboSelectedDisplay.Text = string.Empty;
            }
        }

        public class ProfileComboItem : INotifyPropertyChanged
        {
            public enum ItemType { Category = 1, Profile =2}

            public ItemType Type { get; private set; }
            public string DisplayName { get; private set; }

            public AccountViewModel Account { get; private set; }

            public ProfileComboItem(ItemType type, string displayName, AccountViewModel account)
            {
                this.Type = type;
                this.DisplayName = displayName;
                this.Account = account;
            }

            public bool IsSelectable
            {
                get
                {
                    return this.Account != null;
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

            public override string ToString()
            {
                return this.DisplayName;
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

        }
    }
}
