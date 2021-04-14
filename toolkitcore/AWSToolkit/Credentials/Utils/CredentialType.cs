namespace Amazon.AWSToolkit.Credentials.Utils
{
    /// <summary>
    /// Generalization of Credential types
    /// </summary>
    public enum CredentialType
    {
        Undefined,
        StaticProfile,
        StaticSessionProfile,
        CredentialProcessProfile,
        AssumeRoleProfile,
        AssumeMfaRoleProfile,
        SsoProfile,
        Unknown,
    }
}
