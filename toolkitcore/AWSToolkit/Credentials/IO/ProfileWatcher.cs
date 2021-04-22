using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon.AWSToolkit.Util;
using Amazon.Runtime.CredentialManagement;
using log4net;

namespace Amazon.AWSToolkit.Credentials.IO
{
    /// <summary>
    /// Watches the shared credential files for any changes 
    /// </summary>
    public class ProfileWatcher: IDisposable
    {
        private const double FileChangeDebounceInterval = 300;
        private readonly Dictionary<string, FileSystemWatcher> _sharedCredentialWatchers =
            new Dictionary<string, FileSystemWatcher>(StringComparer.InvariantCultureIgnoreCase);

        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ProfileWatcher));
        
        private readonly DebounceDispatcher _credentialsChangedDispatcher;
        private bool _disposed;
        public event EventHandler<EventArgs> Changed;

        public ProfileWatcher(List<string> credentialFilePaths)
        {
            _credentialsChangedDispatcher = new DebounceDispatcher();
            InitializeWatchers(credentialFilePaths);
        }

        private void InitializeWatchers(List<string> credentialFilePaths)
        {
            foreach (var credentialPath in credentialFilePaths)
            {
                InitializeFileWatcher(credentialPath);
            }
        }

        private void InitializeFileWatcher(string credentialPath)
        {
            try
            {
                var directoryName = Path.GetDirectoryName(credentialPath);
                var fileName = Path.GetFileName(credentialPath);

                if (string.IsNullOrWhiteSpace(directoryName) ||
                    string.IsNullOrWhiteSpace(fileName))
                {
                    return;
                }

                //if directory does not exist yet, create it so that it can be watched as the toolkit is running
                if (!Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                    LOGGER.Debug($"Creating directory for watching: {directoryName}");
                }

                if (this._sharedCredentialWatchers.ContainsKey(credentialPath))
                {
                    return;
                }

                var watcher = new FileSystemWatcher(directoryName, fileName)
                {
                    NotifyFilter = NotifyFilters.LastWrite |
                                   NotifyFilters.CreationTime |
                                   NotifyFilters.FileName |
                                   NotifyFilters.LastAccess | NotifyFilters.Size
                };
                watcher.Changed += OnFileWatcherChanged;
                watcher.Created += OnFileWatcherChanged;
                watcher.Renamed += OnFileWatcherChanged;
                watcher.Deleted += OnFileWatcherChanged;
                watcher.EnableRaisingEvents = true;

                this._sharedCredentialWatchers[credentialPath] = watcher;
            }
            catch (Exception ex)
            {
                LOGGER.Error($"Error initializing file watcher for: {credentialPath}", ex);
            }
        }

        private void OnFileWatcherChanged(object sender, FileSystemEventArgs e)
        {
            _credentialsChangedDispatcher.Debounce(FileChangeDebounceInterval, _ => { RaiseFileChanged(); });
        }

        private void RaiseFileChanged()
        {
            Changed?.Invoke(this, new EventArgs());
        }

        public void Dispose()
        {
            try
            {
                if (_disposed)
                {
                    return;
                }

                foreach (var watcher in _sharedCredentialWatchers)
                {

                    if (watcher.Value != null)
                    {
                        watcher.Value.Changed -= OnFileWatcherChanged;
                        watcher.Value.Created -= OnFileWatcherChanged;
                        watcher.Value.Deleted -= OnFileWatcherChanged;
                        watcher.Value.Renamed -= OnFileWatcherChanged;

                        watcher.Value.Dispose();

                    }

                    _credentialsChangedDispatcher.Dispose();
                }

                _sharedCredentialWatchers.Clear();
            }
            catch (Exception e)
            {
                LOGGER.Error(e);
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}
