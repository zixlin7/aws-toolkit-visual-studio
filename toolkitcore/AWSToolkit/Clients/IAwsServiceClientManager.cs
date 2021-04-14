using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;
using Amazon.Runtime;

namespace Amazon.AWSToolkit.Clients
{
    /// <summary>
    /// Responsible for producing and configuring service clients for the Toolkit.
    /// </summary>
    public interface IAwsServiceClientManager
    {
        /// <summary>
        /// Produce and initialize a service client
        /// </summary>
        /// <typeparam name="T">Service Client to create</typeparam>
        /// <param name="credentialIdentifier">credentials to instantiate the client with</param>
        /// <param name="region">region to instantiate the client with</param>
        /// <returns>A created service client, or null of there is an error</returns>
        T CreateServiceClient<T>(ICredentialIdentifier credentialIdentifier, ToolkitRegion region) where T : class;

        /// <summary>
        /// Produce and initialize a service client
        /// Overload that accepts a region Id instead of <see cref="ToolkitRegion"/>
        /// </summary>
        /// <typeparam name="T">Service Client to create</typeparam>
        /// <param name="credentialIdentifier">credentials to instantiate the client with</param>
        /// <param name="regionId">region to instantiate the client with</param>
        /// <returns>A created service client, or null of there is an error</returns>
        T CreateServiceClient<T>(ICredentialIdentifier credentialIdentifier, string regionId) where T : class;

        /// <summary>
        /// Produce and initialize a service client
        /// Overload that accepts a service client config as an additional parameter.
        ///
        /// Config fields relating to region (ServiceUrl for example) are adjusted based on
        /// the provided region.
        /// </summary>
        /// <typeparam name="T">Service Client to create</typeparam>
        /// <param name="credentialIdentifier">credentials to instantiate the client with</param>
        /// <param name="region">region to instantiate the client with</param>
        /// <param name="serviceClientConfig">Configuration to use with the service client. The region param overrides Region/Url related fields in the config.</param>
        /// <returns>A created service client, or null of there is an error</returns>
        T CreateServiceClient<T>(ICredentialIdentifier credentialIdentifier, ToolkitRegion region,
            ClientConfig serviceClientConfig) where T : class;
    }
}
