using System.ComponentModel;

using Community.VisualStudio.Toolkit;

namespace AwsToolkit.VsSdk.Common.Settings.CodeWhisperer
{
    /// <summary>
    /// CodeWhisperer settings
    /// Reference: https://www.vsixcookbook.com/recipes/settings-and-options.html
    /// </summary>
    public class CodeWhispererSettings : BaseOptionModel<CodeWhispererSettings>
    {
        /// <summary>
        /// Provides Toolkit developers a way to side-load the language server into the Toolkit.
        /// This way we don't need a specific version of a language server in order to test things out.
        /// This is also an escape hatch in case we need to troubleshoot a test build with a customer.
        /// </summary>
        [Category("Language Server")]
        [DisplayName("Language server path")]
        [Description("When set, overrides the default location the AWS Toolkit launches the language server from.")]
        [DefaultValue("")]
        public string LanguageServerPath { get; set; } = string.Empty;

        /// <summary>
        /// Enables and disables (pauses) CodeWhisperer features.
        /// </summary>
        [Category("General")]
        [DisplayName("Pause Automatic Suggestions")]
        [Description("When true, CodeWhisperer will not automatically provide code suggestions (you can still get suggestions on-demand). When false, CodeWhisperer will offer suggestions while you write code.")]
        [DefaultValue(false)]
        public bool PauseAutomaticSuggestions { get; set; } = false;
    }
}
