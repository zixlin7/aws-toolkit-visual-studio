using System.Threading;
using System.Threading.Tasks;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Install
{
    /// <summary>
    /// Interface managing lifecycle of a certain resource eg. LSP Version Manifest/ LSP
    /// This class will be extended to expose methods to delete/de-list resources as well eg. de-listing lsp versions
    /// </summary>
    public interface IResourceManager<T> where T : class
    {
        /// <summary>
        /// Downloads the resource being managed eg. LSP version manifest
        /// </summary>
        /// <returns></returns>
        Task<T> DownloadAsync(CancellationToken token = default);
    }
}
