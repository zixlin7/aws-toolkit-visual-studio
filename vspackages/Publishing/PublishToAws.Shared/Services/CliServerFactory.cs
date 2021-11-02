using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish.Install;
using Amazon.AWSToolkit.Publish.PublishSetting;

using AWS.Deploy.ServerMode.Client;

namespace Amazon.AWSToolkit.Publish.Services
{
    public class CliServerFactory
    {
        public static async Task<CliServer> CreateAsync(InstallOptions installOptions, IPublishSettingsRepository settingsRepository)
        {
            var settings = await settingsRepository.GetAsync();
            return new CliServer(CreateServerModeSession(installOptions, settings.DeployServer));
        }

        private static ServerModeSession CreateServerModeSession(InstallOptions installOptions, DeployServerSettings settings)
        {
            var portRange = settings.PortRange;
            return new ServerModeSession(diagnosticLoggingEnabled: false, deployToolPath: installOptions.GetCliInstallPath(),
                startPort: portRange.Start, endPort: portRange.End);
        }
    }
}
