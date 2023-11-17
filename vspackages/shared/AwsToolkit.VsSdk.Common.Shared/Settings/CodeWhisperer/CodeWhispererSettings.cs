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
        [Category("General")]
        [DisplayName("Language Server Settings")]
        [Description("Represents language server specific settings")]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public LspSettings LspSettings { get; set; } = new LspSettings();

        /// <summary>
        /// Represents the Credential Id (<see cref="Amazon.AWSToolkit.Credentials.Core.ICredentialIdentifier.Id"/>)
        /// currently signed in to CodeWhisperer.
        /// Null/empty represents signed out state.
        /// </summary>
        [Browsable(false)]
        [Category("Connection")]
        [DisplayName("Sign-in credentials")]
        public string CredentialIdentifier { get; set; }

        /// <summary>
        /// Enables and disables (pauses) CodeWhisperer automatic suggestions.
        /// </summary>
        [Category("General")]
        [DisplayName("Auto-suggestions enabled")]
        [Description("When true, CodeWhisperer will automatically offer suggestions while you write code. When false, CodeWhisperer will not automatically provide code suggestions (you can still get suggestions on-demand).")]
        [DefaultValue(true)]
        public bool AutomaticSuggestionsEnabled { get; set; } = true;

        /// <summary>
        /// Include (true) or filter out (false) suggestions that contain license attribution
        /// </summary>
        [Category("General")]
        [DisplayName("Include Suggestions with References")]
        [Description("When set to false, CodeWhisperer will only make suggestions that do not have license attribution.")]
        [DefaultValue(true)]
        public bool IncludeSuggestionsWithReferences { get; set; } = true;


        /// <summary>
        /// Share CodeWhisperer content with AWS (true/false)
        /// </summary>
        [Category("General")]
        [DisplayName("Share CodeWhisperer Content with AWS")]
        [Description("When set to true, your content processed by CodeWhisperer may be used for service improvement (except for content processed by the Enterprise CodeWhisperer service tier). Setting to false will cause AWS to delete any of your content used for that purpose. The information used to provide the CodeWhisperer service to you will not be affected. See the Service Terms (https://aws.amazon.com/service-terms) for more detail.")]
        [DefaultValue(true)]
        public bool ShareCodeWhispererContentWithAws { get; set; } = true;
    }
}
