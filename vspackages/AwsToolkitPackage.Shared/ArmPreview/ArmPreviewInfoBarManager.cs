using System;
using System.Timers;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.VisualStudio.Utilities;

using log4net;

using Microsoft.VisualStudio.Shell;

namespace Amazon.AWSToolkit.VisualStudio.ArmPreview
{
    /// <summary>
    /// Orchestrates the life cycle of an ARM Preview InfoBar
    /// </summary>
    public class ArmPreviewInfoBarManager : IDisposable
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ArmPreviewInfoBarManager));
        private const int _setupRetryIntervalMs = 3000;

        private bool _disposed = false;

        private readonly IServiceProvider _serviceProvider;
        private readonly ToolkitContext _toolkitContext;
        private readonly Timer _timer;

        public ArmPreviewInfoBarManager(IServiceProvider serviceProvider, ToolkitContext toolkitContext)
        {
            _serviceProvider = serviceProvider;
            _toolkitContext = toolkitContext;

            _timer = new Timer()
            {
                AutoReset = false,
                Interval = _setupRetryIntervalMs,
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
        public void ShowArmPreviewInfoBar()
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
                _logger.Error(e);
            }
            finally
            {
                _disposed = true;
            }
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            // If the Toolkit hasn't fully initialized, retry this in a few seconds.
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
                    AddArmPreviewInfoBarToMainWindow();
                }
                catch (Exception exception)
                {
                    _timer.Start();
                    _logger.Error(exception);
                }
            });
        }

        private void AddArmPreviewInfoBarToMainWindow()
        {
            _logger.Debug("Trying to show ARM Preview Banner");
            ThreadHelper.ThrowIfNotOnUIThread();

            var infoBarHost = InfoBarUtils.GetMainWindowInfoBarHost(_serviceProvider);
            if (infoBarHost == null)
            {
                throw new Exception("Unable to get main window InfoBar host");
            }

            var infoBar = new ArmPreviewInfoBar(_toolkitContext);

            var infoBarUiElement = InfoBarUtils.CreateInfoBar(infoBar.InfoBarModel, _serviceProvider);
            if (infoBarUiElement == null)
            {
                throw new Exception("Unable to create InfoBar parent element");
            }

            infoBar.RegisterInfoBarEvents(infoBarUiElement);

            infoBarHost.AddInfoBar(infoBarUiElement);
            _logger.Debug("ARM Preview Banner displayed");
        }
    }
}
