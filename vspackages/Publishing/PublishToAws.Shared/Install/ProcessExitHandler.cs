using System;

namespace Amazon.AWSToolkit.Publish.Install
{
    /// <summary>
    /// Handles process's data and exit code
    /// </summary>
    public class ProcessExitHandler
    {
        private readonly ProcessData _processData;
        private readonly int _exitCode;

        public ProcessExitHandler(ProcessData data, int exitCode)
        {
            _processData = data;
            _exitCode = exitCode;
        }

        public void Execute()
        {
            if (ContainsVerifyError())
            {
                throw new InvalidOperationException(
                    $"AWS Toolkit was unable to verify the contents of the aws.deploy.cli tool.{Environment.NewLine}You might need to install .NET 5 or newer.");
            }

            if (_exitCode != 0)
            {
                throw new InvalidOperationException(
                    $"AWS Toolkit was unable to verify the contents of the aws.deploy.cli tool.{Environment.NewLine}Restart Visual Studio to try again.");
            }
        }

        private bool ContainsVerifyError()
        {
            var errorText = "Unrecognized command or argument 'verify'";
            if (_processData.Output.Contains(errorText) || _processData.Error.Contains(errorText))
            {
                return true;
            }

            return false;
        }
    }
}
