namespace Amazon.AWSToolkit.Credentials.Core
{
    /// <summary>
    /// Represents a possible credential that can be used within the toolkit.
    /// </summary>
    public interface ICredentialIdentifier
    {
        /// <summary>
        /// The ID must be unique across all CredentialIdentifier instances.
        /// It is recommended to concatenate the factory ID into this field to help enforce this requirement.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// The actual name of the credential identifier being created
        /// </summary>
        string ProfileName { get; }

        /// <summary>
        /// A user friendly display name shown in the UI.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// An optional shortened version of the name to display in the UI where space is at a premium
        /// </summary>
        string ShortName { get; }

        /// <summary>
        /// The ID of the corresponding <see cref="ICredentialProviderFactory"/> so that the credential manager knows which factory to invoke in orderto resolve this into a[CredentialsProvider]
        /// </summary>
        string FactoryId { get; }
    }
}
