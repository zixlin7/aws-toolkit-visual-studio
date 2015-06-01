using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.ServiceProcess;
using System.Threading;
using System.Web;

using System.IO;
using System.IO.Pipes;
using System.Diagnostics;

using log4net;


namespace AWSDeploymentHostManagerApp
{
    public enum HostManagerStatus
    {
        Queue = 0,
        Running = 1,
        Stopping = 2
    }

    public class HostManagerApp
    {
        public static ILog LOGGER = LogManager.GetLogger(typeof(HostManagerApp));
        private const string HOSTMANAGER_DIR_PATTERN = "HostManager*";
        internal static string configFilePath;
        internal static Process[] alreadyPresentHostManagers;
        internal static Process hostManager;
        private static StreamWriter hmPipeWriter;
        private static NamedPipeServerStream hmPipe = null;
        private static String hmPipeName = null;
        internal static HostManagerStatus hmStatus = HostManagerStatus.Queue;

        private enum CustomCommand
        {
            StartHostManager = 128
        }

        static void Main(string[] args)
        {
            LOGGER.Info("Starting Listener");
            configFilePath = @"hm.config";
            if (args.Length > 0 && args[0] != null && args[0].Length > 0)
                configFilePath = args[0];

            ResetPipe();
            
            hostManager = StartHostManager(configFilePath);
            HttpListener listener = new HttpListener();

            try
            {
                listener.Prefixes.Add("http://+:80/_hostmanager/");
            }
            catch
            {
                LOGGER.Error("To get this program to run without administration privileges, run the following command as administrator:");
                LOGGER.Error(@"   netsh http add urlacl url=http://+:80/_hostmanager/ user=$DOMAIN\$USER");

                LogManager.Shutdown();

                Environment.Exit(1);
            }

            try
            {
                LOGGER.Info("Starting listener");
                listener.Start();


                for (; ; )
                {
                    HttpListenerContext context = listener.GetContext();
                    ThreadPool.QueueUserWorkItem(new RequestHandler(context, hmPipeWriter).ProcessRequest);
                }
            }
            catch (Exception e)
            {
                LOGGER.Fatal("Fatal error listening for requests", e);
            }

            LogManager.Shutdown();
        }

        /// <summary>
        /// Returns latest folder based on naming convention where date/time
        /// of release (in yyyy-MM-ddTHH:mm:ssZ format) is appended to 'HostManager' 
        /// folder name.
        /// </summary>
        /// <param name="basePath">
        /// The full path to the folder containing HostManager.* deployment folders
        /// </param>
        /// <returns>Full path to the latest available HostManager release</returns>
        public static string LatestHostManagerDirectory(string basePath)
        {
            string latestHMDirectory =
                new DirectoryInfo(basePath)
                        .GetDirectories(HOSTMANAGER_DIR_PATTERN)
                        .OrderByDescending(f => f.Name)
                        .First().Name;

            return Path.Combine(basePath, latestHMDirectory);
        }
        internal static Process StartHostManager(string configFilePath)
        {
            Process hmProcess;

            string configuration;
            if (File.Exists(configFilePath))
            {
                TextReader tr = new StreamReader(configFilePath);
                configuration = tr.ReadToEnd();
                tr.Close();
            }
            else
            {
                configuration = null;
            } 


            string loc = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string hmWorkingDirectory = LatestHostManagerDirectory(Path.GetDirectoryName(loc));
            alreadyPresentHostManagers = Process.GetProcessesByName("AWDeploymentHostManager");
            CallMagicHarp();
            hmProcess = FindProcess();
            ((NamedPipeServerStream)hmPipeWriter.BaseStream).WaitForConnection();

            lock (RequestHandler.hmPipeLock)
            {
                PipeHelper.WriteToPipe(hmPipeWriter, configuration);
            }
            ((NamedPipeServerStream)hmPipeWriter.BaseStream).WaitForPipeDrain();
            hmStatus = HostManagerStatus.Running;
            return hmProcess;
        }
        private static void CallMagicHarp()
        {
            ServiceController controller = new ServiceController("Magic Harp");

            if (controller.Status != ServiceControllerStatus.Running)
            {
                LOGGER.Info("Magic Harp was not running, waiting for Magic Harp to start");
                controller.WaitForStatus(ServiceControllerStatus.Running);
            }

            controller.ExecuteCommand((int)CustomCommand.StartHostManager);
        }
        private static Process FindProcess()
        {
            Process[] candidates = null;
            Process ret = null;
            while (ret == null)
            {
                candidates = Process.GetProcessesByName("AWSDeploymentHostManager");
                ret = candidates.Where(HostManagerApp.NotAlreadyPresent).FirstOrDefault();
            }
            return ret;
        }
        private static bool NotAlreadyPresent(Process proc)
        {
            bool ret = true;
            foreach (Process p in alreadyPresentHostManagers)
            {
                if (p.Id == proc.Id)
                {
                    ret = false;
                    break;
                }
            }
            return ret;
        }

