using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using Microsoft.Web.Administration;
using System.Text;

using System.Threading;

using log4net;


namespace AWSDeploymentHostManager.Tasks
{
    public class RestartAppServerTask : Task
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(RestartAppServerTask));

        public override string Operation
        {
            get
            {
                return "RestartAppServer"; 
            }
        }

        public override string Execute()
        {
            Interlocked.Increment(ref HostManager.ASyncTasksRunning);
            ThreadPool.QueueUserWorkItem(this.DoExecute);
            return GenerateResponse(TASK_RESPONSE_DEFER);
        }

        public void DoExecute(object status)
        {
            try
            {
                ServerManager sm = new ServerManager();
                bool stopping = false;
                bool starting = false;

                ApplicationPool defaultAppPool = ConfigHelper.GetAppPool(sm);

                if (null == defaultAppPool)
                {
                    LOGGER.Warn("Failed to load App Pool");
                    Event.LogWarn(Operation, "Failed to Restart App Server. App pool not found.");
                    return;
                }

                try
                {
                    ObjectState os = defaultAppPool.State;
                    if (os == ObjectState.Started)
                    {
                        LOGGER.Info("Attempting to stop app server.");
                        defaultAppPool.Stop();
                        stopping = true;
                    }
                    else
                    {
                        LOGGER.InfoFormat("App server was not started, skipping stopping: State = {0}", os);
                    }
                }
                catch (Exception e)
                {
                    LOGGER.Warn("Failed to stop app server", e);
                    Event.LogWarn(Operation, String.Format("Failed to stop app server: {0}", e.Message));
                }

                try
                {
                    if (defaultAppPool.State == ObjectState.Started || defaultAppPool.State == ObjectState.Starting)
                    {
                        LOGGER.Warn("App server was not stopping, skipping restarting");
                        Event.LogWarn(Operation, "App server was not stopping, skipping restarting");
                        return;
                    }
                    for (int i = 0; i < 5; i++)
                    {
                        Thread.Sleep(i * 1000);

                        if (defaultAppPool.State == ObjectState.Stopped)
                        {
                            LOGGER.Info("App server stopped.");
                            LOGGER.Info("Attemping to start app server.");
                            defaultAppPool.Start();
                            starting = true;
                            break;
                        }

                    }
                    if (!starting)
                    {
                        if (stopping)
                        {
                            LOGGER.Warn("Time out waiting for app server to stop, not restarting.");
                            Event.LogWarn(Operation, "Time out waiting for app server to stop, not restarting");
                            return;
                        }
                        else
                        {
                            LOGGER.WarnFormat("App server not restarting: State = {0}", defaultAppPool.State);
                        }
                    }

                }
                catch (Exception e)
                {
                    LOGGER.Warn("Failed to start app server", e);
                    Event.LogWarn(Operation, String.Format("Failed to start app server: {0}", e.Message));
                    return;
                }

                if (starting)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        Thread.Sleep(i * 1000);

                        if (defaultAppPool.State == ObjectState.Started)
                        {
                            LOGGER.Info("App server started.");
                            Event.LogInfo(Operation, "Restarted AppServer");
                            return;
                        }
                    }

                    LOGGER.WarnFormat("Time out waiting for app server to restart: State = {0}", defaultAppPool.State);
                    Event.LogWarn(Operation, "Time out waiting for app server to restart");
                }

                return;
            }
            finally
            {
                Interlocked.Decrement(ref HostManager.ASyncTasksRunning);
            }
        }
    }
}
