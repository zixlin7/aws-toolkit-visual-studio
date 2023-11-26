using System;
using System.Timers;

using Amazon.AWSToolkit.Context;

using AwsToolkit.VsSdk.Common.Notifications;

using log4net;

using Microsoft.VisualStudio.Shell;

namespace Amazon.AWSToolkit.CommonUI
{
    /// <summary>
    /// Common toolkit component for managing info bars
    /// </summary>
    public abstract class ToolkitInfoBarManager : IDisposable
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ToolkitInfoBarManager));
        private const int _setupRetryIntervalMs = 3000;

        private bool _disposed = false;

        protected readonly IServiceProvider _serviceProvider;
        protected readonly ToolkitContext _toolkitContext;
        private readonly Timer _timer;

        protected ToolkitInfoBarManager(IServiceProvider serviceProvider, ToolkitContext toolkitContext)
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
        /// Unique identifier/name for info bar
        /// </summary>
        protected abstract string _identifier { get; }


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
            if (_toolkitContext.ToolkitHost == null)
            {
                _timer.Start();
                return;
            }

            _toolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
            {
                try
                {
                    AddInfoBarToMainWindow();
                }
                catch (Exception exception)
                {
                    _timer.Start();
                    _logger.Error(exception);
                }
            });
        }

        protected virtual void AddInfoBarToMainWindow()
        {
            _logger.Debug($"Trying to show info bar for {_identifier}");
            ThreadHelper.ThrowIfNotOnUIThread();

            var infoBarHost = InfoBarUtils.GetMainWindowInfoBarHost(_serviceProvider);
            if (infoBarHost == null)
            {
                throw new Exception("Unable to get main window InfoBar host");
            }

            var infoBar = CreateInfoBar();
            var infoBarUiElement = InfoBarUtils.CreateInfoBar(infoBar.InfoBarModel, _serviceProvider);
            if (infoBarUiElement == null)
            {
                throw new Exception(
                    $"Unable to create info bar parent element for {_identifier}");
            }

            infoBar.RegisterInfoBarEvents(infoBarUiElement);

            infoBarHost.AddInfoBar(infoBarUiElement);

            _logger.Debug($"Info bar displayed for {_identifier}");
        }


        protected abstract ToolkitInfoBar CreateInfoBar();
    }
}
