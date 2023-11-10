using System;
using System.Threading;
using System.Timers;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.VisualStudio.Utilities;

using log4net;

using Microsoft.VisualStudio.Threading;

using Task = System.Threading.Tasks.Task;
using Timer = System.Timers.Timer;

namespace Amazon.AWSToolkit.VisualStudio.SupportedVersion
{
    /// <summary>
    /// Orchestrates the life cycle of a Sunset notification InfoBar
    /// </summary>
    public class SunsetNotificationBarManager : IDisposable
    {
        static readonly ILog _logger = LogManager.GetLogger(typeof(SunsetNotificationBarManager));
        private const int _setupRetryIntervalMs = 3000;

        private bool _disposed = false;

        private readonly IServiceProvider _serviceProvider;
        private readonly JoinableTaskFactory _taskFactory;
        private readonly CancellationToken _cancellationToken;
        private readonly Timer _timer;
        private readonly ISunsetNotificationStrategy _strategy;
        private readonly ToolkitContext _toolkitContext;

        public SunsetNotificationBarManager(
            IServiceProvider serviceProvider,
            ISunsetNotificationStrategy strategy,
            ToolkitContext toolkitContext,
            JoinableTaskFactory taskFactory,
            CancellationToken cancellationToken)
        {
            _serviceProvider = serviceProvider;
            _strategy = strategy;
            _toolkitContext = toolkitContext;
            _taskFactory = taskFactory;
            _cancellationToken = cancellationToken;
            _timer = new Timer()
            {
                AutoReset = false,
                Interval = _setupRetryIntervalMs,
            };
            _timer.Elapsed += OnTimerElapsed;
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
                _timer.Elapsed -= OnTimerElapsed;
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

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _taskFactory.Run(async () => await OnTimerElapsedAsync());
        }

        private async Task OnTimerElapsedAsync()
        {
            try
            {
                await AddInfoBarToMainWindowAsync();
            }
            catch (Exception exception)
            {
                _timer.Start();
                _logger.Error(exception);
            }
        }

        private async Task AddInfoBarToMainWindowAsync()
        {
            await _taskFactory.SwitchToMainThreadAsync();
            _logger.Debug($"Trying to show sunset notification for {_strategy.Identifier}");

            var infoBarHost = InfoBarUtils.GetMainWindowInfoBarHost(_serviceProvider);
            if (infoBarHost == null)
            {
                throw new Exception("Unable to get main window InfoBar host");
            }

            var infoBar = new SunsetNotificationInfoBar(_strategy, _toolkitContext, _taskFactory, _cancellationToken);
            var infoBarUiElement = InfoBarUtils.CreateInfoBar(infoBar.InfoBarModel, _serviceProvider);
            if (infoBarUiElement == null)
            {
                throw new Exception($"Unable to create parent element for sunset notification {_strategy.Identifier}");
            }

            infoBar.RegisterInfoBarEvents(infoBarUiElement);

            infoBarHost.AddInfoBar(infoBarUiElement);

            _logger.Debug($"Sunset notification displayed for {_strategy.Identifier}");
        }
    }
}
