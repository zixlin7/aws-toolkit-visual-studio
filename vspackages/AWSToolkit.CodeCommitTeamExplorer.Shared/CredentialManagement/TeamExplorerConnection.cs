using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CodeCommit.Interface;
using Amazon.AWSToolkit.CodeCommit.Interface.Model;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.State;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Settings;
using Amazon.AWSToolkit.Util;
using Amazon.Runtime.Internal.Settings;
using log4net;
using Microsoft.Win32;
using ThirdParty.Json.LitJson;

namespace Amazon.AWSToolkit.CodeCommitTeamExplorer.CredentialManagement
{
    /// <summary>
    /// Represents the active signed-in account, and any git credential targets
    /// registered with the OS, within Team Explorer. Details of the signed-in
    /// account are persisted across IDE invocations.
    /// </summary>
    public class TeamExplorerConnection : INotifyPropertyChanged
    {
        #region Private data

        private const string connectedProfile = "ConnectedProfile";
        private const string targetsPropertyName = "CredentialTargets";
        private const string settingsFileFolderName = "teamexplorer";
        private const string settingsFileName = "connections.json";

        private static readonly object _synclock = new object();

        private static readonly ILog LOGGER = LogManager.GetLogger(typeof(TeamExplorerConnection));

        private static TeamExplorerConnection _activeConnection;

        private readonly HashSet<string> _persistedTargets = new HashSet<string>();

        #endregion

        public delegate void TeamExplorerBindingChanged(TeamExplorerConnection oldConnection, TeamExplorerConnection newConnection);
        public static event TeamExplorerBindingChanged OnTeamExplorerBindingChanged;

        public static IAWSCodeCommit CodeCommitPlugin { get; private set; }
        private static ToolkitContext _toolkitContext;
        private static IAwsConnectionManager _teamExplorerAwsConnectionManager;
        private static ToolkitRegion _fallbackRegion;

        /// <summary>
        /// Gets and sets the single active connection, if any, within Team Explorer.
        /// </summary>
        public static TeamExplorerConnection ActiveConnection
        {
            get
            {
                // Don't log this getter to avoid log spam from this getter.
                // It is frequently queried in RelayCommand CanExecute calls.
                lock (_synclock)
                {
                    return _activeConnection;
                }
            }
            private set
            {
                LOGGER.Debug("TeamExplorerConnection: set_ActiveConnection");
                TeamExplorerConnection oldConnection = null;
                lock (_synclock)
                {
                    oldConnection = _activeConnection;
                    _activeConnection = value;
                }
                OnTeamExplorerBindingChanged?.Invoke(oldConnection, value);
                _teamExplorerAwsConnectionManager.ChangeConnectionSettings(value?.Account?.Identifier, value?.Account?.Region ?? _fallbackRegion);
            }
        }

        public void RevalidateConnection()
        {
            _teamExplorerAwsConnectionManager.RefreshConnectionState();
        }

        /// <summary>
        /// The AWS account used to 'sign in' in Team Explorer.
        /// </summary>
        public AccountViewModel Account { get; }

        public ObservableCollection<ICodeCommitRepository> Repositories { get; } = new ObservableCollection<ICodeCommitRepository>();

        private ConnectionState _awsConnectionState;

        public ConnectionState AwsConnectionState
        {
            get => _awsConnectionState;
            set
            {
                if (_awsConnectionState != value)
                {
                    _awsConnectionState = value;
                    OnPropertyChanged(nameof(AwsConnectionState));
                    OnPropertyChanged(nameof(IsAccountValid));
                    OnPropertyChanged(nameof(IsValidatingAccount));
                    OnPropertyChanged(nameof(AccountValidationMessage));
                }
            }
        }

        public bool IsAccountValid => AwsConnectionState is ConnectionState.ValidConnection;
        public bool IsValidatingAccount => AwsConnectionState is ConnectionState.ValidatingConnection;
        public string AccountValidationMessage => AwsConnectionState?.Message ?? string.Empty;
        public string AccountId => _teamExplorerAwsConnectionManager?.ActiveAccountId;

