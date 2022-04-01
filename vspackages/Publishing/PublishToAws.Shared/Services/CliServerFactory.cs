using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish.Install;
using Amazon.AWSToolkit.Publish.PublishSetting;
using Amazon.AWSToolkit.Shared;

using AWS.Deploy.ServerMode.Client;

namespace Amazon.AWSToolkit.Publish.Services
{
    public class CliServerFactory
    {
        public static async Task<CliServer> CreateAsync(InstallOptions installOptions,
            IPublishSettingsRepository settingsRepository, IAWSToolkitShellProvider toolkitHost)
        {
            var settings = await settingsRepository.GetAsync();
            return new CliServer(CreateServerModeSession(installOptions, settings.DeployServer, toolkitHost));
        }

        private static ServerModeSession CreateServerModeSession(InstallOptions installOptions,
            DeployServerSettings settings, IAWSToolkitShellProvider toolkitHost)
        {
            var portRange = settings.PortRange;
            var deployToolPath = GetDeployToolPath(installOptions, settings, toolkitHost);
            var diagnosticLoggingEnabled = GetDiagnosticLoggingEnabled(settings, toolkitHost);

            //TODO: Pass additional arguments when Server mode client is updated
            var additionalArguments = GetAdditionalArguments(settings, toolkitHost);

            return new ServerModeSession(diagnosticLoggingEnabled: diagnosticLoggingEnabled,
                deployToolPath: deployToolPath,
                startPort: portRange.Start, endPort: portRange.End);
        }

        private static string GetDeployToolPath(InstallOptions installOptions, DeployServerSettings settings,
            IAWSToolkitShellProvider toolkitHost)
        {
            var deployToolPath = installOptions.GetCliInstallPath();
            if (!string.IsNullOrEmpty(settings.AlternateCliPath))
            {
                deployToolPath = settings.AlternateCliPath;
                toolkitHost.OutputToHostConsole($"Publish to AWS is using a deploy tool from an alternate location: {settings.AlternateCliPath}", true);
            }

            return deployToolPath;
        }

        private static string GetAdditionalArguments(DeployServerSettings settings,
            IAWSToolkitShellProvider toolkitHost)
        {
            if (!string.IsNullOrEmpty(settings.AdditionalArguments))
            {
                toolkitHost.OutputToHostConsole($"Publish to AWS is using a deploy tool with additional command-line arguments: {settings.AdditionalArguments}", true);
                return settings.AdditionalArguments;
            }

            return string.Empty;
        }

        private static bool GetDiagnosticLoggingEnabled(DeployServerSettings settings,
            IAWSToolkitShellProvider toolkitHost)
        {
            if (settings.LoggingEnabled)
            {
                toolkitHost.OutputToHostConsole("Diagnostic logging has been enabled for Publish to AWS", true);
            }

            return settings.LoggingEnabled;
        }
    }
}
