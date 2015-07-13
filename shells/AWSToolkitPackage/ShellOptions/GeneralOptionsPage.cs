using System;
using System.Globalization;
using System.Diagnostics;
using System.Windows.Forms;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms.Integration;
using Amazon.Runtime.Internal.Settings;
using Amazon.AWSToolkit;
using Amazon.AWSToolkit.VisualStudio.ShellOptions;
using Microsoft.VisualStudio.Shell;

using Amazon.AWSToolkit.VisualStudio;
using Amazon.AWSToolkit.MobileAnalytics;

namespace Microsoft.Samples.VisualStudio.IDE.OptionsPage
{
    /// <summary>
    /// Container page for general AWS Toolkit options
    /// </summary>
    [Guid(GuidList.guid_VSPackageGeneralOptionsString)]
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

        protected override IWin32Window Window
        {
            get
            {
                return _hostedControl;
            }
        }

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
            var hostedFilesLocation = PersistenceManager.Instance.GetSetting(ToolkitSettingsConstants.HostedFilesLocation);
            (_hostedControl.Child as GeneralOptionsPageControl).HostedFilesLocation = hostedFilesLocation;

            var analyticsPermitted = PersistenceManager.Instance.GetSetting(ToolkitSettingsConstants.AnalyticsPermitted);
            (_hostedControl.Child as GeneralOptionsPageControl).AnalyticsPermission = analyticsPermitted;

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
            var hostedFilesLocation = (_hostedControl.Child as GeneralOptionsPageControl).HostedFilesLocation;
            PersistenceManager.Instance.SetSetting(ToolkitSettingsConstants.HostedFilesLocation, hostedFilesLocation);

            var analyticsPermitted = (_hostedControl.Child as GeneralOptionsPageControl).AnalyticsPermission;
            PersistenceManager.Instance.SetSetting(ToolkitSettingsConstants.AnalyticsPermitted, analyticsPermitted);
        }

        #endregion Event Handlers
    }
}
