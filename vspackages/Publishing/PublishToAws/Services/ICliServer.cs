using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Runtime;

using AWS.Deploy.ServerMode.Client;

namespace Amazon.AWSToolkit.Publish.Services
{
    /// <summary>
    /// Exposes the <see cref="ICliServer"/> service
    /// to the PublishToAws VS Package.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public interface SCliServer
    {
        
    }

    /// <summary>
    /// Interface for the VS service that manages the aws deploy cli server mode
    /// </summary>
    public interface ICliServer
    {
        /// <summary>
        /// Fires when the service does not appear to be available (but should be)
        /// </summary>
        event EventHandler Disconnect;

        /// <summary>
        /// Ensures one instance of the service is started
        /// </summary>
        Task StartAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Stops the running instance of the service
        /// </summary>
        void Stop();

        /// <summary>
        /// Produces a REST Client against the currently running service
        /// </summary>
        /// <param name="credentialsGenerator">Resolves credentials prior to each request</param>
        /// <returns>Rest Client, null on error or if service is not running</returns>
        IRestAPIClient GetRestClient(Func<Task<AWSCredentials>> credentialsGenerator);

        /// <summary>
        /// Produces a Deployment Communication SignalR Client against the currently running service
        /// </summary>
        /// <returns>Deployment Client, null on error or if service is not running</returns>
        IDeploymentCommunicationClient GetDeploymentClient();
    }
}
