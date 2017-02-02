using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;

using Amazon.Runtime;
using Amazon.Runtime.Internal.Settings;

using Amazon.AWSToolkit.Account;
using log4net;
using Amazon.Util;
using Amazon.Runtime.CredentialManagement;

namespace Amazon.AWSToolkit.Navigator.Node
{
    public class AWSViewModel : AbstractViewModel
    {
        Dispatcher _dispatcher;
        AWSViewMetaNode _metaNode;
        ObservableCollection<AccountViewModel> _accounts = new ObservableCollection<AccountViewModel>();
        SettingsWatcher _watcher;
        ILog _logger = LogManager.GetLogger(typeof(AWSViewModel));

        public AWSViewModel(Dispatcher dispatcher, AWSViewMetaNode metaNode)
            : base(metaNode, null, "root")
        {
            this._dispatcher = dispatcher;
            this._metaNode = metaNode;
            this._watcher = PersistenceManager.Instance.Watch(ToolkitSettingsConstants.RegisteredProfiles);
            this._watcher.SettingsChanged += new EventHandler((o, e) =>
            {
                this._dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate()
                {
                    this.Refresh();
                });
            });

            Refresh();
        }

        public ObservableCollection<AccountViewModel> RegisteredAccounts
        {
            get
            {
                return this._accounts;
            }
        }

        public override ObservableCollection<IViewModel> Children
        {
            get
            {
                return new ObservableCollection<IViewModel>();
            }
        }

        public AccountViewModel AccountFromIdentityKey(string identityKey)
        {
            foreach (AccountViewModel account in RegisteredAccounts)
            {
                if (string.Compare(account.SettingsUniqueKey, identityKey, true) == 0)
                    return account;
            }

            return null;
        }

        public AccountViewModel AccountFromAccountNumber(string accountNumber)
        {
            if (string.IsNullOrEmpty(accountNumber))
                return null;

            accountNumber = accountNumber.Replace("-", null);
            foreach (AccountViewModel account in RegisteredAccounts)
            {
                if (account.AccountNumber != null && string.Compare(account.AccountNumber.Replace("-", null), accountNumber, true) == 0)
                    return account;
            }

            return null;
        }

        private void LoadRegisteredProfiles()
        {
            List<AccountViewModel> updatedAccounts = new List<AccountViewModel>();

            HashSet<string> netSDKStoreAccessKeys = new HashSet<string>();

            var netStore = new NetSDKCredentialsFile();
            foreach (var profile in netStore.ListProfiles())
            {
                AWSCredentials credentials;
                if (AWSCredentialsFactory.TryGetAWSCredentials(profile, netStore, out credentials))
                {
                    // Cache access key to make sure we don't add the same account from the shared file.
                    netSDKStoreAccessKeys.Add(credentials.GetCredentials().AccessKey);
                    updatedAccounts.Add(new AccountViewModel(this._metaNode.FindChild<AccountViewMetaNode>(), this, netStore, profile));
                }
            }

            var sharedStore = new SharedCredentialsFile();
            foreach (var profile in sharedStore.ListProfiles())
            {
                AWSCredentials credentials;
                if (AWSCredentialsFactory.TryGetAWSCredentials(profile, netStore, out credentials))
                {
                    // Don't add account if it was added by the NET SDK Credential store
                    if (!netSDKStoreAccessKeys.Contains(credentials.GetCredentials().AccessKey))
                    {
                        updatedAccounts.Add(new AccountViewModel(this._metaNode.FindChild<AccountViewMetaNode>(), this, sharedStore, profile));
                    }
                }
            }

            var deleted = _accounts.Except(updatedAccounts, AccountViewModelEqualityComparer.Instance).ToList();
            foreach (var del in deleted)
                _accounts.Remove(del);

            var added = updatedAccounts.Except(_accounts, AccountViewModelEqualityComparer.Instance).ToList();
            foreach (var add in added)
                _accounts.Add(add);
        }

        public void Refresh()
        {
            LoadRegisteredProfiles();
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

        public override bool FailedToLoadChildren
        {
            get
            {
                return false;
            }
        }
    }

    internal class AccountViewModelEqualityComparer : IEqualityComparer<AccountViewModel>
    {
        private static AccountViewModelEqualityComparer _instance = new AccountViewModelEqualityComparer();
        public static AccountViewModelEqualityComparer Instance { get { return _instance; } }

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
