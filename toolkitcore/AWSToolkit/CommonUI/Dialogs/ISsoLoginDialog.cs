namespace Amazon.AWSToolkit.CommonUI.Dialogs
{
    public interface ISsoLoginDialog
    {
        /// <summary>
        /// Indicates whether the connection represents an AWS SSO Credential profile
        /// or an AWS Builder ID connection
        /// </summary>
        bool IsBuilderId { get; set; }

        /// <summary>
        /// User code required for authorization
        /// </summary>
        string UserCode { get; set; }

        /// <summary>
        /// Login Uri to be opened in browser
        /// </summary>
        string LoginUri { get; set; }

        /// <summary>
        /// The name of the credential profile for which connection is being made
        /// only applicable for SSO based credential profile
        /// </summary>
        string CredentialName { get; set; }

        bool Show();
    }
}
