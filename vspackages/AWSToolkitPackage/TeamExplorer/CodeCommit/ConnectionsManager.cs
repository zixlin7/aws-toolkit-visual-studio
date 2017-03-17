using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Account;

namespace Amazon.AWSToolkit.VisualStudio.TeamExplorer.CodeCommit
{
    /// <summary>
    /// Represents the set of 'open' connections to CodeCommit. A 
    /// connection is an AWS credential profile that was selected 
    /// in the 'Connect' dialog launched from our Invitation panel
    /// in Team Explorer.
    /// </summary>
    /// <remarks>
    /// Team Explorer seems to revolve around one connection per provider
    /// at a time thus we mimic the GitHub flow and to change AWS credentials 
    /// requires the user to 'sign out' and then go through the sign in dialog 
    /// again, selecting a different credential profile. The credentials 
    /// in use here are regular AWS ones, not the service specific credentials.
    /// If the user has not associated a set of service specific credentials
    /// with the profile at the time we need to perform git operations on
    /// a repo (clone), we will prompt them.
    /// </remarks>
    internal class ConnectionsManager : INotifyCollectionChanged
    {
        private static readonly object _synclock = new object();
        private static ConnectionsManager _instance;

        // for now we'll store the unique id of the profile
        private ObservableCollection<string> ProfileConnections { get; }

        // the account Team Explorer is currently bound to
        private AccountViewModel _teamExplorerAccount;

        public delegate void TeamExplorerBindingChanged(AccountViewModel boundAccount);

        public event TeamExplorerBindingChanged OnTeamExplorerBindingChanged;

        public static ConnectionsManager Instance
        {
            get
            {
                lock (_synclock)
                {
                    if (_instance == null)
                        _instance = new ConnectionsManager();
                }

                return _instance;
            }
        }

        /// <summary>
        /// The AWS account connected through Team Explorer, which has the notion of
        /// a single active account per provider.
        /// </summary>
        public AccountViewModel TeamExplorerAccount
        {
            get
            {
                lock (_synclock)
                {
                    return _teamExplorerAccount;
                }
            }
            private set
            {
                lock (_synclock)
                {
                    _teamExplorerAccount = value;
                    OnTeamExplorerBindingChanged?.Invoke(_teamExplorerAccount);
                }
            }
        }

        /// <summary>
        /// Registers that an account has opened a connection to CodeCommit somewhere
        /// in the IDE.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="isActiveTeamExplorerConnection">
        /// True if this connection is being registered as the active account for Team Explorer
        /// integration, false if this is just a registration from the AWS Explorer.
        /// </param>
        public void RegisterProfileConnection(AccountViewModel account, bool isActiveTeamExplorerConnection)
        {
            lock (_synclock)
            {
                var alreadyRegistered = false;
                foreach (var p in ProfileConnections)
                {
                    if (p.Equals(account.UniqueIdentifier, StringComparison.OrdinalIgnoreCase))
                    {
                        alreadyRegistered = true;
                        break;
                    }
                }

                if (alreadyRegistered)
                {
                    // todo: log we're ignoring it
                }

                if (isActiveTeamExplorerConnection)
                {
                    TeamExplorerAccount = account;
                }

                ProfileConnections.Add(account.UniqueIdentifier);
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, account.UniqueIdentifier));
            }
        }

        /// <summary>
        /// Removes a connected profile.
        /// </summary>
        /// <param name="account"></param>
        public void DeregisterProfileConnection(AccountViewModel account)
        {
            lock (_synclock)
            {
                for (var i = 0; i < ProfileConnections.Count; i++)
                {
                    if (ProfileConnections[i].Equals(account.UniqueIdentifier, StringComparison.OrdinalIgnoreCase))
                    {
                        if (TeamExplorerAccount != null && TeamExplorerAccount.UniqueIdentifier.Equals(account.UniqueIdentifier, StringComparison.OrdinalIgnoreCase))
                        {
                            TeamExplorerAccount = null;
                        }

                        ProfileConnections.RemoveAt(i);
                        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, account.UniqueIdentifier));
                        return;
                    }
                }

                // todo: log or error that we didn't find the account registered
            }
        }

        /// <summary>
        /// Returns the number of currently registered profiles.
        /// </summary>
        public int Count
        {
            get { return ProfileConnections.Count; }
        }

        /// <summary>
        /// Event for interested parties to be notified when the collection of
        /// connections is updated.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private ConnectionsManager()
        {
            ProfileConnections = new ObservableCollection<string>();
        }
    }
}
