using System.Diagnostics;

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

            // Give ourselves a hint when this function should be updated
            Debug.Assert(!Debugger.IsAttached, $"Unhandled factory Id: {credentialFactoryId}",
                $"The Toolkit needs to be updated to report the correct credential source for {credentialFactoryId} in metrics. This was probably caused by adding a new credentials provider, but forgetting to map it to a credentials source.");

            return CredentialSourceId.Other;
        }
    }
}
