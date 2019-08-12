using System.Diagnostics;
using log4net;


namespace Amazon.AWSToolkit.EC2.ConnectionUtils
{
    public class SCPUtil
    {
        public const string WINSCP_EXECUTABLE = "WinSCP.exe";

        static readonly ILog LOGGER = LogManager.GetLogger(typeof(SSHUtil));

        public static void ConnectWithPrivateKey(string computer, string username, string rsaPrivateKey)
        {
            string ppkFile = ToolsUtil.WritePEMToPPKFile(rsaPrivateKey);
            string puttyLocation = GetWinSCPLocation();

            Process convertProc = new Process();
            convertProc.StartInfo.FileName = puttyLocation;
            convertProc.StartInfo.Arguments = string.Format("scp://{0}@{1} /privatekey=\"{2}\"", username, computer, ppkFile);
            convertProc.Start();

            ToolsUtil.SetupThreadToDeleteFile(ppkFile);

        }

        public static void Connect(string computer, string username, string password)
        {
            Process convertProc = new Process();
            convertProc.StartInfo.FileName = GetWinSCPLocation();
            convertProc.StartInfo.Arguments = string.Format("scp://{0}:{2}@{1}", username, computer, password);
            convertProc.Start();
        }

        public static string GetWinSCPLocation()
        {
            return ToolsUtil.FindTool(WINSCP_EXECUTABLE);
        }
    }
}
