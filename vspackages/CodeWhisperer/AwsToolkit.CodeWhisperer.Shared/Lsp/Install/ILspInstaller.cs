using System.Threading;
using System.Threading.Tasks;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Install
{
    /// <summary>
    /// Manages the toolkit installation of the Language server
    /// </summary>
    public interface ILspInstaller
    {
        /// <summary>
        /// Installs the Lsp and returns the path where it is installed
        /// </summary>
        /// <param name="token"></param>
        Task<string> ExecuteAsync(CancellationToken token = default);
    }
}
