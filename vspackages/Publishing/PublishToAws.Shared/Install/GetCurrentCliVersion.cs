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
            var lines = RemoveEmptyLinesFrom(output.Split(Environment.NewLine.ToCharArray()));

            var versionLine = lines.Last();

            var installedVersion = ParseVersionFromVersionLine(versionLine);

            return NuGetVersion.Parse(installedVersion);
        }

        private IEnumerable<string> RemoveEmptyLinesFrom(IEnumerable<string> lines)
        {
            return lines.Where(line => !string.IsNullOrWhiteSpace(line));
        }

        // Version line has the following format:
        // 0.11.16+4c541282dc
        private string ParseVersionFromVersionLine(string versionLine)
        {
            return versionLine.Split('+')[0];
        }
    }
}
