using System.Diagnostics;
using log4net;

namespace Amazon.AWSToolkit.EC2.ConnectionUtils
{
    public class SSHUtil
    {
        public const string PUTTY_EXECUTABLE = "Putty.exe";

        static readonly ILog LOGGER = LogManager.GetLogger(typeof(SSHUtil));
        const string CONVERTING_APP = "PemToPPKConverter.exe";

        public static void ConnectWithPrivateKey(string computer, string username, string rsaPrivateKey)
        {
            string ppkFile = ToolsUtil.WritePEMToPPKFile(rsaPrivateKey);
            string puttyLocation = GetPuttyLocation();

            Process convertProc = new Process();
            convertProc.StartInfo.FileName = puttyLocation;
            convertProc.StartInfo.Arguments = string.Format("-ssh {0}@{1} -i \"{2}\"", username, computer, ppkFile);
            convertProc.Start();

            ToolsUtil.SetupThreadToDeleteFile(ppkFile);

        }

        public static void Connect(string computer, string username, string password)
        {
            Process convertProc = new Process();
            convertProc.StartInfo.FileName = GetPuttyLocation();
            convertProc.StartInfo.Arguments = string.Format("-ssh {0}@{1} -pw {2}", username, computer, password);
            convertProc.Start();
        }

        public static string GetPuttyLocation()
        {
            return ToolsUtil.FindTool(PUTTY_EXECUTABLE);
        }
    }
}