        public void RefreshRepositories()
        {
            LOGGER.Debug("TeamExplorerConnection: RefreshRepositories");

            if (CodeCommitPlugin == null)
            {
                LOGGER.Warn("Skipping loading refresh repositories because the main toolkit instance hasn't initialized yet.");
                return;
            }

            var reposToValidate = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            RegistryKey regkey = null;
            try
            {
                regkey = OpenTEGitSourceControlRegistryKey("Repositories");
                if (regkey != null)
                {
                    var subkeyNames = regkey.GetSubKeyNames();
                    foreach (var subkeyName in subkeyNames)
                    {
                        RegistryKey subkey = null;
                        try
                        {
                            subkey = regkey.OpenSubKey(subkeyName);
                            if (subkey != null)
                            {
                                try
                                {
                                    var path = subkey.GetValue("Path") as string;
                                    if (CodeCommitPlugin != null && CodeCommitPlugin.IsCodeCommitRepository(path))
                                    {
                                        if (path != null && Directory.Exists(path))
                                        {
                                            reposToValidate.Add(path);
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    LOGGER.Error("Error processing repo at registry " + subkey.Name, e);
                                }
                            }
                        }
                        catch
                        {
                        }
                        finally
                        {
                            subkey?.Dispose();
                        }
                    }
                }
            }
            finally
            {
                regkey?.Dispose();
            }

            if (reposToValidate.Any())
            {
                // as this probing could take some time, spin up a thread to add the new
                // repos into the collection
                ThreadPool.QueueUserWorkItem(
                    async state => { await QueryNewlyAddedRepositoriesDataAsync(state); },
                    reposToValidate
                );
            }
        }

        private async Task QueryNewlyAddedRepositoriesDataAsync(object state)
        {
            LOGGER.Debug("TeamExplorerConnection: QueryNewlyAddedRepositoriesDataAsync");
            if (CodeCommitPlugin == null)
                return;

            var repoPaths = state as IEnumerable<string>;
            if (repoPaths == null)
                return;

            var validRepos = new List<ICodeCommitRepository>();

            if (IsAccountValid)
            {
                validRepos.AddRange(await CodeCommitPlugin.GetRepositories(Account, repoPaths));
            }

            Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                Repositories.Clear();
                foreach (var repo in validRepos)
                {
                    Repositories.Add(repo);
                }

                OnPropertyChanged(nameof(Repositories));
            });
        }

        /// <summary>
        /// Returns the credential targets currently registered with the OS for
        /// this connection.
        /// </summary>
        public IEnumerable<string> OsCredentialTargets => _persistedTargets.ToArray();

        /// <summary>
        /// Registers a set of git credentials with the OS, and updates
        /// the persisted targets in the backing file so we know to clean up
        /// when the user signs out.
        /// </summary>
        /// <param name="credentials"></param>
        public void RegisterCredentials(GitCredentials credentials)
        {
            // always update the OS in case the password has been changed
            credentials.Save();

            if (_persistedTargets.Contains(credentials.Target))
                return;

            _persistedTargets.Add(credentials.Target);
            SaveConnectionData(true);
        }

        public static void Signin(AccountViewModel account)
        {
            ActiveConnection = new TeamExplorerConnection(account);
            ActiveConnection.SaveConnectionData(true);
        }

        public void Signout()
        {
            ClearAllTargets();
            SaveConnectionData(false);

            Repositories.Clear();
            OnPropertyChanged("Repositories");

            ActiveConnection = null;
        }

        private void ClearAllTargets()
        {
            foreach (var t in _persistedTargets)
            {
                DeleteOSCredentialTarget(t);
            }

            _persistedTargets.Clear();
        }

        private static void DeleteOSCredentialTarget(string target)
        {
            if (!GitCredentials.Delete(target, GitCredentials.CredentialType.Generic))
            {
                LOGGER.ErrorFormat("Attempt to clear credential target {0} failed.", target);
            }
        }

