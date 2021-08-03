using System.Windows.Forms;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms.Integration;
using Microsoft.VisualStudio.Shell;

using Amazon.AWSToolkit.Settings;
using Amazon.AWSToolkit.VisualStudio.ShellOptions;
using Amazon.AWSToolkit.VisualStudio;

namespace Microsoft.Samples.VisualStudio.IDE.OptionsPage
{
    /// <summary>
    /// Container page for general AWS Toolkit options
    /// </summary>
    [Guid(GuidList.GeneralOptionsGuidString)]
    [ComVisible(true)]
	public class GeneralOptionsPage : DialogPage
    {
        readonly ElementHost _hostedControl;

        public GeneralOptionsPage()
        {
            _hostedControl = new ElementHost
            {
                Child = new GeneralOptionsPageControl(),
                Dock = DockStyle.Fill
            };
        }

        protected override IWin32Window Window => _hostedControl;

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
            var optionsControl = _hostedControl.Child as GeneralOptionsPageControl;
            if (optionsControl == null) return;

            var hostedFilesLocation = ToolkitSettings.Instance.HostedFilesLocation;
            optionsControl.HostedFilesLocation = hostedFilesLocation;

            optionsControl.TelemetryEnabled = ToolkitSettings.Instance.TelemetryEnabled;

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
            // place the settings into toolkit storage under the user's AppData/Local
            // location; this then makes it accessible by all AWS tooling
            //e.ApplyBehavior = ApplyKind.Apply;

            // returns empty for 'default', or region system name to use regional location, or 
            // specific file system location
            var optionsControl = _hostedControl.Child as GeneralOptionsPageControl;
            if (optionsControl == null) return;

            var hostedFilesLocation = optionsControl.HostedFilesLocation;
            ToolkitSettings.Instance.HostedFilesLocation = hostedFilesLocation;

            ToolkitSettings.Instance.TelemetryEnabled = optionsControl.TelemetryEnabled;
        }

        #endregion Event Handlers
    }
}
