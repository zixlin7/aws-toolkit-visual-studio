using System.Threading;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Amazon.AWSToolkit.Publish.Package
{
    /// <summary>
    /// Interface around <see cref="PublishToAwsPackage"/> which provides
    /// mocking points to code that accesses the package.
    ///
    /// This interface is intended to contain only what is accessed
    /// by consumer code.
    /// </summary>
    public interface IPublishToAwsPackage : IAsyncServiceProvider
    {
        CancellationToken DisposalToken { get; }
        JoinableTaskFactory JoinableTaskFactory { get; }
    }
}
