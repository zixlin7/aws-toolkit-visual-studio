using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.CommonUI.WizardFramework;

namespace Amazon.AWSToolkit.Shared
{
    /// <summary>
    /// Optional interface for toolkit shell providers, allowing windows/wizards/dialogs
    /// launched from within the toolkit code to adopt the theme of the parent shell in
    /// a programmatic fashion.
    /// </summary>
    public interface IAWSToolkitShellThemeService
    {
        /// <summary>
        /// Called when the toolkit is about to launch a wizard
        /// </summary>
        /// <param name="wizard"></param>
        void ThemeWizard(IAWSWizard wizard);

        /// <summary>
        /// Called at control instantiation or on theme change detection to determine additional
        /// theme specific resource dictionary updates that need to be applied to the control.
        /// </summary>
        /// <param name="apply">
        /// These dictionaries, if any, will be added to the top of the merged dictionaries for
        /// the requesting control.
        /// </param>
        /// <param name="remove">
        /// Contains the source uris of any dictionaries to be removed from the merged dictionaries
        /// of the requesting control.
        /// </param>
        /// <remarks>
        /// Not all of the keys used by Visual Studio look equally good in light and dark themes,
        /// so we do theme detection and then apply (or remove) additional override dictionaries that
        /// map to better colors as needed.
        /// </remarks>
        void QueryShellThemeOverrides(out IEnumerable<Uri> apply, out IEnumerable<Uri> remove);
    }
}
