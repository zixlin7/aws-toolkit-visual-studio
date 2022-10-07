using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.Runtime.CredentialManagement;

using Amazon.Runtime;

using Amazon.S3;
using Amazon;

namespace AwsToolkit.Tests.Integration
{
    /// <summary>
    /// Provides access to common toolkit functions in the absence of ToolkitFactory.Instance
    /// WARNING:  This class is at risk of becoming a bloated catchall.  Refactor if needed before
    /// that happens!
    /// </summary>
    public static class ToolkitTestUtils
    {
        public static T GetClient<T>(AWSCredentials credentials, string region) where T : AmazonServiceClient
        {
            var ctor = typeof(T).GetConstructor(new Type[] { typeof(AWSCredentials), typeof(RegionEndpoint) });
            return ctor.Invoke(new object[] { credentials, RegionEndpoint.GetBySystemName(region) }) as T;
        }

        public static T GetClient<T>(string region) where T : AmazonServiceClient
        {
            return GetClient<T>(GetCredentials(), region);
        }

        public static AWSCredentials GetCredentials()
        {
            var chain = new CredentialProfileStoreChain();
            chain.TryGetAWSCredentials("default", out var awsCredentials);
            return awsCredentials;
        }
    }
}
