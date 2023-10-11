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
        /// Installs the Lsp and returns the path where it is installed
        /// </summary>
        /// <param name="notifier"></param>
        /// <param name="token"></param>
        Task<string> ExecuteAsync(ITaskStatusNotifier notifier, CancellationToken token = default);
    }
}
