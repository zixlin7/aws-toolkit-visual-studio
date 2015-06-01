using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Net;

namespace AWSDeploymentHostManager
{
    internal class PipeHost
    {
        const int TIMER_PERIOD_IN_MINUTES = 15;
        private StreamReader hmPipeReader;
        private Thread listener;
        private bool listening = false;
        internal static PipeHost instance;

        private HostManager hm;

        public static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                if (String.Equals(args[0], "test"))
                {
                    HostManager.LOGGER.Info("Testing Host Manager");
                    while (true)
                    {
                        Thread.Sleep((int)TimeSpan.FromDays(1).TotalMilliseconds);
                    }
                }
            }

            HostManager.LOGGER.Info("Starting Host Manager");
            PipeHost ph = new PipeHost();
        }

        public PipeHost()
        {
            var pipeName = GetPipeName();
            HostManager.LOGGER.Debug("Creating Pipe to " + pipeName);
            NamedPipeClientStream hmPipe = new NamedPipeClientStream(".", pipeName, PipeDirection.In);

            HostManager.LOGGER.Debug("Connecting to Pipe");
            hmPipe.Connect();
            HostManager.LOGGER.Debug("Connected to Pipe");
            hmPipeReader = new StreamReader(hmPipe, Encoding.UTF8, false, PipeHelper.BufferSize);

            HostManager.LOGGER.Debug("Reading Configuration from Pipe");
            string configuration = PipeHelper.ReadFromPipe(hmPipeReader);

            HostManager.LOGGER.Debug("Creating Host Manager");
            hm = new HostManager(configuration);

            HostManager.LOGGER.Debug("Setting up Timer");
            Timer updateConfigPoll = new Timer(new TimerCallback(this.TimerCallback));
            updateConfigPoll.Change(TimeSpan.FromMinutes(TIMER_PERIOD_IN_MINUTES), TimeSpan.FromMinutes(TIMER_PERIOD_IN_MINUTES));

            StartListening();
        }

        private static string GetPipeName()
        {
            string loc = System.Reflection.Assembly.GetExecutingAssembly().Location;
            int last = loc.LastIndexOf('\\');
            int nextLast = loc.LastIndexOf('\\', last - 1);
            return loc.Substring(nextLast + 1, last - nextLast - 1);
        }

        internal void StartListening()
        {
            instance = this;
            listener = new Thread(ListenForRequests);
            listener.Start();
        }
        private void ListenForRequests(Object state)
        {
            try
            {
                listening = true;
                while (true)
                {
                    string taskPipeName;
                    taskPipeName = PipeHelper.ReadFromPipe(hmPipeReader);
                    HostManager.LOGGER.Debug("Pipe Name: " + taskPipeName);
                    Interlocked.Increment(ref HostManager.SyncTasksRunning);
                    if (taskPipeName.Equals("done"))
                    {
                        Interlocked.Decrement(ref HostManager.SyncTasksRunning);
                        listening = false;
                        while (HostManager.SyncTasksRunning != 0) Thread.Sleep((int)TimeSpan.FromSeconds(1).TotalMilliseconds);
                        while (HostManager.ASyncTasksRunning != 0) Thread.Sleep((int)TimeSpan.FromSeconds(1).TotalMilliseconds);
                        break;
                    }
                    ThreadPool.QueueUserWorkItem(DoRequest, taskPipeName);
                }
            }
            catch (Exception e)
            {
                HostManager.LOGGER.Error("Unexpected Exception:", e);
                throw;
            }
        }

        private void TimerCallback(Object state)
        {
            if (hm.Status == HostManagerStatus.Running)
            {
                hm.CheckForNewVersionAndUpdate();
                hm.UpdateConfig();
            }
        }

        internal bool RequestStatusUpdate()
        {
            bool ret;
            string url = null;
            try
            {
                url = "http://localhost/_hostmanager/hostmanagercheck";
                var httpRequest = WebRequest.Create(url) as HttpWebRequest;
                using (var httpResponse = httpRequest.GetResponse() as HttpWebResponse)
                {
                    if (httpResponse.StatusCode == HttpStatusCode.OK)
                    {
                        HostManager.LOGGER.DebugFormat("Hostmanger update status request received at {0}", url);
                        ret = true;
                    }
                    else
                    {
                        HostManager.LOGGER.DebugFormat("Failed hostmanager status update request to {0} with status code {1}", url, httpResponse.StatusCode);
                        ret = false;
                    }
                }
            }
            catch (Exception e)
            {
                HostManager.LOGGER.Warn(string.Format("Failed hostmanager status update request to {0}", url), e);
                ret = false;
            }
            return ret;
        }

        internal void DoRequest(Object state)
        {
            string taskPipeName = (string)state;
            using (NamedPipeClientStream taskPipe = new NamedPipeClientStream(".", taskPipeName, PipeDirection.InOut))
            {
                string request;
                taskPipe.Connect();
                StreamReader taskReader = new StreamReader(taskPipe, Encoding.UTF8, false, PipeHelper.BufferSize);
                request = PipeHelper.ReadFromPipe(taskReader);

                string response;
                if (string.Equals("healthcheck", request, StringComparison.InvariantCultureIgnoreCase))
                {
                    response = hm.PerformApplicationHealthcheck();
                }
                else if (string.Equals("statuscheck", request, StringComparison.InvariantCultureIgnoreCase))
                {
                    response = hm.Status.ToString();
                }
                else if (string.Equals("versioncheck", request, StringComparison.InvariantCultureIgnoreCase))
                {
                    response = hm.GetCurrentVersion();
                }
                else
                {
                    response = hm.ProcessTaskRequest(request);
                }

                StreamWriter taskWriter = new StreamWriter(taskPipe, Encoding.UTF8, PipeHelper.BufferSize);
                PipeHelper.WriteToPipe(taskWriter, response);
                taskPipe.WaitForPipeDrain();
                Interlocked.Decrement(ref HostManager.SyncTasksRunning);
            }
        }

        internal void GetReadyForExit(HostManagerStatus newStatus)
        {
            hm.Status = newStatus;
            bool updateRequested = false;
            while (!updateRequested)
            {
                updateRequested = RequestStatusUpdate();
            }
            if ((newStatus == HostManagerStatus.Queue) || (newStatus == HostManagerStatus.Stopping))
            {
                while (listening)
                {
                    Thread.Sleep((int)TimeSpan.FromSeconds(1).TotalMilliseconds);
                }
                instance = null;
            }
        }

    }
}
