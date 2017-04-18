using System;
using System.Collections.Generic;

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

        object CaptionFontFamilyKey { get; }
        object CaptionFontSizeKey { get; }
        object CaptionFontWeightKey { get; }

        object EnvironmentBoldFontWeightKey { get; }
        object EnvironmentFontFamilyKey { get; }
        object EnvironmentFontSizeKey { get; }

        object Environment122PercentFontSizeKey { get; }
        object Environment122PercentFontWeightKey { get; }
        object Environment133PercentFontSizeKey { get; }
        object Environment133PercentFontWeightKey { get; }
        object Environment155PercentFontSizeKey { get; }
        object Environment155PercentFontWeightKey { get; }
        object Environment200PercentFontSizeKey { get; }
        object Environment200PercentFontWeightKey { get; }
        object Environment310PercentFontSizeKey { get; }
        object Environment310PercentFontWeightKey { get; }
        object Environment375PercentFontSizeKey { get; }
        object Environment375PercentFontWeightKey { get; }
    }
}
