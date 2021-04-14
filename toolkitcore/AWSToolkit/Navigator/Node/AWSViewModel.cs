using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Context;
using Amazon.Runtime.Internal.Settings;

using log4net;

namespace Amazon.AWSToolkit.Navigator.Node
{
    public class AWSViewModel : AbstractViewModel
    {
        private readonly ToolkitContext _toolkitContext;

        AWSViewMetaNode _metaNode;
        ObservableCollection<AccountViewModel> _accounts = new ObservableCollection<AccountViewModel>();
        SettingsWatcher _sdkCredentialWatcher;
        ILog _logger = LogManager.GetLogger(typeof(AWSViewModel));

        public AWSViewModel(
            AWSViewMetaNode metaNode,
            ToolkitContext toolkitContext)
            : base(metaNode, null, "root")
        {
            _metaNode = metaNode;
            _toolkitContext = toolkitContext;

            _toolkitContext.CredentialManager.CredentialManagerUpdated += OnCredentialManagerUpdated;
            SetupCredentialWatchers();
            Refresh();
        }

        public ObservableCollection<AccountViewModel> RegisteredAccounts => this._accounts;

        public override ObservableCollection<IViewModel> Children => new ObservableCollection<IViewModel>();

        public AccountViewModel AccountFromIdentityKey(string identityKey)
        {
            foreach (AccountViewModel account in RegisteredAccounts)
            {
                if (string.Compare(account.SettingsUniqueKey, identityKey, true) == 0)
                    return account;
            }

            return null;
        }

        public AccountViewModel AccountFromCredentialId(string credentialId)
        {
            return RegisteredAccounts.FirstOrDefault(account => string.Equals(account.Identifier.Id, credentialId, StringComparison.OrdinalIgnoreCase));
        }

        public AccountViewModel AccountFromProfileName(string profileName)
        {
            if (string.IsNullOrEmpty(profileName))
                return null;

            foreach (var account in RegisteredAccounts)
            {
                if (account.DisplayName.Equals(profileName, StringComparison.Ordinal))
                    return account;
            }

            return null;
        }

        public void Refresh()
        {
            LoadProfiles();
        }

        public static int CompareViewModel(IViewModel x, IViewModel y)
        {
            return x.Name.CompareTo(y.Name);
        }

        public override void Refresh(bool async)
        {
            this.Refresh();
        }

        public override void LoadDnDObjects(IDataObject dndDataObjects)
        {
        }

        public override bool FailedToLoadChildren => false;

        private void OnCredentialManagerUpdated(object sender, EventArgs e)
        {
            this.Refresh();
        }

        //TODO:This is added as a hack to ensure sdk credential file has a file watcher registered
        private void SetupCredentialWatchers()
        {
            var persistenceManager = PersistenceManager.Instance as PersistenceManager;

            if (persistenceManager == null)
            {
                _logger.Error(
                    "Unable to access PersistenceManager - encrypted accounts may not automatically refresh in the Explorer");
            }
            else
            {
                this._sdkCredentialWatcher =
                    persistenceManager.Watch(ToolkitSettingsConstants.RegisteredProfiles);
                this._sdkCredentialWatcher.SettingsChanged += new EventHandler((o, e) =>
                {
                    Debug.WriteLine("Placeholder call to ensure settings watcher is invoked on file change");
                });
            }
        }

        private void LoadProfiles()
        {
            this._accounts.Clear();
            var allIdentifiers = _toolkitContext.CredentialManager.GetCredentialIdentifiers();
            foreach (var identifier in allIdentifiers)
            {
                var accountViewModel = new AccountViewModel(
                    this._metaNode.FindChild<AccountViewMetaNode>(),
                    this, identifier,
                    _toolkitContext);
                accountViewModel.CreateServiceChildren();

                _accounts.Add(accountViewModel);
            }

            base.NotifyPropertyChanged("RegisteredAccounts");
        }
    }

    internal class AccountViewModelEqualityComparer : IEqualityComparer<AccountViewModel>
    {
        private static AccountViewModelEqualityComparer _instance = new AccountViewModelEqualityComparer();
        public static AccountViewModelEqualityComparer Instance => _instance;

        #region IEqualityComparer<AccountViewModel> Members

        public bool Equals(AccountViewModel x, AccountViewModel y)
        {
            if (x == null || y == null)
                return (x == y);
            return string.Equals(x.SettingsUniqueKey, y.SettingsUniqueKey);
        }

        public int GetHashCode(AccountViewModel obj)
        {
            return (obj == null ? 0 : (obj.SettingsUniqueKey == null ? 0 : obj.SettingsUniqueKey.GetHashCode()));
        }

        #endregion
    }
}
