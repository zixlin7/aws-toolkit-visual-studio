using System;
using System.Diagnostics;

using Amazon.Runtime;

using log4net;

namespace Amazon.AWSToolkit.Clients
{
    /// <summary>
    /// Utility methods to help with the instantiation of AWS Service Client classes.
    /// </summary>
    public static class ServiceClientCreator
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ServiceClientCreator));

        /// <summary>
        /// Uses reflection to locate and instantiate a service client from credentials and a region endpoint.
        ///
        /// This is the typical client creation approach, leveraging the SDK to provide service Url details.
        /// </summary>
        /// <param name="serviceClientType">A service client class to create. The class is expected to contain the required constructor.</param>
        /// <param name="credentials">Credentials to instantiate the service client with</param>
        /// <param name="regionEndpoint">Region Endpoint details to instantiate the service client with</param>
        /// <returns>A created service client, or null of there is an error</returns>
        public static IAmazonService CreateServiceClient(Type serviceClientType, AWSCredentials credentials, RegionEndpoint regionEndpoint)
        {
            try
            {
                AssertValidServiceClient(serviceClientType);

                var constructor = serviceClientType.GetConstructor(new[] { typeof(AWSCredentials), typeof(RegionEndpoint) });
                if (constructor == null)
                {
                    throw new Exception("No constructor found that accepts AWSCredentials and RegionEndpoint");
                }

                return constructor.Invoke(new object[] { credentials, regionEndpoint }) as IAmazonService;
            }
            catch (Exception e)
            {
                Logger.Error($"Error creating service client: {serviceClientType.FullName}", e);
                ShowDebugAssert(serviceClientType.FullName, e);
                return null;
            }
        }

        /// <summary>
        /// Uses reflection to locate and instantiate a service client from credentials and a client configuration.
        /// 
        /// This is used in client creation scenarios where we want to customize the client (for example, to
        /// use local endpoints).
        /// </summary>
        /// <param name="serviceClientType">A service client class to create. The class is expected to contain the required constructor.</param>
        /// <param name="credentials">Credentials to instantiate the service client with</param>
        /// <param name="serviceClientConfig">Client configuration to instantiate the service client with</param>
        /// <returns>A created service client, or null of there is an error</returns>
        public static IAmazonService CreateServiceClient(Type serviceClientType, AWSCredentials credentials, ClientConfig serviceClientConfig)
        {
            try
            {
                AssertValidServiceClient(serviceClientType);

                var constructor = serviceClientType.GetConstructor(new[] {typeof(AWSCredentials), serviceClientConfig.GetType()});
                if (constructor == null)
                {
                    throw new Exception($"No constructor found that accepts AWSCredentials and {serviceClientConfig.GetType().Name}");
                }

                return constructor.Invoke(new object[] {credentials, serviceClientConfig}) as IAmazonService;
            }
            catch (Exception e)
            {
                Logger.Error($"Error creating service client: {serviceClientType.FullName}", e);
                ShowDebugAssert(serviceClientType.FullName, e);
                return null;
            }
        }

        /// <summary>
        /// Uses reflection to locate and instantiate a service client from a client configuration.
        /// 
        /// This is used in client creation scenarios where we want to create a client without
        /// specific credentials (for example, to use local endpoints in DynamoDB Local).
        /// </summary>
        /// <param name="serviceClientType">A service client class to create. The class is expected to contain the required constructor.</param>
        /// <param name="serviceClientConfig">Client configuration to instantiate the service client with</param>
        /// <returns>A created service client, or null of there is an error</returns>
        public static IAmazonService CreateServiceClient(Type serviceClientType, ClientConfig serviceClientConfig)
        {
            try
            {
                AssertValidServiceClient(serviceClientType);

                var constructor = serviceClientType.GetConstructor(new[] {serviceClientConfig.GetType()});
                if (constructor == null)
                {
                    throw new Exception($"No constructor found that accepts AWSCredentials and {serviceClientConfig.GetType().Name}");
                }

                return constructor.Invoke(new object[] {serviceClientConfig}) as IAmazonService;
            }
            catch (Exception e)
            {
                Logger.Error($"Error creating service client: {serviceClientType.FullName}", e);
                ShowDebugAssert(serviceClientType.FullName, e);
                return null;
            }
        }

        /// <summary>
        /// Add runtime checks to help identify when an incorrect service client type has been requested
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        private static void AssertValidServiceClient(Type serviceClientType)
        {
            if (serviceClientType.IsInterface)
            {
                throw new InvalidOperationException($"Unable to create service clients from instance types: {serviceClientType.FullName}");
            }

            if (serviceClientType.IsAbstract)
            {
                throw new InvalidOperationException($"Unable to create service clients from abstract classes: {serviceClientType.FullName}");
            }

            if (!typeof(IAmazonService).IsAssignableFrom(serviceClientType))
            {
                throw new InvalidOperationException($"Service clients are expected to implement {typeof(IAmazonService)}: {serviceClientType.FullName}");
            }
        }

        private static void ShowDebugAssert(string clientName, Exception exception)
        {
            // Because we are creating service clients with reflection, we don't get compiler checks
            // to see that the type has the expected constructor. Show a debug message to improve
            // the likelihood we see the problem during development.
#if DEBUG
            Debug.Assert(!Debugger.IsAttached,
                $"Toolkit bug trying to create {clientName}. This will silently fail for users. Check that the correct class (not interface) has been requested.",
                exception.Message);
#endif
        }
    }
}
