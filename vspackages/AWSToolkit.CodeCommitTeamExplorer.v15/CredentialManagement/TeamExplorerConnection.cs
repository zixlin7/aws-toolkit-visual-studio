using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CodeCommit.Interface;
using Amazon.AWSToolkit.CodeCommit.Interface.Model;
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

        public delegate void TeamExplorerBindingChanged(TeamExplorerConnection connection);
        public static event TeamExplorerBindingChanged OnTeamExplorerBindingChanged;

        public static IAWSCodeCommit CodeCommitPlugin { get; set; }

        /// <summary>
        /// Gets and sets the single active connection, if any, within Team Explorer.
        /// </summary>
        public static TeamExplorerConnection ActiveConnection
        {
            get
            {
                LOGGER.Info("TeamExplorerConnection: get_ActiveConnection");
                lock (_synclock)
                {
                    return _activeConnection;
                }
            }
            private set
            {
                LOGGER.Info("TeamExplorerConnection: set_ActiveConnection");
                lock (_synclock)
                {
                    _activeConnection = value;
                }
                OnTeamExplorerBindingChanged?.Invoke(value);
            }
        }

        /// <summary>
        /// The AWS account used to 'sign in' in Team Explorer.
        /// </summary>
        public AccountViewModel Account { get; }

        public ObservableCollection<ICodeCommitRepository> Repositories { get; } = new ObservableCollection<ICodeCommitRepository>();

        public void RefreshRepositories()
        {
            LOGGER.Info("TeamExplorerConnection: RefreshRepositories");

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
                ThreadPool.QueueUserWorkItem(QueryNewlyAddedRepositoriesDataAsync, reposToValidate);
            }
        }

        private void QueryNewlyAddedRepositoriesDataAsync(object state)
        {
            LOGGER.Info("TeamExplorerConnection: QueryNewlyAddedRepositoriesDataAsync");
            if (CodeCommitPlugin == null)
                return;


            var repoPaths = state as IEnumerable<string>;
            if (repoPaths == null)
                return;

            var validRepos = CodeCommitPlugin.GetRepositories(Account, repoPaths);
            Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                Repositories.Clear();
                foreach (var repo in validRepos)
                {
                    Repositories.Add(repo);
                }

                OnPropertyChanged("Repositories");
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

        /// <summary>
        /// Removes credentials from the OS using the target name we specified
        /// when they were registered. The target name is a domain url to a CodeCommit
        /// endpoint (not a repository).
        /// </summary>
        /// <param name="credentialsTarget"></param>
        public void DeregisterCredentials(string credentialsTarget)
        {
            if (!_persistedTargets.Contains(credentialsTarget))
            {
                LOGGER.ErrorFormat("Received request to deregister unknown credential target {0}", credentialsTarget);
                return;
            }

            DeleteOSCredentialTarget(credentialsTarget);
            _persistedTargets.Remove(credentialsTarget);

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
                        return new TeamExplorerConnection(account, credentialTargets);

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
            LOGGER.Info("TeamExplorerConnection: static TeamExplorerConnection");
            ToolkitFactory.AddToolkitInitializedDelegate(Initialize);
        }

        private static void Initialize()
        {
            LOGGER.Info("TeamExplorerConnection: Initialize");
            Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

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

        private TeamExplorerConnection(AccountViewModel account)
        {
            Account = account;
            RefreshRepositories();
        }

        private TeamExplorerConnection(AccountViewModel account, IEnumerable<string> credentialTargets)
        {
            Account = account;

            try
            {
                var svcCredentials = account.GetCredentialsForService(ServiceSpecificCredentialStore.CodeCommitServiceName);

                foreach (var target in credentialTargets)
                {
                    _persistedTargets.Add(target);
                    using (var gitCredentials = new GitCredentials(svcCredentials.Username, svcCredentials.Password, target))
                    {
                        gitCredentials.Save();
                    }
                }

                RefreshRepositories();
            }
            catch (Exception e)
            {
                LOGGER.Error(string.Format("Failed to load service credentials for profile {0} or to register credentials with the OS", account.DisplayName), e);
            }
        }

        private static RegistryKey OpenTEGitSourceControlRegistryKey(string path)
        {
#if VS2017
            string TEGitKey;
            if (string.Equals(ToolkitFactory.Instance?.ShellProvider.ShellVersion, "2019"))
            {
                TEGitKey = @"Software\Microsoft\VisualStudio\16.0\TeamFoundation\GitSourceControl";
            }
            else if (string.Equals(ToolkitFactory.Instance?.ShellProvider.ShellVersion, "2017"))
            {
                TEGitKey = @"Software\Microsoft\VisualStudio\15.0\TeamFoundation\GitSourceControl";
            }
            else
            {
                var errorMessage = $"Error unable to determine TeamFoundation\\GitSourceControl for shell {ToolkitFactory.Instance?.ShellProvider.ShellVersion}";
                LOGGER.Error(errorMessage);

// If we are debug mode throw a fatal exception so we know to update this code when adding support for a new shell.
// We don't want to run the risk of crashing VS for the released version so we will just log the error.
#if DEBUG
                throw new Exception(errorMessage);
#else
                return null;
#endif
            }

            LOGGER.Info($"Using regkey {TEGitKey} to look for TeamFoundation\\GitSourceControl for VS {ToolkitFactory.Instance?.ShellProvider.ShellVersion}");

#elif VS2015
            const string TEGitKey = @"Software\Microsoft\VisualStudio\14.0\TeamFoundation\GitSourceControl";
#else
#error "No VS20xx conditional defined in package.build.targets - cannot set Team Explorer registry key in TeamExplorerConnection::RefreshRepositories()."
#endif

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
