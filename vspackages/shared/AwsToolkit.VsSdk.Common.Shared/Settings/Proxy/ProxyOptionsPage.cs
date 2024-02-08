using System.Runtime.InteropServices;
using System.Windows;

using Microsoft.VisualStudio.Shell;

namespace AwsToolkit.VsSdk.Common.Settings.Proxy
{
    [ComVisible(true)]
    [Guid("bb53528e-2a49-47c4-9b69-e8b65734ad77")]
    public class ProxyOptionsPage : UIElementDialogPage
    {
        private readonly ProxyOptions _proxyOptionsControl = new ProxyOptions();

        protected override UIElement Child => _proxyOptionsControl;

        /// <summary>
        /// Overrides from where settings are loaded.
        /// </summary>
        public override void LoadSettingsFromStorage()
        {
            _proxyOptionsControl.ViewModel.Load();
        }

        /// <summary>
        /// Overrides where settings are stored
        /// </summary>
        public override void SaveSettingsToStorage()
        {
            _proxyOptionsControl.ViewModel.Save();
        }
    }
}