        /// <summary>
        /// Serializes the set of persisted targets to disk.
        /// </summary>
        /// <param name="signedIn">
        /// False if we're saving due to the user signing out; this stops us persisting the active 
        /// profile name (which we won't have cleared at the time we call this method).
        /// </param>
        private void SaveConnectionData(bool signedIn)
        {
            var data = ToJson(signedIn);
            SaveConnectionData(data);
        }

        private static void SaveConnectionData(string jsonData)
        {
            var storefile = GetSettingsFilePath();
            using (var writer = new StreamWriter(storefile))
            {
                writer.Write(jsonData);
            }
        }

        /// <summary>
        /// Serializes the connection data for storage in the settings file.
        /// </summary>
        /// <param name="signedIn">
        /// False if we're saving due to the user signing out; this stops us persisting the active 
        /// profile name (which we won't have cleared at the time we call this method).
        /// </param>
        /// <returns></returns>
        private string ToJson(bool signedIn)
        {
            var connectionData = new Dictionary<string, object>
            {
                {connectedProfile, signedIn ? Account.DisplayName : string.Empty}
            };

            if (signedIn)
            {
                var credentialTargets = new List<string>();
                foreach (var t in _persistedTargets)
                {
                    credentialTargets.Add(t);
                }
                connectionData.Add(targetsPropertyName, credentialTargets);
            }

            var jsonWriter = new JsonWriter { PrettyPrint = true };
            JsonMapper.ToJson(connectionData, jsonWriter);
            return jsonWriter.ToString();
        }

        /// <summary>
        /// Deserializes a connection, if any, from the storage file.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        private static TeamExplorerConnection FromJson(string json)
        {
            try
            {
                var jsonReader = new JsonReader(json);
                var connectionData = JsonMapper.ToObject(jsonReader);

                var profileName = (string)connectionData[connectedProfile];

                if (!string.IsNullOrEmpty(profileName))
                {
                    var credentialTargets = new List<string>();
                    var account = ToolkitFactory.Instance.RootViewModel.AccountFromProfileName(profileName);
                    var credentialTargetValues = connectionData[targetsPropertyName];
                    if (credentialTargetValues != null && credentialTargetValues.IsArray)
                    {
                        foreach (var target in credentialTargetValues)
                        {
                            credentialTargets.Add(target.ToString());
                        }                        
                    }

                    if (account != null)
                    {
                        if (_toolkitContext.CredentialManager.IsLoginRequired(account.Identifier))
                        {
                            // At this time, don't support automatic (or deferred) connection of
                            // credentials requiring a login. This avoids a login prompt on IDE startup.
                            return null;
                        }

                        return new TeamExplorerConnection(account, credentialTargets);
                    }

                    // assume something went wrong in a previous run and we have stale data,
                    // and potentially some credentials left in the OS - clear them out
                    LOGGER.Info("Detected stale data in connections.json file; cleaning out any persisted credential targets");
                    foreach (var target in credentialTargets)
                    {
                        DeleteOSCredentialTarget(target);
                    }

                    File.Delete(GetSettingsFilePath());
                }
            }
            catch (Exception e)
            {
                LOGGER.ErrorFormat("Exception parsing connection data", e);
            }

            return null;
        }

        private static string GetSettingsFilePath()
        {
            var settingsFolder = PersistenceManager.GetSettingsStoreFolder();
            var location = Path.Combine(settingsFolder, settingsFileFolderName);

            if (!Directory.Exists(location))
                Directory.CreateDirectory(location);

            return Path.Combine(location, settingsFileName);
        }

        static TeamExplorerConnection()
        {
            LOGGER.Debug("TeamExplorerConnection: static TeamExplorerConnection");
            ToolkitFactory.AddToolkitInitializedDelegate(Initialize);
        }

