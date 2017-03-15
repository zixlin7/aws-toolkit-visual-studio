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
using Amazon.Runtime.CredentialManagement.Internal;

namespace Amazon.AWSToolkit.Navigator.Node
{
    public class AWSViewModel : AbstractViewModel
    {
        Dispatcher _dispatcher;
        AWSViewMetaNode _metaNode;
        ObservableCollection<AccountViewModel> _accounts = new ObservableCollection<AccountViewModel>();
        SettingsWatcher _sdkCredentialWatcher;
        FileSystemWatcher _sharedCredentialWatcher;

        ILog _logger = LogManager.GetLogger(typeof(AWSViewModel));

        public AWSViewModel(Dispatcher dispatcher, AWSViewMetaNode metaNode)
            : base(metaNode, null, "root")
        {
            this._dispatcher = dispatcher;
            this._metaNode = metaNode;

            SetupCredentialWatchers();

            Refresh();
        }

        private void SetupCredentialWatchers()
        {
            this._sdkCredentialWatcher = PersistenceManager.Instance.Watch(ToolkitSettingsConstants.RegisteredProfiles);
            this._sdkCredentialWatcher.SettingsChanged += new EventHandler((o, e) =>
            {
                this._dispatcher.Invoke((System.Windows.Forms.MethodInvoker) delegate()
                {
                    this.Refresh();
                });
            });

            SetupSharedCredentialFileMonitoring();
        }

        /// <summary>
        /// If the shared credential file path exists, sets up a file monitor if 
        /// we have not already done so. If we had a monitor, but the path no longer
        /// exists we leave the existing monitor alone.
        /// </summary>
        /// <remarks>
        /// Factored into method so that in future, if the toolkit supports writing
        /// new profiles to the shared credential file, we can enable monitoring
        /// if we did not do so on initialization due to the path not existing.
        /// </remarks>
        internal void SetupSharedCredentialFileMonitoring()
        {
            if (this._sharedCredentialWatcher != null)
                return;

            var sharedStore = new SharedCredentialsFile();

            var sharedCredentialPath = Path.GetDirectoryName(sharedStore.FilePath);
            if (!Directory.Exists(sharedCredentialPath))
                return;

            this._sharedCredentialWatcher = new FileSystemWatcher(sharedCredentialPath, Path.GetFileName(sharedStore.FilePath));

            Action<object, FileSystemEventArgs> callback = (o, e) => 
                {
                    this._dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate ()
                    {
                        this.Refresh();
                    });
                };
            this._sharedCredentialWatcher.Changed += new FileSystemEventHandler(callback);
            this._sharedCredentialWatcher.Created += new FileSystemEventHandler(callback);
            this._sharedCredentialWatcher.Renamed += new RenamedEventHandler(callback);

            this._sharedCredentialWatcher.EnableRaisingEvents = true;
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

            HashSet<string> netSDKStoreAccessKeys = ProcessStore(updatedAccounts, new HashSet<string>(), new NetSDKCredentialsFile());
            ProcessStore(updatedAccounts, netSDKStoreAccessKeys, new SharedCredentialsFile());

            var deleted = _accounts.Except(updatedAccounts, AccountViewModelEqualityComparer.Instance).ToList();
            foreach (var del in deleted)
                _accounts.Remove(del);

            var added = updatedAccounts.Except(_accounts, AccountViewModelEqualityComparer.Instance).ToList();
            foreach (var add in added)
                _accounts.Add(add);

            base.NotifyPropertyChanged("RegisteredAccounts");
        }

        private HashSet<string> ProcessStore(List<AccountViewModel> accounts, HashSet<string> alreadyAddedAccessKeys, ICredentialProfileStore store)
        {
            HashSet<string> accessKeys = new HashSet<string>();
            foreach (var profile in store.ListProfiles())
            {
                // Toolkit doesn't currently support profiles requiring callbacks for MFA tokens
                if (CredentialProfileUtils.IsCallbackRequired(profile))
                    continue;

                AWSCredentials credentials;
                if (AWSCredentialsFactory.TryGetAWSCredentials(profile, store, out credentials))
                {
                    ImmutableCredentials immutableCredentials = null;
                    try
                    {
                        immutableCredentials = credentials.GetCredentials();
                    }
                    catch (Exception e)
                    {
                        _logger.Warn($"Skipping adding profile {profile.Name} because getting credentials failed.", e);
                        continue;
                    }

                    // Cache access key to make sure we don't add the same account from the shared file.
                    if (CredentialProfileUtils.GetProfileType(profile) == CredentialProfileType.Basic && !accessKeys.Contains(immutableCredentials.AccessKey))
                    {
                        accessKeys.Add(immutableCredentials.AccessKey);
                    }

                    // Don't add account if it was added by the NET SDK Credential store
                    if (CredentialProfileUtils.GetProfileType(profile) != CredentialProfileType.Basic ||
                        !alreadyAddedAccessKeys.Contains(immutableCredentials.AccessKey))
                    {
                        accounts.Add(new AccountViewModel(this._metaNode.FindChild<AccountViewMetaNode>(), this, store, profile));
                    }
                }
            }

            return accessKeys;
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
