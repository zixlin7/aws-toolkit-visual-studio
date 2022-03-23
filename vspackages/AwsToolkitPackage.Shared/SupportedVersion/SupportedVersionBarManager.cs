using System;
using System.Timers;

using Amazon.AWSToolkit.VisualStudio.Utilities;

using log4net;

using Microsoft.VisualStudio.Shell;

namespace Amazon.AWSToolkit.VisualStudio.SupportedVersion
{
    /// <summary>
    /// Orchestrates the life cycle of a Minimum Version Supported InfoBar
    /// </summary>
    public class SupportedVersionBarManager : IDisposable
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(SupportedVersionBarManager));
        private const int SetupRetryIntervalMs = 3000;

        private bool _disposed = false;

        private readonly IServiceProvider _serviceProvider;
        private readonly Timer _timer;
        private readonly SupportedVersionStrategy _supportedVersionStrategy;
        
        public SupportedVersionBarManager(IServiceProvider serviceProvider, SupportedVersionStrategy supportedVersionStrategy)
        {
            _serviceProvider = serviceProvider;
            _supportedVersionStrategy = supportedVersionStrategy;
            _timer = new Timer()
            {
                AutoReset = false,
                Interval = SetupRetryIntervalMs,
            };
            _timer.Elapsed += TimerOnElapsed;
        }

        /// <summary>
        /// Responsible for displaying the InfoBar.
        /// Display is performed on a timer, so that attempts
        /// can be performed until it succeeds. (In VS 2019, this
        /// may run before the main window shows, because 2019
        /// shows a project selection dialog first by default)
        /// </summary>
        public void ShowInfoBar()
        {
            _timer.Start();
        }

        public void Dispose()
        {
            try
            {
                if (_disposed)
                {
                    return;
                }

                _timer.Stop();
                _timer.Elapsed -= TimerOnElapsed;
                _timer.Dispose();
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            finally
            {
                _disposed = true;
            }
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            if (ToolkitFactory.Instance?.ShellProvider == null)
            {
                _timer.Start();
                return;
            }

            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread(() =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                try
                {
                    AddSupportedVersionInfoBarToMainWindow();
                }
                catch (Exception exception)
                {
                    _timer.Start();
                    Logger.Error(exception);
                }
            });
        }

        private void AddSupportedVersionInfoBarToMainWindow()
        {
            var hostDeprecated = _supportedVersionStrategy.HostDeprecated;
            Logger.Debug($"Trying to show minimum supported version banner for {hostDeprecated.Version}");
            ThreadHelper.ThrowIfNotOnUIThread();

            var infoBarHost = InfoBarUtils.GetMainWindowInfoBarHost(_serviceProvider);
            if (infoBarHost == null)
            {
                throw new Exception("Unable to get main window InfoBar host");
            }

            var supportedVersionInfoBar = new SupportedVersionInfoBar(_supportedVersionStrategy);
            var supportedVersionInfoBarUiElement = InfoBarUtils.CreateInfoBar(supportedVersionInfoBar.InfoBarModel, _serviceProvider);
            if (supportedVersionInfoBarUiElement == null)
            {
                throw new Exception($"Unable to create minimum supported version info bar parent element for {hostDeprecated.Version}");
            }

            supportedVersionInfoBar.RegisterInfoBarEvents(supportedVersionInfoBarUiElement);

            infoBarHost.AddInfoBar(supportedVersionInfoBarUiElement);

            Logger.Debug($"Minimum supported version info banner displayed for {hostDeprecated.Version}");
        }
    }
}
