using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ServiceProcess;

using Microsoft.Win32;

using log4net;

namespace AWSDeploymentHostManager.Tasks
{
    public class UnmanageTask : Task
    {
        public const string SERVICE_NAME = "Magic Harp";
        static ILog LOGGER = LogManager.GetLogger(typeof(UnmanageTask));

        public const int SERVICE_DISABLE_START_STATUS = 4;
        public const int SERVICE_MANUAL_START_STATUS = 3;
        public const int SERVICE_AUTOMATIC_START_STATUS = 2;

        bool _killProcess = true;
        public UnmanageTask()
        {
        }

        public UnmanageTask(bool killProcess)
        {
            this._killProcess = killProcess;
        }

        public override string Execute()
        {
            LOGGER.Info("Execute");
            Interlocked.Increment(ref HostManager.ASyncTasksRunning);
            ThreadPool.QueueUserWorkItem(this.DoExecute);
            return GenerateResponse(TASK_RESPONSE_DEFER);
        }

        public override string Operation
        {
            get { return "Unmanage"; }
        }

        public void DoExecute(object state)
        {
            // Sleep to make sure we don't shutdown the app before response has been returned.
            Thread.Sleep(100);
            try
            {
                ServiceController controller;
                try
                {
                    controller = new ServiceController();
                    controller.ServiceName = SERVICE_NAME;

                    SetMagicHarpStartMethod(SERVICE_DISABLE_START_STATUS);

                    LOGGER.Info("Attempting to shutdown Magic Harp service");
                    if (PipeHost.instance != null)
                    {
                        PipeHost.instance.GetReadyForExit(HostManagerStatus.Stopping);
                    };
                }
                finally
                {
                    Interlocked.Decrement(ref HostManager.ASyncTasksRunning);
                }

                HostManager.instance.Exit();
                if (controller.CanStop)
                {
                    controller.Stop();
                    controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMilliseconds(100));
                }

                LOGGER.Warn("Shutdown of Magic Harp service didn't stop HostManager, forcing the exit if requested.");
                if (this._killProcess)
                {
                    Environment.Exit(0);
                }                
            }
            catch (Exception e)
            {
                Event.LogWarn(this.Operation, "Error shutting down host manager service: " + e.Message);
                LOGGER.Error("Error shutting down host manager service.", e);
            }
        }

        public static int GetMagicHarpStartMethod()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + SERVICE_NAME, false);
            return Convert.ToInt32(key.GetValue("Start"));
        }

        public static void SetMagicHarpStartMethod(int mode)
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + SERVICE_NAME, true);
            key.SetValue("Start", mode);
        }

    }
}
