using System;
using System.IO;
using Amazon.AWSToolkit.Util;
using Amazon.Runtime.Internal.Settings;
using log4net;

namespace Amazon.AWSToolkit.Settings
{
    // Raises events when toolkit settings file(s) change
    public class ToolkitSettingsWatcher : IToolkitSettingsWatcher, IDisposable
    {
        internal static ILog LOGGER = LogManager.GetLogger(typeof(ToolkitSettingsWatcher));
        private const double FileChangeDebounceIntervalMs = 333;

        public event EventHandler SettingsChanged;

        private bool _disposed = false;
        private FileSystemWatcher _fileWatcher;
        private readonly DebounceDispatcher _settingsChangedDispatcher;

        public ToolkitSettingsWatcher()
        {
            _settingsChangedDispatcher = new DebounceDispatcher();
            var settingsFolder = Path.GetFullPath(PersistenceManager.GetSettingsStoreFolder());
            // Toolkit settings are stored in various json files in settingsFolder
            _fileWatcher = new FileSystemWatcher(settingsFolder, "*.json")
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = false,
            };

            _fileWatcher.Changed += OnFileWatcherChanged;
            _fileWatcher.Created += OnFileWatcherChanged;
            _fileWatcher.Deleted += OnFileWatcherChanged;
            _fileWatcher.Renamed += OnFileWatcherChanged;
            _fileWatcher.Error += OnFileWatcherError;
        }

        private void OnFileWatcherError(object sender, ErrorEventArgs e)
        {
            LOGGER.Error(e.GetException());
        }

        private void OnFileWatcherChanged(object sender, FileSystemEventArgs e)
        {
            // Debounce: Each file modification can trigger more than one change event (virus scanners?)
            _settingsChangedDispatcher.Debounce(FileChangeDebounceIntervalMs, _ => { OnSettingsChanged(); });
        }

        public void Dispose()
        {
            try
            {
                if (_disposed)
                {
                    return;
                }

                if (_fileWatcher != null)
                {
                    _fileWatcher.Changed -= OnFileWatcherChanged;
                    _fileWatcher.Created -= OnFileWatcherChanged;
                    _fileWatcher.Deleted -= OnFileWatcherChanged;
                    _fileWatcher.Renamed -= OnFileWatcherChanged;
                    _fileWatcher.Error -= OnFileWatcherError;

                    _fileWatcher.Dispose();

                    _fileWatcher = null;
                }

                _settingsChangedDispatcher.Dispose();
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

        private void OnSettingsChanged()
        {
            SettingsChanged?.Invoke(this, new EventArgs());
        }
    }
}
