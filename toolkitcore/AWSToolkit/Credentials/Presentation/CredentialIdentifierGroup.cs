namespace Amazon.AWSToolkit.Credentials.Presentation
{
    /// <summary>
    /// Simple record class representing how a set of <see cref="Core.ICredentialIdentifier"/> is grouped,
    /// and what that group's sort priority is.
    /// </summary>
    public class CredentialsIdentifierGroup
    {
        public static readonly CredentialsIdentifierGroup SdkCredentials = new CredentialsIdentifierGroup
        {
            GroupName = ".NET Credentials",
            SortPriority = 1,
        };

        public static readonly CredentialsIdentifierGroup SharedCredentials = new CredentialsIdentifierGroup
        {
            GroupName = "Shared Credentials",
            SortPriority = SdkCredentials.SortPriority + 1,
        };

        public static readonly CredentialsIdentifierGroup AdditionalCredentials = new CredentialsIdentifierGroup
        {
            GroupName = "Additional Credentials",
            SortPriority = SharedCredentials.SortPriority + 1,
        };

        public string GroupName;

        /// <summary>
        /// A lower priority value would be sorted before a higher value
        /// </summary>
        public int SortPriority;

        // Used by the implicit UI Binding
        public override string ToString()
        {
            return GroupName;
        }
    }
}
