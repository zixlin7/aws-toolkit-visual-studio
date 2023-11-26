using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Credentials.InfoBar;
using Amazon.AwsToolkit.VsSdk.Common.Tasks;
using Amazon.AWSToolkit.Context;

using AwsToolkit.VsSdk.Common.Notifications;

using log4net;

using Microsoft.VisualStudio.Shell;

namespace Amazon.AwsToolkit.CodeWhisperer.Credentials
{
    /// <summary>
    /// Informs the user (in a non-blocking manner) when credentials expire.
    /// </summary>
    [Export(typeof(IExpirationNotificationManager))]
    internal class ExpirationNotificationManager : IExpirationNotificationManager, IDisposable
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ExpirationNotificationManager));

        private readonly ICodeWhispererManager _manager;
        private readonly IServiceProvider _serviceProvider;
        private readonly ToolkitJoinableTaskFactoryProvider _taskFactoryProvider;
        private ConnectionExpiredInfoBar _infoBar;

        [ImportingConstructor]
        public ExpirationNotificationManager(ICodeWhispererManager manager,
            SVsServiceProvider serviceProvider,
            IToolkitContextProvider toolkitContextProvider,
            ToolkitJoinableTaskFactoryProvider taskFactoryProvider)
        {
            _manager = manager;
            _serviceProvider = serviceProvider;
            _taskFactoryProvider = taskFactoryProvider;

            _manager.ConnectionStatusChanged += OnConnectionStatusChanged;
        }

        private void OnConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs e)
        {
            _taskFactoryProvider.JoinableTaskFactory.Run(async () => await OnConnectionStatusChangedAsync(sender, e));
        }

        private async Task OnConnectionStatusChangedAsync(object sender, ConnectionStatusChangedEventArgs e)
        {
            switch (e.ConnectionStatus)
            {
                case ConnectionStatus.Expired:
                    // Use a banner to inform the user that their connection is expired.
                    await ShowExpiredConnectionInfoBarAsync();
                    break;
                case ConnectionStatus.Connected:
                    // The user is now connected, so close the "expired connection" banner if it is showing.
                    await CloseInfoBarAsync();
                    break;
                default:
                    // We do nothing when Disconnected -- the connection component auto-progresses
                    // the state from Expired to Disconnected, and we want the expired banner to remain visible.
                    break;
            }
        }

        protected async Task ShowExpiredConnectionInfoBarAsync()
        {
            try
            {
                await _taskFactoryProvider.JoinableTaskFactory.SwitchToMainThreadAsync();

                var infoBarHost = InfoBarUtils.GetMainWindowInfoBarHost(_serviceProvider) ??
                                  throw new Exception("Unable to get main window InfoBar host");

                var infoBar = new ConnectionExpiredInfoBar(_manager, _taskFactoryProvider);
                var infoBarUiElement = InfoBarUtils.CreateInfoBar(infoBar.InfoBarModel, _serviceProvider) ??
                                       throw new Exception($"Unable to create info bar parent element");

                infoBar.RegisterInfoBarEvents(infoBarUiElement);
                infoBarHost.AddInfoBar(infoBarUiElement);

                _infoBar = infoBar;
            }
            catch (Exception e)
            {
                _logger.Error("Unable to display the expired connection banner", e);
            }
        }

        private async Task CloseInfoBarAsync()
        {
            try
            {
                await _taskFactoryProvider.JoinableTaskFactory.SwitchToMainThreadAsync();

                _infoBar?.Close();
                _infoBar = null;
            }
            catch (Exception e)
            {
                _logger.Error("Failed to close expiration notice. It might remain on the screen.", e);
            }
        }

        public void Dispose()
        {
            _manager.ConnectionStatusChanged -= OnConnectionStatusChanged;
        }
    }
}
