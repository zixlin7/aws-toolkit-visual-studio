using System.Threading;
using System.Threading.Tasks;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Install
{
    /// <summary>
    /// Manages the toolkit installation of the Language server
    /// </summary>
    public interface ILspInstaller
    {
        Task ExecuteAsync(CancellationToken token = default);
    }
}
