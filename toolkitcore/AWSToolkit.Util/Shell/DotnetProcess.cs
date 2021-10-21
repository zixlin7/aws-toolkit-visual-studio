using System.Diagnostics;

namespace Amazon.AWSToolkit.Shell
{
    public class DotnetProcess
    {
        public static Process CreateHeadless(string arguments)
        {
            return HeadlessProcess.Create("dotnet.exe", arguments);
        }
    }
}
