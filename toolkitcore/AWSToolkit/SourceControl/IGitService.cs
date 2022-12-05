using System;
using System.Threading;
using System.Threading.Tasks;

namespace Amazon.AWSToolkit.SourceControl
{
    public interface IGitService
    {
        Task CloneAsync(Uri remoteUri, string localPath, bool recurseSubmodules = false, CancellationToken cancellationToken = default);

        string GetDefaultRepositoryPath();
    }
}