        internal static void ResetPipe()
        {
            try
            {
                if(hmPipe != null)
                    hmPipe.Disconnect();
            }
            catch (Exception e)
            {
                LOGGER.Info("Pipe failed to get disconnected", e);
            }

            string loc = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string hmWorkingDirectory = LatestHostManagerDirectory(Path.GetDirectoryName(loc));
            string pipeName = hmWorkingDirectory.Substring(hmWorkingDirectory.LastIndexOf('\\') + 1);
            LOGGER.InfoFormat("Pipe reset, creating pipe {0}", pipeName);

            hmPipeName = pipeName;
            hmPipe = new NamedPipeServerStream(pipeName, PipeDirection.Out, 1, PipeTransmissionMode.Message, PipeOptions.WriteThrough, PipeHelper.BufferSize, PipeHelper.BufferSize);
            hmPipeWriter = new StreamWriter(hmPipe, Encoding.UTF8, PipeHelper.BufferSize);
        }
    }

    class RequestHandler
    {
        private HttpListenerContext listenerContext;
        private StreamWriter hmPipeWriter;
        internal static object hmPipeLock = new object();
        private Random rnd = new Random();
        
        private const string
            HEALTHCHECK_RESPONSE_OK = "<healthcheck><status>ok</status></healthcheck>",
            HEALTHCHECK_RESPONSE_FAIL = "<healthcheck><status>failed</status></healthcheck>",
            HEALTHCHECK_RESPONSE_UNKNOWN = "<healthcheck><status>unknown</status></healthcheck>",
            VERSIONCHECK_RESPONSE_FORMAT = "<versioncheck><version>{0}</version></versioncheck>";

        public RequestHandler(HttpListenerContext context, StreamWriter hmPipeWriter)
        {
            this.listenerContext = context;
            this.hmPipeWriter = hmPipeWriter;
        }

