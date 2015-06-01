using System;
using System.Text;
using System.IO;
using System.IO.Pipes;

namespace AWSDeploymentHostManagerApp
{
    public static class PipeHelper
    {
        public const int BufferSize = 10000;
        public const int ChunkSize = 500;
        public static void WriteToPipe(StreamWriter sw, String value)
        {
            try
            {
                if (value == null) value = String.Empty;
                int numLines = (value.Length / ChunkSize) + ((value.Length % ChunkSize) == 0 ? 0 : 1);
                sw.WriteLine(numLines);
                int pos = 0;
                for (int i = 0; i < numLines; i++)
                {
                    sw.WriteLine(value.Substring(pos, ((pos + 500) < value.Length) ? ChunkSize : value.Length - pos));
                    sw.Flush();
                    ((PipeStream)sw.BaseStream).WaitForPipeDrain();
                    pos += 500;
                }
                sw.Flush();
            }
            catch (IOException ioe)
            {
                if (ioe.Message.Equals("Pipe is broken.", StringComparison.InvariantCultureIgnoreCase))
                {
                    HostManagerApp.ResetPipe();
                    HostManagerApp.hostManager = HostManagerApp.StartHostManager(HostManagerApp.configFilePath);
                }
                throw;
            }
            catch (Exception e)
            {
                HostManagerApp.LOGGER.Error("Error writing to pipe", e);
                throw;
            }
        }
        public static string ReadFromPipe(StreamReader sr)
        {
            try
            {
                int numLines = int.Parse(sr.ReadLine());
                StringBuilder sb = new StringBuilder(ChunkSize * numLines);
                for (int i = 0; i < numLines; i++)
                {
                    sb.Append(sr.ReadLine());
                }
                return sb.ToString();
            }
            catch (Exception e)
            {
                HostManagerApp.LOGGER.Error("Error reading from pipe", e);
                throw;
            }
        }
    }
}
