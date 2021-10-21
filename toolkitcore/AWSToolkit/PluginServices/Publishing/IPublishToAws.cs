using System.Threading.Tasks;

namespace Amazon.AWSToolkit.PluginServices.Publishing
{
    /// <summary>
    /// Exposes the <see cref="IPublishToAws"/> service
    /// across VS Packages.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public interface SPublishToAws
    {

    }

    /// <summary>
    /// Interface for the VS service that manages the Publishes to AWS functionality
    /// </summary>
    public interface IPublishToAws
    {
        Task ShowPublishToAwsDocument(ShowPublishToAwsDocumentArgs args);
    }
}
