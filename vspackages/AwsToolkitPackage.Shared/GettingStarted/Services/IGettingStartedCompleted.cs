namespace Amazon.AWSToolkit.VisualStudio.GettingStarted.Services
{
    /// <summary>
    /// Provides access to update final Getting Started screen properties when adding new profiles.
    /// </summary>
    public interface IGettingStartedCompleted
    {
        /// <summary>
        /// Displays invisible/red/green status box when null/false/true.
        /// </summary>
        bool? Status { get; set; }

        /// <summary>
        /// Credential type name of successfully added profile.
        /// </summary>
        string CredentialTypeName { get; set; }

        /// <summary>
        /// Credential name of successfully added profile.
        /// </summary>
        string CredentialName { get; set; }
    }
}
