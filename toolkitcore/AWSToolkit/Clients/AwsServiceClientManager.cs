using System;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;
using Amazon.Runtime;

using log4net;

namespace Amazon.AWSToolkit.Clients
{
    /// <summary>
    /// Responsible for producing and configuring service clients for the Toolkit.
    /// </summary>
    public class AwsServiceClientManager : IAwsServiceClientManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AwsServiceClientManager));
        private readonly ICredentialManager _credentialManager;
        private readonly IRegionProvider _regionProvider;

        public AwsServiceClientManager(ICredentialManager credentialManager, IRegionProvider regionProvider)
        {
            _credentialManager = credentialManager;
            _regionProvider = regionProvider;
        }

        /// <summary>
        /// Produce and initialize a service client
        /// Overload that accepts a region Id instead of <see cref="ToolkitRegion"/>
        /// </summary>
        /// <typeparam name="T">Service Client to create</typeparam>
        /// <param name="credentialIdentifier">credentials to instantiate the client with</param>
        /// <param name="regionId">region to instantiate the client with</param>
        /// <returns>A created service client, or null of there is an error</returns>
        public T CreateServiceClient<T>(ICredentialIdentifier credentialIdentifier, string regionId) where T : class
        {
            try
            {
                ToolkitRegion region = _regionProvider.GetRegion(regionId);
                if (region == null)
                {
                    throw new NullReferenceException($"Unknown region id: {regionId}");
                }

                return CreateServiceClient<T>(credentialIdentifier, region);
            }
            catch (Exception e)
            {
                var type = typeof(T);
                Logger.Error($"Error creating service client: {type.FullName}", e);
                return null;
            }
        }

        /// <summary>
        /// Produce and initialize a service client
        /// </summary>
        /// <typeparam name="T">Service Client to create</typeparam>
        /// <param name="credentialIdentifier">credentials to instantiate the client with</param>
        /// <param name="region">region to instantiate the client with</param>
        /// <returns>A created service client, or null of there is an error</returns>
        public T CreateServiceClient<T>(ICredentialIdentifier credentialIdentifier, ToolkitRegion region)
            where T : class
        {
            Type serviceClientType = typeof(T);
            try
            {
                if (_regionProvider.IsRegionLocal(region.Id))
                {
                    var serviceClientConfig = CreateServiceClientConfig(serviceClientType);
                    if (serviceClientConfig == null)
                    {
                        throw new Exception($"Unable to get service client config type for {serviceClientType.Name}");
                    }

                    return CreateServiceClient<T>(credentialIdentifier, region, serviceClientConfig);
                }
                else
                {
                    var toolkitCredentials = _credentialManager.GetToolkitCredentials(credentialIdentifier, region);
                    if (toolkitCredentials == null)
                    {
                        throw new Exception($"Unable to get toolkit credentials for {credentialIdentifier.Id}");
                    }

                    if (toolkitCredentials.Supports(AwsConnectionType.AwsToken))
                    {
                        // Calling code should use the overload that accepts a ClientConfig
                        throw new Exception($"Token provider based credentials require a service client configuration");
                    }

                    var regionEndpoint = RegionEndpoint.GetBySystemName(region.Id);
                    if (regionEndpoint == null)
                    {
                        throw new Exception($"Unexpected region id: {region.Id}");
                    }

                    return ServiceClientCreator.CreateServiceClient(serviceClientType,
                        toolkitCredentials.GetAwsCredentials(),
                        regionEndpoint) as T;
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Error creating service client: {serviceClientType.FullName}", e);
                return null;
            }
        }

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
        public T CreateServiceClient<T>(ICredentialIdentifier credentialIdentifier, ToolkitRegion region,
            ClientConfig serviceClientConfig) where T : class
        {
            Type serviceClientType = typeof(T);

            try
            {
                if (_regionProvider.IsRegionLocal(region.Id))
                {
                    return CreateLocalServiceClient<T>(serviceClientConfig, serviceClientType);
                }
                else
                {
                    var toolkitCredentials = _credentialManager.GetToolkitCredentials(credentialIdentifier, region);
                    if (toolkitCredentials == null)
                    {
                        throw new Exception($"Unable to get toolkit credentials for {credentialIdentifier.Id}");
                    }

                    AWSCredentials credentials = null;
                    if (toolkitCredentials.Supports(AwsConnectionType.AwsToken))
                    {
                        serviceClientConfig.AWSTokenProvider = toolkitCredentials.GetTokenProvider();
                        credentials = new AnonymousAWSCredentials();
                    }
                    else
                    {
                        // Configure the Service client to use the provided region
                        SetRegionEndpoint<T>(region, serviceClientConfig);

                        credentials = toolkitCredentials.GetAwsCredentials();
                    }

                    return ServiceClientCreator.CreateServiceClient(serviceClientType, credentials, serviceClientConfig) as T;
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Error creating service client: {serviceClientType.FullName}", e);
                return null;
            }
        }

        private T CreateLocalServiceClient<T>(ClientConfig serviceClientConfig, Type serviceClientType) where T : class
        {
            // Configure the Service client to use the local url
            var serviceUrl = _regionProvider.GetLocalEndpoint(serviceClientConfig.RegionEndpointServiceName);
            if (string.IsNullOrWhiteSpace(serviceUrl))
            {
                throw new Exception(
                    $"Unable to get local endpoint for service client: {serviceClientType.Name} and service: {serviceClientConfig.RegionEndpointServiceName}");
            }

            serviceClientConfig.ServiceURL = serviceUrl;

            // Service client requires mock credentials to create localhost
            AWSCredentials credentials = new AwsMockCredentials();
            return ServiceClientCreator.CreateServiceClient(serviceClientType, credentials, serviceClientConfig) as T;
        }

        private static void SetRegionEndpoint<T>(ToolkitRegion region, ClientConfig serviceClientConfig) where T : class
        {
            var regionEndpoint = RegionEndpoint.GetBySystemName(region.Id);
            if (regionEndpoint == null)
            {
                throw new Exception($"Unexpected region id: {region.Id}");
            }

            serviceClientConfig.RegionEndpoint = regionEndpoint;
        }

        /// <summary>
        /// Given a Service Client type, instantiates and returns a ClientConfig for that service client.
        /// </summary>
        /// <param name="serviceClientType">Service Client class type. Expected to be a service client.</param>
        private static ClientConfig CreateServiceClientConfig(Type serviceClientType)
        {
            if (string.IsNullOrWhiteSpace(serviceClientType?.FullName))
            {
                throw new Exception("Unable to get name of service client type");
            }

            // Determine the name of the service client config class
            // eg: AmazonS3Client -> AmazonS3Config
            var clientConfigTypeName = serviceClientType.FullName.Replace("Client", "Config");
            var clientConfigType = serviceClientType.Assembly.GetType(clientConfigTypeName);

            // Instantiate (eg AmazonS3Config)
            return Activator.CreateInstance(clientConfigType) as ClientConfig;
        }
    }
}
