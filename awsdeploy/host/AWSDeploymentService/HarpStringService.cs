using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.IO;

using log4net;

namespace AWSDeploymentService
{
    public partial class HarpStringService : ServiceBase
    {
        private static ILog LOGGER = LogManager.GetLogger(typeof(HarpStringService));
        private Process _process;
        private Timer _timer;
        private bool _ignoreTimer = true;
        private object _lockObject = new Object();

        public HarpStringService()
        {
            InitializeComponent();
            _timer = new Timer(new TimerCallback(HealthCheck));
        }

        protected override void OnStart(string[] args)
        {
           LOGGER.Info("Starting Harp String");
           ThreadPool.QueueUserWorkItem(this.StartProcessWhenInstanceReady);
        }
 
        private void StartProcessWhenInstanceReady(object status)
        {
            if (_ignoreTimer)
            {
                lock (_lockObject)
                {
                    if (_ignoreTimer)
                    {
                        _ignoreTimer = false;
                        LOGGER.Info("Waiting on WMI Event to start HostManagerApp");
                        WaitForInstanceReady();
                        LOGGER.Info("Starting HostManagerApp");
                        StartProcess();
                        _timer.Change(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
                    }
                }
            }
        }

        protected override void OnStop()
        {
            if (!_ignoreTimer)
            {
                lock (_lockObject)
                {
                    if (!_ignoreTimer)
                    {
                        LOGGER.Info("Stopping Harp String");
                        _ignoreTimer = true;
                        _timer.Change(Timeout.Infinite, Timeout.Infinite);
                        KillProcess();
                        LOGGER.Info("Stopped Harp String");
                    }
                }
            }
        }

        private void HealthCheck(object state)
        {
            if (!_ignoreTimer)
            {
                lock (_lockObject)
                {
                    if (!_ignoreTimer)
                    {
                        LOGGER.Debug("Performing Health Check");
                        if ((_process == null) || (_process.HasExited))
                        {
                            LOGGER.Info("Process not running, restarting process");
                            StartProcess();
                            return;
                        }
                    }
                }
            }
        }

        private enum CustomCommand
        {
            StartHostManager = 128
        }

        protected override void OnCustomCommand(int intCommand)
        {
            CustomCommand command = (CustomCommand)intCommand;

            switch (command)
            {
                case CustomCommand.StartHostManager:
                    StartProcess();
                    break;
            }
        }

        private void StartProcess()
        {
            try
            {
                KillProcess();
                _process = FindProcess();
                if (_process == null)
                {
                    string loc = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string exePath = Path.Combine(loc, "AWSDeploymentHostManagerApp.exe");

                    LOGGER.InfoFormat("Starting process: {0}", exePath);
                    _process = Process.Start(new ProcessStartInfo()
                    {
                        FileName = exePath,
                        WorkingDirectory = loc
                    });
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Error starting HostManagerApp process", e);
            }
        }

        private void KillProcess()
        {
            if (_process == null)
                return;
            if (_process.HasExited)
            {
                _process = null;
                return;
            }

            try
            {
                this._process.Kill();
                if (!_process.WaitForExit((int)TimeSpan.FromSeconds(5).TotalMilliseconds))
                {
                    LOGGER.Info("Timeout waiting for HostManagerApp to exit.");
                }
            }
            catch(Exception e)
            {
                LOGGER.Info("Error killing process", e);
            }
            finally
            {
                _process = null;
            }
        }

        private Process FindProcess()
        {
            Process[] candidates = Process.GetProcessesByName("AWSDeploymentHostManagerApp");
            if (candidates.Length > 1) LOGGER.WarnFormat("Found {0} HostmanagerApps running.", candidates.Length);
            return (candidates.Length > 0) ? candidates[0] : null;
        }

        private void WaitForInstanceReady()
        {
            LOGGER.Info("Waiting for EC2 Configuration to complete.");
            int tries = 0;
            while (!EC2ConfigUtil.CheckInstanceReady())
            {
                Thread.Sleep(5000);
                if (++tries % 5 == 0)
                    LOGGER.Info("Still waiting.");
            }
            LOGGER.Info("EC2 Configuration complete.");
        }
        
    }
}
