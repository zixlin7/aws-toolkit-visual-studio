using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;

namespace Amazon.AWSToolkit.Credentials.Utils
{
    public static class CredentialSource
    {
        /// <summary>
        /// Returns the metrics based credential source for a given credential factory Id.
        /// Defaults to <see cref="CredentialSourceId.Other"/> if unknown.
        /// </summary>
        public static CredentialSourceId FromCredentialFactoryId(string credentialFactoryId)
        {
            if (SharedCredentialProviderFactory.SharedProfileFactoryId.Equals(credentialFactoryId))
            {
                return CredentialSourceId.SharedCredentials;
            }
            else if (SDKCredentialProviderFactory.SdkProfileFactoryId.Equals(credentialFactoryId))
            {
                return CredentialSourceId.SdkStore;
            }

            return CredentialSourceId.Other;
        }
    }
}
