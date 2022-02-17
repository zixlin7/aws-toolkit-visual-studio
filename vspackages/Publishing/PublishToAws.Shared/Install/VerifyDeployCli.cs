using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Shell;

using log4net;

using Microsoft.VisualStudio.Threading;

namespace Amazon.AWSToolkit.Publish.Install
{
    /// <summary>
    /// Responsible for verifying the aws.deploy.cli nuget package
    /// </summary>
    public class VerifyDeployCli
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(VerifyDeployCli));

        private readonly InstallOptions _options;

        public VerifyDeployCli(InstallOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// Execute the dotnet command for verification of the nuget package
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>true/false based on success or error respectively</returns>
        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var process =
                DotnetProcess.CreateHeadless($"nuget verify \"{_options.ToolPath}\\**\\aws.deploy.cli.*.nupkg\" --all");
            process.Start();

            var data = await RetrieveProcessDataAsync(process);
            RecordProcessLogs(data);

            var exitCode = await process.WaitForExitAsync(cancellationToken);
            HandleProcessExit(data, exitCode);
        }

        private async Task<ProcessData> RetrieveProcessDataAsync(Process process)
        {
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            return new ProcessData(output, error);
        }

        private void RecordProcessLogs(ProcessData processData)
        {
            new ProcessLogger(processData, Logger).Record();
        }

        private void HandleProcessExit(ProcessData data, int exitCode)
        {
            new ProcessExitHandler(data, exitCode).Execute();
        }
    }
}
