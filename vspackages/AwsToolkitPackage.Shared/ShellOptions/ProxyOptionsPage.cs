using System.Windows.Forms;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Amazon.AWSToolkit;
using Amazon.AWSToolkit.VisualStudio.ShellOptions;
using Amazon.AWSToolkit.VisualStudio;

namespace Microsoft.Samples.VisualStudio.IDE.OptionsPage
{
    /// <summary>
    /// Container page for Proxy AWS Toolkit options
    /// </summary>
    [Guid(GuidList.ProxyOptionsGuidString)]
    [ComVisible(true)]
	public class ProxyOptionsPage : DialogPage
    {
        readonly ProxyOptionsPageForm _hostedControl;

        public ProxyOptionsPage()
        {
            this._hostedControl = new ProxyOptionsPageForm();
        }


        protected override IWin32Window Window => this._hostedControl;

        #region Event Handlers

        /// <summary>
        /// Handles "Activate" messages from the Visual Studio environment.
        /// </summary>
		/// <devdoc>
		/// This method is called when Visual Studio wants to activate this page.  
		/// </devdoc>
        /// <remarks>If the Cancel property of the event is set to true, the page is not activated.</remarks>
        protected override void OnActivate(CancelEventArgs e)
		{
            var currentSettings = ProxyUtilities.RetrieveCurrentSettings();
            this._hostedControl.ProxySettings = currentSettings;
            base.OnActivate(e);
		}

        /// <summary>
        /// Handles "Deactive" messages from the Visual Studio environment.
        /// </summary>
		/// <devdoc>
		/// This method is called when VS wants to deactivate this
		/// page.  If true is set for the Cancel property of the event, 
		/// the page is not deactivated.
		/// </devdoc>
        /// <remarks>
        /// A "Deactive" message is sent when a dialog page's user interface 
        /// window loses focus or is minimized but is not closed.
        /// </remarks>
		protected override void OnDeactivate(CancelEventArgs e)
		{
		}

        /// <summary>
        /// Handles Apply messages from the Visual Studio environment.
        /// </summary>
		/// <devdoc>
		/// This method is called when VS wants to save the user's 
		/// changes then the dialog is dismissed.
		/// </devdoc>
        protected override void OnApply(PageApplyEventArgs e)
		{
            var settings = this._hostedControl.ProxySettings;
            ProxyUtilities.ApplyProxySettings(settings);
        }

        #endregion Event Handlers
    }
}
