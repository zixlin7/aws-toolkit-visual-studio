using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CommonUI.Notifications;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Install
{
    /// <summary>
    /// Manages the toolkit installation of the Language server
    /// </summary>
    public interface ILspInstaller
    {
        /// <summary>
        /// Installs the Lsp and returns information about the installation <see cref="LspInstallResult"/>
        /// </summary>
        /// <param name="notifier"></param>
        /// <param name="token"></param>
        Task<LspInstallResult> ExecuteAsync(ITaskStatusNotifier notifier, CancellationToken token = default);
    }
}
