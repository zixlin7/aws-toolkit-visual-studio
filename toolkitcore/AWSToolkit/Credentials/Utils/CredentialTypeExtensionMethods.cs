using TelemetryCredentialType = Amazon.AwsToolkit.Telemetry.Events.Generated.CredentialType;

namespace Amazon.AWSToolkit.Credentials.Utils
{
    public static class CredentialTypeExtensionMethods
    {
        /// <summary>
        /// Converts <see cref="CredentialType"/> to the Telemetry based <see cref="TelemetryCredentialType"/>
        /// </summary>
        public static TelemetryCredentialType? AsTelemetryCredentialType(this CredentialType credentialType)
        {
            switch (credentialType)
            {
                case CredentialType.StaticProfile:
                    return TelemetryCredentialType.StaticProfile;
                case CredentialType.StaticSessionProfile:
                    return TelemetryCredentialType.StaticSessionProfile;
                case CredentialType.CredentialProcessProfile:
                    return TelemetryCredentialType.CredentialProcessProfile;
                case CredentialType.AssumeRoleProfile:
                    return TelemetryCredentialType.AssumeRoleProfile;
                case CredentialType.AssumeEc2InstanceRoleProfile:
                    return TelemetryCredentialType.Ec2Metadata;
                case CredentialType.AssumeMfaRoleProfile:
                    return TelemetryCredentialType.AssumeMfaRoleProfile;
                case CredentialType.AssumeSamlRoleProfile:
                    return TelemetryCredentialType.AssumeSamlRoleProfile;
                case CredentialType.SsoProfile:
                    return TelemetryCredentialType.SsoProfile;
                case CredentialType.BearerToken:
                    return TelemetryCredentialType.BearerToken;
                case CredentialType.Undefined:
                    return null;
                case CredentialType.Unknown:
                default:
                    return TelemetryCredentialType.Other;
            }
        }
    }
}
