using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Account.Controller;
using Amazon.AWSToolkit.Account.View;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.CommonUI.Components
{
    /// <summary>
    /// Interaction logic for AccountAndRegionPicker.xaml
    /// </summary>
    public partial class AccountAndRegionPicker : INotifyPropertyChanged
    {
        // property names used with NotifyPropertyChanged
        public static readonly string uiProperty_Region = "region";
        public static readonly string uiProperty_Account = "account";

        readonly object _syncObj = new object();
        RegisterAccountController _registerAccountController = null;
        IEnumerable<string> _serviceNames;

        readonly HashSet<string> _verifiedAccounts = new HashSet<string>();

        public AccountAndRegionPicker()
        {
            InitializeComponent();
        }

        public void SwitchToVerticalLayout()
        {
            this._ctlHorizontalLayout.Visibility = Visibility.Collapsed;
            this._ctlVerticalLayout.Visibility = Visibility.Visible;

            this._ctlHorizontalHolderAccount.Children.Remove(this._ctlAccount);
            this._ctlHorizontalHolderRegion.Children.Remove(this._regionSelector);

            this._ctlVerticalHolderAccount.Children.Add(this._ctlAccount);
            this._ctlVerticalHolderRegion.Children.Add(this._regionSelector);
        }

        public void Initialize()
        {
            Initialize(ToolkitFactory.Instance.Navigator.SelectedAccount, ToolkitFactory.Instance.Navigator.SelectedRegionEndPoints, null);
        }

        public void Initialize(AccountViewModel selectedAccount, RegionEndPointsManager.RegionEndPoints selectedRegion, IEnumerable<string> serviceNames)
        {
            _rootViewModel = ToolkitFactory.Instance.RootViewModel;
            this._serviceNames = serviceNames;

            this._accountSelector.PropertyChanged += _accountSelector_PropertyChanged;
            this._accountSelector.PopulateComboBox(this.Accounts);
            if (selectedAccount != null)
            {
                this._accountSelector.SelectedAccount = selectedAccount;
                NotifyPropertyChanged(uiProperty_Account);
            }

            UpdateRegions(selectedRegion);

        }

        private void UpdateRegions(RegionEndPointsManager.RegionEndPoints selectedRegion)
        {
            var regions = new List<RegionEndPointsManager.RegionEndPoints>();
            foreach (RegionEndPointsManager.RegionEndPoints rep in RegionEndPointsManager.GetInstance().Regions)
            {
                if (this.SelectedAccount == null)
                    continue;

                if (this.SelectedAccount.HasRestrictions || rep.HasRestrictions)
                {
                    if (!rep.ContainAnyRestrictions(this.SelectedAccount.Restrictions))
                    {
                        continue;
                    }
                }

                if (this._serviceNames == null)
                {
                    regions.Add(rep);
                }
                else
                {
                    foreach (var name in this._serviceNames)
                    {
                        if (rep.GetEndpoint(name) != null)
                        {
                            regions.Add(rep);
                            break;
                        }
                    }
                }
            }

            this._regionSelector.ItemsSource = regions;
            if (this._regionSelector.Items.Count != 0)
            {
                RegionEndPointsManager.RegionEndPoints region = null;
                if (selectedRegion != null)
                {
                    // if the requested selection does not exist in our subset, attempt fallback to us-east-1
                    // (regardless of any toolkit default) for safety and if that doesn't exist, go with the
                    // first available region in the subset
                    foreach (var r in regions.Where(r => r.SystemName.Equals(selectedRegion.SystemName, StringComparison.OrdinalIgnoreCase)))
                    {
                        region = r;
                        break;
                    }

                    if (region == null)
                        region = regions.FirstOrDefault(r => r.SystemName.Equals(RegionEndPointsManager.US_EAST_1, StringComparison.OrdinalIgnoreCase));

                    if (region == null)
                        region = regions[0];
                }
                else
                {
                    region = RegionEndPointsManager.GetInstance().GetDefaultRegionEndPoints();
                }

                this._regionSelector.SelectedItem = region;
            }
        }



        AWSViewModel _rootViewModel;

        public AWSViewModel RootViewModel
        {
            get => IsInitialized ? _rootViewModel : null;
            set
            {
                this._rootViewModel = value;
                this._accountSelector.IsEnabled = this.Accounts.Count != 0;
            }
        }

        ObservableCollection<AccountViewModel> _accounts;
        public ObservableCollection<AccountViewModel> Accounts
        {
            get
            {
                if (RootViewModel == null)
                    return null;

                if (this._accounts == null)
                {
                    this._accounts = new ObservableCollection<AccountViewModel>();

                    foreach (var account in this.RootViewModel.RegisteredAccounts)
                    {
                        this._accounts.Add(account);
                    }
                }

                return this._accounts;
            }
        }

        public AccountViewModel SelectedAccount
        {
            get => this._accountSelector.SelectedAccount as AccountViewModel;
            set
            {
                if (IsInitialized)
                {
                    this._accountSelector.SelectedAccount = value;
                    UpdateRegions(this.SelectedRegion);
                }
            }
        }

        bool _accountValidationPending = false;
        public bool AccountValidationPending
        {
            get
            {
                lock (_syncObj)
                    return _accountValidationPending;
            }

            set
            {
                lock (_syncObj)
                    _accountValidationPending = value;
            }
        }

        public bool IsSelectedAccountValid
        {
            get
            {
                if (AccountValidationPending)
                    return false;

                // collection only ever accessed on UI thread, no need to lock
                AccountViewModel account = _accountSelector.SelectedAccount as AccountViewModel;
                return account != null && _verifiedAccounts.Contains(account.SettingsUniqueKey);
            }
        }

        void _accountEntryPopup_Loaded(object sender, RoutedEventArgs e)
        {
            if (_registerAccountController == null)
            {
                _registerAccountController = new RegisterAccountController();
                RegisterAccountControl control = new RegisterAccountControl(_registerAccountController);
                control.SetMandatoryFieldsReadyCallback(MandatoryFieldsReadinessChange);
                _accountFieldContainer.Content = control;
                _popupAccountOK.IsEnabled = false;
            }
        }

        void _popupAccountOK_Click(object sender, RoutedEventArgs e)
        {
            _registerAccountController.Persist();
            _useOtherAccount.IsChecked = false;

            RootViewModel.Refresh();

            this._accounts.Clear();
            AccountViewModel selectedAccount = null;
            foreach (AccountViewModel account in RootViewModel.RegisteredAccounts)
            {
                if (!account.HasRestrictions)
                {
                    this._accounts.Add(account);

                    if (string.Compare(account.AccountDisplayName, _registerAccountController.Model.DisplayName) == 0)
                    {
                        selectedAccount = account;
                    }
                }
            }

            this._accountSelector.PopulateComboBox(this._accounts);
            if (this.Accounts.Count > 0)
            {
                _accountSelector.IsEnabled = true;

                if (selectedAccount != null)
                {
                    SelectedAccount = selectedAccount;
                }
            }
        }

        void _popupAccountCancel_Click(object sender, RoutedEventArgs e)
        {
            _useOtherAccount.IsChecked = false;
        }

        private void MandatoryFieldsReadinessChange(bool allCompleted)
        {
            _popupAccountOK.IsEnabled = allCompleted;
        }

        // Attempt to verify that the selected/added account is (a) valid and (b) signed up for CloudFormation.
        // This is awkward to handle outside the page.
        private void _accountSelector_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateRegions(this.SelectedRegion);
            NotifyPropertyChanged(uiProperty_Account);
        }
        public RegionEndPointsManager.RegionEndPoints SelectedRegion
        {
            get => this._regionSelector.SelectedItem as RegionEndPointsManager.RegionEndPoints;
            set { if (IsInitialized) this._regionSelector.SelectedItem = value; }
        }

        private void _regionSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged(uiProperty_Region);
        }
    }
}
