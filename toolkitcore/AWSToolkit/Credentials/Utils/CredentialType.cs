using System.ComponentModel;

namespace Amazon.AWSToolkit.Credentials.Utils
{
    /// <summary>
    /// Generalization of Credential types
    /// </summary>
    public enum CredentialType
    {
        Undefined,
        [Description("IAM User Role")]
        StaticProfile,
        [Description("IAM Role Session")]
        StaticSessionProfile,
        [Description("Credential Process")]
        CredentialProcessProfile,
        [Description("Assume Role")]
        AssumeRoleProfile,
        [Description("Assume EC2 Instance Role")]
        AssumeEc2InstanceRoleProfile,
        [Description("Assume MFA")]
        AssumeMfaRoleProfile,
        [Description("Assume SAML")]
        AssumeSamlRoleProfile,
        [Description("IAM Identity Center (Successor to AWS Single Sign-on)")]
        SsoProfile,
        [Description("Bearer Token")]
        BearerToken,
        Unknown,
    }
}
