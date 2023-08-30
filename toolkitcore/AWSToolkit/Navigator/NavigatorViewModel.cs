using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Regions;
using log4net;

namespace Amazon.AWSToolkit.Navigator
{
    /// <summary>
    /// Backing data for the AWS Explorer (NavigatorControl)
    /// </summary>
    public class NavigatorViewModel : BaseModel
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(NavigatorViewModel));

        private readonly IRegionProvider _regionProvider;

        /// <summary>
        /// Tracks the most recent non-null region that was used with a partition Id
        /// </summary>
        private readonly Dictionary<string, string> _lastRegionIdPerPartitionId = new Dictionary<string, string>();
        
        private string _errorMessage;
        private string _partitionId;
        private ToolkitRegion _region;
        private bool _isConnectionValid;
        private bool _isConnectionTerminal;
        private string _connectionMessage;
        private NavigatorAccountConnectionStatus _connectionStatus;
        private AccountViewModel _account;
        private ObservableCollection<ToolkitRegion> _regions = new ObservableCollection<ToolkitRegion>();
        private ObservableCollection<AccountViewModel> _accounts = new ObservableCollection<AccountViewModel>();
 
        private ObservableCollection<NavigatorCommand> _navigatorCommands =
            new ObservableCollection<NavigatorCommand>();
        private ICommand _addAccountCommand;
        private ICommand _deleteAccountCommand;
        private ICommand _editAccountCommand;

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                SetProperty(ref _errorMessage, value, () => ErrorMessage);
                NotifyPropertyChanged(nameof(ShowErrorMessage));
            }
        }

        public bool ShowErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

        /// <summary>
        /// Id of the partition that the "selected account" is associated with.
        /// Example: "aws"
        /// </summary>
        public string PartitionId
        {
            get => _partitionId;
            set
            {
                SetProperty(ref _partitionId, value, () => PartitionId);
            }
        }

        /// <summary>
        /// Currently selected Region
        /// </summary>
        public ToolkitRegion Region
        {
            get => _region;
            set
            {
                SetProperty(ref _region, value, () => Region);

                if (!string.IsNullOrWhiteSpace(_region?.Id) && !string.IsNullOrWhiteSpace(PartitionId))
                {
                    _lastRegionIdPerPartitionId[PartitionId] = _region.Id;
                }
            }
        }

        /// <summary>
        /// Indicates if the connection is valid or not i.e.
        /// if the connection is valid, load the tree nodes else
        /// hide the connection messaging panel
        /// </summary>
        public bool IsConnectionValid
        {
            get => _isConnectionValid;
            set
            {
                SetProperty(ref _isConnectionValid, value, () => IsConnectionValid);
            }
        }

        /// <summary>
        /// Indicates whether the connection state is terminal or not
        /// i.e. if it is validating or has reached a final state
        /// </summary>
        public bool IsConnectionTerminal
        {
            get => _isConnectionTerminal;
            set
            {
                SetProperty(ref _isConnectionTerminal, value, () => IsConnectionTerminal);
            }
        }

        /// <summary>
        /// Represents the current state of the connected account.
        /// Examples: Info, Warning, Error
        /// </summary>
        public NavigatorAccountConnectionStatus ConnectionStatus
        {
            get => _connectionStatus;
            set
            {
                SetProperty(ref _connectionStatus, value, () => ConnectionStatus);
            }
        }

        /// <summary>
        /// Message displayed with the current connection state
        /// </summary>
        public string ConnectionMessage
        {
            get => _connectionMessage;
            set
            {
                SetProperty(ref _connectionMessage, value, () => ConnectionMessage);
            }
        }

        /// <summary>
        /// Navigator Commands representing a collection of actions displayed in the AWS Explorer connection messaging panel
        /// </summary>
        public ObservableCollection<NavigatorCommand> NavigatorCommands
        {
            get => _navigatorCommands;
            set
            {
                SetProperty(ref _navigatorCommands, value, () => NavigatorCommands);
            }
        }

        /// <summary>
        /// Regions that can be selected in the AWS Explorer
        /// </summary>
        public ObservableCollection<ToolkitRegion> Regions
        {
            get => _regions;
            set
            {
                SetProperty(ref _regions, value, () => Regions);                
            }
        }


        /// <summary>
        /// Currently selected Account
        /// </summary>
        public AccountViewModel Account
        {
            get => _account;
            set
            {
                SetProperty(ref _account, value, () => Account);
            }
        }

        /// <summary>
        /// Accounts that can be selected in the AWS Explorer
        /// </summary>
        public ObservableCollection<AccountViewModel> Accounts
        {
            get => _accounts;
            private set
            {
                SetProperty(ref _accounts, value, () => Accounts);
            }
        }

        /// <summary>
        /// Prompts user to add new account
        /// </summary>
        public ICommand AddAccountCommand
        {
            get => _addAccountCommand;
            set
            {
                SetProperty(ref _addAccountCommand, value, () => AddAccountCommand);
            }
        }

        /// <summary>
        /// Prompts user to delete currently selected account in the navigator
        /// </summary>
        public ICommand DeleteAccountCommand
        {
            get => _deleteAccountCommand;
            set
            {
                SetProperty(ref _deleteAccountCommand, value, () => DeleteAccountCommand);
            }
        }

        /// <summary>
        /// Prompts user to edit currently selected account in the navigator
        /// </summary>
        public ICommand EditAccountCommand
        {
            get => _editAccountCommand;
            set
            {
                SetProperty(ref _editAccountCommand, value, () => EditAccountCommand);
            }
        }

        public NavigatorViewModel(IRegionProvider regionProvider)
        {
            _regionProvider = regionProvider;
        }

        /// <summary>
        /// Updates the <see cref="Regions"/> list with regions contained by the given partitionId.
        /// Side effect: Databinding generally sets <see cref="Region"/> to null as a result.
        /// </summary>
        public void ShowRegionsForPartition(string partitionId)
        {
            var regions = _regionProvider.GetRegions(partitionId);

            Regions = new ObservableCollection<ToolkitRegion>(regions.OrderBy(r => r.DisplayName));
        }

        /// <summary>
        /// Returns the requested region from <see cref="Regions"/>, or null if not available.
        /// </summary>
        public ToolkitRegion GetRegion(string regionId)
        {
            if (string.IsNullOrWhiteSpace(regionId)) { return null; }

            return Regions.FirstOrDefault(r => r.Id == regionId);
        }

        /// <summary>
        /// Retrieves the RegionId most recently used with the queried partition Id.
        /// Returns null if the partition hasn't been used.
        /// </summary>
        public string GetMostRecentRegionId(string partitionId)
        {
            if (string.IsNullOrWhiteSpace(partitionId)) { return null; }

            if (_lastRegionIdPerPartitionId.TryGetValue(partitionId, out var regionId))
            {
                return regionId;
            }

            return null;
        }

        /// <summary>
        /// Updates the list of Accounts, attempting to maintain or reselect
        /// the current selected Account, based on Credentials Id.
        /// </summary>
        public void UpdateAccounts(IList<AccountViewModel> accounts)
        {
            try
            {
                // If the incoming list of accounts doesn't have the currently
                // selected account, we'll have to try re-selecting it
                var reselectAccount = !accounts.Contains(Account);

                // We want to add accounts to the list and select the "current" account
                // (if necessary) before removing accounts from the list.
                // This prevents data binding scenarios that cause an alternate (or null)
                // account to get selected when it is not found in the updated list of accounts.
                var accountsToAdd = accounts.Except(Accounts).ToList();
                var accountsToRemove = Accounts.Except(accounts).ToList();

                // Add the new accounts to the list
                AddAccounts(accountsToAdd);

                // Ensure the current account remains selected, based on Credentials Id.
                if (reselectAccount)
                {
                    var newAccount = accounts.FirstOrDefault(x =>
                        x.Identifier != null && x.Identifier.Id == Account?.Identifier?.Id);
                    Account = newAccount;
                }

                // Remove the outdated accounts from the list
                RemoveAccounts(accountsToRemove);

            }
            catch (Exception e)
            {
                Logger.Error("Error trying to update the list of Accounts", e);
            }
        }

        private void RemoveAccounts(List<AccountViewModel> accountsToRemove)
        {
            try
            {
                accountsToRemove.ForEach(x => Accounts.Remove(x));
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private void AddAccounts(List<AccountViewModel> accountsToAdd)
        {
            try
            {
                accountsToAdd.ForEach(x => Accounts.Add(x));
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}
