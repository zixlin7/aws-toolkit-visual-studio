using System.Threading;
using System.Threading.Tasks;

using log4net;

namespace Amazon.AWSToolkit.Publish.Install
{
    /// <summary>
    /// Initializes the aws.deploy.cli including installation and verification
    /// </summary>
    public class DeployCli
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(DeployCli));

        private readonly InstallOptions _options;

        public DeployCli(InstallOptions options)
        {
            _options = options;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await InstallDeployCliAsync(cancellationToken);
            await VerifyDeployCliAsync(cancellationToken);
            Logger.Debug("Finished initializing AWS.Deploy.CLI");
        }

        private async Task InstallDeployCliAsync(CancellationToken cancellationToken)
        {
            var installer = DeployCliInstallerFactory.Create(_options);
            await installer.InstallAsync(cancellationToken);
        }

        private async Task VerifyDeployCliAsync(CancellationToken cancellationToken)
        {
            var verifyDeployCli = new VerifyDeployCli(_options);
            await verifyDeployCli.ExecuteAsync(cancellationToken);
        }
    }
}
