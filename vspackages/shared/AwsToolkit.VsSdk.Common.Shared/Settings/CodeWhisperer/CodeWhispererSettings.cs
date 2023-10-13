using System.ComponentModel;

using Community.VisualStudio.Toolkit;

namespace AwsToolkit.VsSdk.Common.Settings.CodeWhisperer
{
    /// <summary>
    /// CodeWhisperer settings
    /// Reference: https://www.vsixcookbook.com/recipes/settings-and-options.html
    /// </summary>
    public class CodeWhispererSettings : BaseOptionModel<CodeWhispererSettings>, ILspSettings
    {
        [Category("Language Server")]
        [DisplayName("Language server path")]
        [Description("When set, overrides the default location the AWS Toolkit launches the language server from.")]
        [DefaultValue("")]
        public string LanguageServerPath { get; set; } = string.Empty;

        // TODO: Un-comment browsable when developer testing is completed to hide the setting from users
        // [Browsable(false)]
        [Category("Language Server")]
        [DisplayName("Version manifest folder")]
        [Description("When set, attempts to use the specified folder to fetch the relevant LSP Version Manifest.")]
        [DefaultValue("")]
        public string VersionManifestFolder { get; set; } = string.Empty;

        /// <summary>
        /// Enables and disables (pauses) CodeWhisperer features.
        /// </summary>
        [Category("General")]
        [DisplayName("Pause Automatic Suggestions")]
        [Description("When true, CodeWhisperer will not automatically provide code suggestions (you can still get suggestions on-demand). When false, CodeWhisperer will offer suggestions while you write code.")]
        [DefaultValue(false)]
        public bool PauseAutomaticSuggestions { get; set; } = false;

        /// <summary>
        /// Include (true) or filter out (false) suggestions that contain license attribution
        /// </summary>
        [Category("General")]
        [DisplayName("Include Suggestions with References")]
        [Description("When set to false, CodeWhisperer will only make suggestions that do not have license attribution.")]
        [DefaultValue(true)]
        public bool IncludeSuggestionsWithReferences { get; set; } = true;
    }
}
