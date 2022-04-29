using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish.Util;
using Amazon.AWSToolkit.Shell;

using Microsoft.VisualStudio.Threading;

namespace Amazon.AWSToolkit.Publish.Install
{
    public class InstallDeployCli
    {
        private readonly InstallOptions _options;

        public InstallDeployCli(InstallOptions options)
        {
            _options = options;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var process = DotnetProcess.CreateHeadless($"tool update --tool-path \"{_options.ToolPath}\" --version {_options.VersionRange} {PublishToAwsConstants.DeployToolPackageName}");
            process.Start();
            await process.WaitForExitAsync(cancellationToken);
        }
    }
}
