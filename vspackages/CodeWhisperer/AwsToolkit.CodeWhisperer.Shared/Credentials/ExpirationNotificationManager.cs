using System;
using System.ComponentModel.Composition;

namespace Amazon.AwsToolkit.CodeWhisperer.Credentials
{
    /// <summary>
    /// Informs the user (in a non-blocking manner) when credentials expire.
    /// </summary>
    [Export(typeof(IExpirationNotificationManager))]
    internal class ExpirationNotificationManager : IExpirationNotificationManager, IDisposable
    {
        private readonly ICodeWhispererManager _manager;

        [ImportingConstructor]
        public ExpirationNotificationManager(ICodeWhispererManager manager)
        {
            _manager = manager;

            _manager.StatusChanged += OnConnectionStatusChanged;
        }

        private void OnConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs e)
        {
            // todo : IDE-11373 : if status is Expired, show an InfoBar
            // todo : IDE-11373 : if status is Connected and the InfoBar is showing, hide it
        }

        public void Dispose()
        {
            _manager.StatusChanged -= OnConnectionStatusChanged;
        }
    }
}
