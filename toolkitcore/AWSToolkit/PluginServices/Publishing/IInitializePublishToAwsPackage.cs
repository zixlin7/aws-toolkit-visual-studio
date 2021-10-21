using System;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Shared;

namespace Amazon.AWSToolkit.PluginServices.Publishing
{
    /// <summary>
    /// Exposes the <see cref="IInitializePublishToAwsPackage"/> service
    /// across VS Packages.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public interface SInitializePublishToAwsPackage
    {
        
    }

    /// <summary>
    /// Interface for the service that initializes the Publish to AWS Package
    /// </summary>
    public interface IInitializePublishToAwsPackage
    {
        event EventHandler<EventArgs> Initialize;

        Task InitializePackage(ToolkitContext toolkitContext, IAWSToolkitShellProvider shellProvider);
    }
}
