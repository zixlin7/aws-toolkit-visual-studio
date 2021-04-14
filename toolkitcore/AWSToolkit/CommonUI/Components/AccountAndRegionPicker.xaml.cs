using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.State;

namespace Amazon.AWSToolkit.CommonUI.Components
{
    /// <summary>
    /// This is a reusable control that allows users to select
    /// a Credentials and Region pairing, much like the AWS Explorer.
    /// To use this control, data bind it with a <see cref="AccountAndRegionPickerViewModel"/> object.
    /// </summary>
    public partial class AccountAndRegionPicker
    {
        /// <summary>
        /// Indicates that a Connection (Credentials-Region pairing) or its validity has changed.
        /// </summary>
        public event EventHandler ConnectionChanged;

        private AccountAndRegionPickerViewModel _viewModel;
        private AwsConnectionManager _connectionManager;

        public AccountAndRegionPicker()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            Unloaded += OnUnloaded;
            DataContextChanged += OnDataContextChanged;
            _accountSelector.PropertyChanged += _accountSelector_PropertyChanged;

            SetViewModel(DataContext as AccountAndRegionPickerViewModel);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= OnUnloaded;
            // Unfortunately we have to re-listen for Loaded events, because some wizard flows can 
            // go "back" a page to one that contains this picker, and we have to re-set it up.
            Loaded += OnLoaded;

            DataContextChanged -= OnDataContextChanged;
            _accountSelector.PropertyChanged -= _accountSelector_PropertyChanged;
            SetViewModel(null);
        }

        /// <summary>
        /// Hold onto the viewmodel whenever one is assigned as the DataContext
        /// </summary>
        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            SetViewModel(e.NewValue as AccountAndRegionPickerViewModel);
        }

        private void SetViewModel(AccountAndRegionPickerViewModel viewModel)
        {
            // Un-register the existing viewmodel/state
            if (_viewModel != null) { _viewModel.PropertyChanged -= ViewModel_PropertyChanged; }
            if (_connectionManager != null)
            {
                _connectionManager.ConnectionStateChanged -= ConnectionManager_ConnectionStateChanged;
                _connectionManager.Dispose();
            }

            _viewModel = viewModel;

            // Register and setup the viewmodel/state
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
                _connectionManager = _viewModel.CreateConnectionManager();
                _connectionManager.ConnectionStateChanged += ConnectionManager_ConnectionStateChanged;

                // Populate ListBoxes and Initialize UI
                _viewModel.Accounts = ToolkitFactory.Instance.RootViewModel.RegisteredAccounts;
                OnRegionChanged();
                OnAccountChanged();
            }
        }

        private void ConnectionManager_ConnectionStateChanged(object sender, Credentials.Utils.ConnectionStateChangeArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _viewModel.ConnectionIsValid = e.State is ConnectionState.ValidConnection;
                _viewModel.IsValidating = e.State is ConnectionState.ValidatingConnection;
                _viewModel.ValidationMessage = e.State.Message;
                _viewModel.ConnectionIsBad = !_viewModel.ConnectionIsValid && e.State.IsTerminal;

                RaiseConnectionChanged();
            });
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.Accounts))
            {
                OnAccountsChanged();
            }
            else if (e.PropertyName == nameof(_viewModel.Account))
            {
                OnAccountChanged();
            }
            else if (e.PropertyName == nameof(_viewModel.PartitionId))
            {
                OnPartitionIdChanged();
            }
            else if (e.PropertyName == nameof(_viewModel.Region))
            {
                OnRegionChanged();
            }
        }

        private void OnAccountsChanged()
        {
            var currentAccountId = _viewModel.Account?.Identifier?.Id;
            _accountSelector.PopulateComboBox(_viewModel.Accounts);
            _viewModel.Account = _viewModel.Accounts.FirstOrDefault(a => a.Identifier.Id == currentAccountId);
        }

        private void OnAccountChanged()
        {
            _accountSelector.SelectedAccount = _viewModel.Account;

            // Propagate changes to Region (via partition) if the account changed
            _viewModel.PartitionId = _viewModel.Account?.PartitionId;

            _connectionManager.ChangeConnectionSettings(_viewModel.Account?.Identifier, _viewModel.Region);
        }

        private void OnPartitionIdChanged()
        {
            var currentRegionId = _viewModel.Region?.Id;

            var regionsView = CollectionViewSource.GetDefaultView(_viewModel.Regions);

            using (regionsView.DeferRefresh())
            {
                _viewModel.ShowRegions(_viewModel.PartitionId);
            }

            // When the Partition changes the list of Regions, the currently selected Region
            // is likely cleared (from databinding).
            // Make a reasonable region selection, if the currently selected region is not available.
            var selectedRegion = _viewModel.GetRegion(currentRegionId) ??
                                 _viewModel.GetRegion(_viewModel.GetMostRecentRegionId(_viewModel.PartitionId)) ??
                                 _viewModel.Regions.FirstOrDefault();

            _viewModel.Region = selectedRegion;

            regionsView.Refresh();
        }

        private void OnRegionChanged()
        {
            _connectionManager.ChangeConnectionSettings(_viewModel.Account?.Identifier, _viewModel.Region);
        }

        private void _accountSelector_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var accountSelector = sender as RegisteredProfilesPicker;
            if (accountSelector == null) { return; }

            _viewModel.Account = accountSelector.SelectedAccount;
        }

        private void RaiseConnectionChanged()
        {
            ConnectionChanged?.Invoke(this, new EventArgs());
        }

        private void RetryConnectionValidation(object sender, RoutedEventArgs e)
        {
            _connectionManager.RefreshConnectionState();
        }
    }
}