        public void ProcessRequest(object state)
        {
            HostManagerApp.LOGGER.Debug("Starting request");
            try
            {
                HostManagerApp.LOGGER.DebugFormat("Starting Process Request");

                byte[] response = null;

                if (listenerContext.Request.RawUrl.Contains("/tasks"))
                {
                    TextReader requestReader = new StreamReader(listenerContext.Request.InputStream);
                    string requestBody = requestReader.ReadToEnd().Trim();
                    requestReader.Close();

                    HostManagerApp.LOGGER.DebugFormat("Initial request body: {0}", requestBody);
                    requestBody = HttpUtility.UrlDecode(requestBody);
                    HostManagerApp.LOGGER.DebugFormat("URL decoded request body: {0}", requestBody);
                    int posFirstBracket = requestBody.IndexOf("{");
                    if (posFirstBracket > 0)
                    {
                        requestBody = requestBody.Substring(posFirstBracket);
                        HostManagerApp.LOGGER.DebugFormat("Prefix removed request body: {0}", requestBody);
                    }

                    string responseBody = GetResponseFromHostManager(requestBody);

                    HostManagerApp.LOGGER.DebugFormat("Response body: {0}", responseBody);
                    response = Encoding.UTF8.GetBytes(responseBody);

                    listenerContext.Response.ContentLength64 = response.Length;
                    listenerContext.Response.OutputStream.Write(response, 0, response.Length);
                    listenerContext.Response.OutputStream.Close();
                }
                else if (listenerContext.Request.RawUrl.Contains("/healthcheck"))
                {
                    int status = 0;

                    if (Int32.TryParse(GetResponseFromHostManager("healthcheck"), out status) && status > 0)
                    {
                        if (status == 200)
                        {
                            response = Encoding.UTF8.GetBytes(HEALTHCHECK_RESPONSE_OK);
                        }
                        else
                        {
                            response = Encoding.UTF8.GetBytes(HEALTHCHECK_RESPONSE_FAIL);
                            listenerContext.Response.StatusCode = status;
                        }
                    }
                    else
                    {
                        response = Encoding.UTF8.GetBytes(HEALTHCHECK_RESPONSE_UNKNOWN);
                        listenerContext.Response.StatusCode = 500;
                    }

                    listenerContext.Response.ContentLength64 = response.Length;
                    listenerContext.Response.OutputStream.Write(response, 0, response.Length);
                    listenerContext.Response.OutputStream.Close();
                }
                else if (listenerContext.Request.RawUrl.Contains("/hostmanagercheck"))
                {
                    string strStatus = GetResponseFromHostManager("statuscheck");
                    HostManagerStatus newStatus = HostManagerStatus.Running;
                    if (Enum.TryParse<HostManagerStatus>(strStatus, out newStatus))
                    {
                        HostManagerApp.hmStatus = newStatus;
                    }
                    else
                    {
                        HostManagerApp.LOGGER.WarnFormat("Got Unknow status from HostManager {0} ignoring", strStatus);
                        return;
                    }

                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("<html><body><H1>AWS Deployment HostManager<H1></body></html>");
                    response = Encoding.UTF8.GetBytes(sb.ToString());

                    listenerContext.Response.ContentLength64 = response.Length;
                    listenerContext.Response.OutputStream.Write(response, 0, response.Length);
                    listenerContext.Response.OutputStream.Close();

                    switch (HostManagerApp.hmStatus)
                    {
                        case HostManagerStatus.Queue:
                            lock (hmPipeLock)
                            {
                                PipeHelper.WriteToPipe(hmPipeWriter, "done");
                            }
                            HostManagerApp.ResetPipe();
                            HostManagerApp.hostManager = HostManagerApp.StartHostManager(HostManagerApp.configFilePath);
                            break;
                        case HostManagerStatus.Stopping:
                            lock (hmPipeLock)
                            {
                                PipeHelper.WriteToPipe(hmPipeWriter, "done");
                                HostManagerApp.hostManager.WaitForExit();
                            }
                            Environment.Exit(0);
                            break;
                        case HostManagerStatus.Running:
                            //nothing to do in this case.
                            break;
                        default:
                            HostManagerApp.LOGGER.WarnFormat("HostManager in unknown state: {0}", HostManagerApp.hmStatus);
                            break;
                    }
                }
                else if (listenerContext.Request.RawUrl.Contains("/versioncheck"))
                {
                    string formattedResponse = String.Format(VERSIONCHECK_RESPONSE_FORMAT, GetResponseFromHostManager("versioncheck"));
                    response = Encoding.UTF8.GetBytes(formattedResponse);

                    listenerContext.Response.ContentLength64 = response.Length;
                    listenerContext.Response.OutputStream.Write(response, 0, response.Length);
                    listenerContext.Response.OutputStream.Close();
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("<html><body><H1>AWS Deployment HostManager<H1></body></html>");
                    response = Encoding.UTF8.GetBytes(sb.ToString());

                    listenerContext.Response.ContentLength64 = response.Length;
                    listenerContext.Response.OutputStream.Write(response, 0, response.Length);
                    listenerContext.Response.OutputStream.Close();
                }
            }
            catch (Exception e)
            {
                HostManagerApp.LOGGER.Error("Error processing request", e);
            }
            finally
            {
                HostManagerApp.LOGGER.Debug("Finished request");
            }
        }

        public string GetResponseFromHostManager(string request)
        {
            string responseBody;
            try
            {
                string taskPipeName = String.Format("hostmanager_{0}_{1}", DateTime.UtcNow.Ticks.ToString(), rnd.Next().ToString("X8"));
                using (NamedPipeServerStream taskPipe = new NamedPipeServerStream(taskPipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.WriteThrough, PipeHelper.BufferSize, PipeHelper.BufferSize))
                {
                    bool sent = false;
                    while (!sent)
                    {
                        while (HostManagerApp.hmStatus != HostManagerStatus.Running)
                        {
                            Thread.Sleep((int)TimeSpan.FromSeconds(1).TotalMilliseconds);
                        }
                        lock (hmPipeLock)
                        {
                            if (HostManagerApp.hmStatus == HostManagerStatus.Running)
                            {
                                PipeHelper.WriteToPipe(hmPipeWriter, taskPipeName);
                                sent = true;
                            }
                        }
                    }
                    taskPipe.WaitForConnection();

                    StreamWriter sw = new StreamWriter(taskPipe, Encoding.UTF8, PipeHelper.BufferSize);
                    PipeHelper.WriteToPipe(sw, request);
                    taskPipe.WaitForPipeDrain();

                    StreamReader sr = new StreamReader(taskPipe, Encoding.UTF8, false, PipeHelper.BufferSize);
                    responseBody = PipeHelper.ReadFromPipe(sr);
                }
            }
            catch (IOException ioe)
            {
                if (ioe.Message.Equals("Pipe is broken.", StringComparison.InvariantCultureIgnoreCase))
                {
                    HostManagerApp.LOGGER.Info("Request failed due to broken pipe, attempting to fix and retry", ioe);
                    responseBody = GetResponseFromHostManager(request);
                }
                else
                {
                    throw;
                }
            }
            return responseBody;
        }
    }
}
