using System.Collections.Generic;
using System.ComponentModel;

namespace AwsToolkit.VsSdk.Common.Settings
{
    /// <summary>
    /// Lsp settings
    /// </summary>
    public class LspSettings
    {
        /// <summary>
        /// Provides Toolkit developers a way to side-load the language server into the Toolkit.
        /// This way we don't need a specific version of a language server in order to test things out.
        /// This is also an escape hatch in case we need to troubleshoot a test build with a customer.
        /// </summary>
        [DisplayName("Language Server Override Path")]
        [Description("When set, overrides the default location the AWS Toolkit launches the language server from.")]
        [DefaultValue("")]
        [Browsable(true),
        NotifyParentProperty(true),
        EditorBrowsable(EditorBrowsableState.Always)]
        public string LanguageServerPath { get; set; } = string.Empty;

        /// <summary>
        /// Provides Toolkit developers a way to load the lsp version manifest from a local folder 
        /// This provides a way to test things locally while the remote version is still under development
        /// </summary>
        // TODO: Un-comment browsable when developer testing is completed to hide the setting from users
        // [Browsable(false)]
        [DisplayName("Version Manifest Folder")]
        [Description("When set, attempts to use the specified folder to fetch the relevant LSP Version Manifest.")]
        [DefaultValue("")]
        [Browsable(true), NotifyParentProperty(true),
         EditorBrowsable(EditorBrowsableState.Never)]
        public string VersionManifestFolder { get; set; } = string.Empty;

        ///<summary>
        /// Represents list of version manifest deprecation notices dismissed by the user 
        /// </summary>
        /// <remark>Can be saved by user actions <see cref="ILspSettingsRepository.SaveLspSettingsAsync"/> </remark>
        //[Browsable(false)]
        [DisplayName("Dismissed Manifest Deprecation Notices")]
        [Description("Represents list of version manifest deprecation notices dismissed by the user")]
        [DefaultValue("")]
        [Browsable(false), NotifyParentProperty(true),
         EditorBrowsable(EditorBrowsableState.Never)]
        public List<DismissedManifestDeprecation> DismissedManifestDeprecations { get; set; } = new List<DismissedManifestDeprecation>();

        ///<summary>
        /// Represents list of version manifests and their associated cached etag values
        /// </summary>
        [DisplayName("Cached Manifest E-Tags")]
        [Description("Represents list of version manifests and their associated cached etag values")]
        [Browsable(false), NotifyParentProperty(true),
         EditorBrowsable(EditorBrowsableState.Never)]
        public List<ManifestCachedEtag> ManifestCachedEtags { get; set; } = new List<ManifestCachedEtag>();

        public override string ToString()
        {
            return string.Empty;
        }

    }
}