        private static void Initialize()
        {
            LOGGER.Debug("TeamExplorerConnection: Initialize");
            Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                _toolkitContext = ToolkitFactory.Instance.ToolkitContext;
                if (_toolkitContext == null)
                {
                    LOGGER.Error("TeamExplorerConnection - ToolkitContext not available at load time");
                }
                else
                {
                    _teamExplorerAwsConnectionManager = new AwsConnectionManager(
                        AwsConnectionManager.DefaultStsClientCreator,
                        _toolkitContext.CredentialManager,
                        _toolkitContext.TelemetryLogger,
                        _toolkitContext.RegionProvider,
                        new AppDataToolkitSettingsRepository()
                    );

                    _fallbackRegion = _toolkitContext.RegionProvider.GetRegion(RegionEndpoint.USEast1.DisplayName);
                    _teamExplorerAwsConnectionManager.ConnectionStateChanged += TeamExplorerAwsConnectionManager_ConnectionStateChanged;
                }

                CodeCommitPlugin = ToolkitFactory.Instance.QueryPluginService(typeof(IAWSCodeCommit)) as IAWSCodeCommit;
                if (CodeCommitPlugin == null)
                {
                    LOGGER.Error("TeamExplorerConnection - CodeCommit plugin not available at load time");
                }

                var storeFile = GetSettingsFilePath();
                if (!File.Exists(storeFile))
                    return;

                var data = File.ReadAllText(storeFile);
                ActiveConnection = FromJson(data);
            });
        }

        /// <summary>
        /// Fired when the Team Explorer is associated with a different set of Credentials
        /// </summary>
        private static void TeamExplorerAwsConnectionManager_ConnectionStateChanged(object sender, ConnectionStateChangeArgs e)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var connection = ActiveConnection;
                if (connection != null)
                {
                    connection.AwsConnectionState = e.State;

                    connection.RefreshRepositories();
                }
            });
        }

        private TeamExplorerConnection(AccountViewModel account) : this(account, null)
        {
        }

        private TeamExplorerConnection(AccountViewModel account, IEnumerable<string> credentialTargets)
        {
            Account = account;

            try
            {
                if (credentialTargets != null)
                {
                    var svcCredentials =
                        account.GetCredentialsForService(ServiceSpecificCredentialStore.CodeCommitServiceName);

                    foreach (var target in credentialTargets)
                    {
                        _persistedTargets.Add(target);
                        using (var gitCredentials =
                            new GitCredentials(svcCredentials.Username, svcCredentials.Password, target))
                        {
                            gitCredentials.Save();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LOGGER.Error(string.Format("Failed to load service credentials for profile {0} or to register credentials with the OS", account.DisplayName), e);
            }
        }

        private static RegistryKey OpenTEGitSourceControlRegistryKey(string path)
        {
            string TEGitKey;
            if (string.Equals(ToolkitFactory.Instance?.ShellProvider.HostInfo.Version, ToolkitHosts.Vs2022.Version))
            {
                TEGitKey = @"Software\Microsoft\VisualStudio\17.0\TeamFoundation\GitSourceControl";
            }
            else if (string.Equals(ToolkitFactory.Instance?.ShellProvider.HostInfo.Version, ToolkitHosts.Vs2019.Version))
            {
                TEGitKey = @"Software\Microsoft\VisualStudio\16.0\TeamFoundation\GitSourceControl";
            }
            else if (string.Equals(ToolkitFactory.Instance?.ShellProvider.HostInfo.Version, ToolkitHosts.Vs2017.Version))
            {
                TEGitKey = @"Software\Microsoft\VisualStudio\15.0\TeamFoundation\GitSourceControl";
            }
            else
            {
                var errorMessage = $"Error unable to determine TeamFoundation\\GitSourceControl for shell {ToolkitFactory.Instance?.ShellProvider.HostInfo.Version}";
                LOGGER.Error(errorMessage);

// If we are debug mode throw a fatal exception so we know to update this code when adding support for a new shell.
// We don't want to run the risk of crashing VS for the released version so we will just log the error.
#if DEBUG
                throw new Exception(errorMessage);
#else
                return null;
#endif
            }

            LOGGER.Info($"Using regkey {TEGitKey} to look for TeamFoundation\\GitSourceControl for VS {ToolkitFactory.Instance?.ShellProvider.HostInfo.Version}");

            try
            {
                return Registry.CurrentUser.OpenSubKey(TEGitKey + "\\" + path, false);
            }
            catch (Exception e)
            {
                LOGGER.Error("Failed to open Team Explorer registry key " + TEGitKey, e);
            }

            return null;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
