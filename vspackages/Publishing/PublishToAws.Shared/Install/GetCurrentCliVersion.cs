using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Shell;

using Microsoft.VisualStudio.Threading;

using NuGet.Versioning;

namespace Amazon.AWSToolkit.Publish.Install
{
    public class GetCurrentCliVersion
    {
        public static class ErrorMessages
        {
            public const string VersionOutputEmpty = "Unable to determine deploy tool version, no output was detected.";
        }

        private readonly InstallOptions _options;

        public GetCurrentCliVersion(InstallOptions options)
        {
            _options = options;
        }

        public async Task<NuGetVersion> ExecuteAsync(CancellationToken cancellationToken)
        {
            var process = HeadlessProcess.Create(_options.GetCliInstallPath(), "--version");
            process.Start();
            await process.WaitForExitAsync(cancellationToken);

            var output = await process.StandardOutput.ReadToEndAsync();
            return ParseVersionOutOfCliOutput(output);
        }

        private NuGetVersion ParseVersionOutOfCliOutput(string output)
        {
            var installedVersion = ParseVersionFromCliOutput(output);

            return NuGetVersion.Parse(installedVersion);
        }

        public static string ParseVersionFromCliOutput(string output)
        {
            if (string.IsNullOrWhiteSpace(output))
            {
                throw new DeployToolException(ErrorMessages.VersionOutputEmpty);
            }

            var lines = output
                .Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Where(IsNotEmpty)
                .ToList();

            if (!lines.Any())
            {
                throw new DeployToolException(ErrorMessages.VersionOutputEmpty);
            }

            var versionLine = lines.Last();

            var installedVersion = ParseVersionFromVersionLine(versionLine);
            return installedVersion;
        }

        private static bool IsNotEmpty(string text) => !string.IsNullOrWhiteSpace(text);

        // Version line has the following format:
        // 0.11.16+4c541282dc
        private static string ParseVersionFromVersionLine(string versionLine)
        {
            return versionLine.Split('+')[0];
        }
    }
}
