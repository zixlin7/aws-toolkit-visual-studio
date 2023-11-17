using System;
using System.Timers;

using AwsToolkit.VsSdk.Common.Notifications;

using log4net;
using Microsoft.VisualStudio.Shell;

namespace Amazon.AWSToolkit.VisualStudio.Telemetry
{
    /// <summary>
    /// Orchestrates the life cycle of a Telemetry InfoBar
    /// </summary>
    public class TelemetryInfoBarManager : IDisposable
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(TelemetryInfoBarManager));
        private const int SetupRetryIntervalMs = 3000;

        private bool disposed = false;

        private readonly IServiceProvider _serviceProvider;
        private readonly Timer _timer;

        public TelemetryInfoBarManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
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
        public void ShowTelemetryInfoBar()
        {
            _timer.Start();
        }

        public void Dispose()
        {
            try
            {
                if (disposed)
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
                disposed = true;
            }
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            if (ToolkitFactory.Instance == null)
            {
                _timer.Start();
                return;
            }

            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread(() =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                try
                {
                    AddTelemetryInfoBarToMainWindow();
                }
                catch (Exception exception)
                {
                    _timer.Start();
                    Logger.Error(exception);
                }
            });
        }

        private void AddTelemetryInfoBarToMainWindow()
        {
            Logger.Debug("Trying to show Telemetry Banner");
            ThreadHelper.ThrowIfNotOnUIThread();

            var infoBarHost = InfoBarUtils.GetMainWindowInfoBarHost(_serviceProvider);
            if (infoBarHost == null)
            {
                throw new Exception("Unable to get main window InfoBar host");
            }

            var telemetryInfoBar = new TelemetryInfoBar();

            var infoBarUiElement = InfoBarUtils.CreateInfoBar(telemetryInfoBar.InfoBarModel, _serviceProvider);
            if (infoBarUiElement == null)
            {
                throw new Exception("Unable to create InfoBar parent element");
            }

            telemetryInfoBar.RegisterInfoBarEvents(infoBarUiElement);

            infoBarHost.AddInfoBar(infoBarUiElement);
            Logger.Debug("Telemetry Banner displayed");
        }
    }
}
