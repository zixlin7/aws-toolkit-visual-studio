using System.Diagnostics;

namespace Amazon.AWSToolkit.Shell
{
    public class HeadlessProcess
    {
        public static Process Create(string filename, string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = filename,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            return new Process() { StartInfo = startInfo };
        } 
    }
}
