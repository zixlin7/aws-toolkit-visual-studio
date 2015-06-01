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
    public partial class MagicHarpService : ServiceBase
    {
        private const string HOSTMANAGER_DIR_PATTERN = "HostManager*";
        private static ILog LOGGER = LogManager.GetLogger(typeof(MagicHarpService));
        private bool _running = false;
        private object _lockObject = new Object();
        private Process _currentProcess;

        public MagicHarpService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            if (!_running)
            {
                lock (_lockObject)
                {
                    if (!_running)
                    {
                        LOGGER.Info("Starting Magic Harp");
                        _running = true;
                    }
                }
            }
        }

        protected override void OnStop()
        {
            if (_running)
            {
                lock (_lockObject)
                {
                    if (_running)
                    {
                        LOGGER.Info("Stopping Magic Harp");
                        _running = false;
                        KillCurrentProcess();
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
                if (_running)
                {
                    lock (_lockObject)
                    {
                        if (_running)
                        {
                            KillCurrentProcess();

                            string loc = LatestHostManagerDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                            string exePath = Path.Combine(loc, "AWSDeploymentHostManager.exe");

                            LOGGER.InfoFormat("Starting process: {0}", exePath);
                            this._currentProcess = Process.Start(new ProcessStartInfo()
                            {
                                FileName = exePath,
                                WorkingDirectory = loc
                            });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Error starting HostManager process", e);
            }
        }

        public static string LatestHostManagerDirectory(string basePath)
        {
            string latestHMDirectory =
                new DirectoryInfo(basePath)
                        .GetDirectories(HOSTMANAGER_DIR_PATTERN)
                        .OrderByDescending(f => f.Name)
                        .First().Name;

            return Path.Combine(basePath, latestHMDirectory);
        }

        void KillCurrentProcess()
        {
            try
            {
                if (this._currentProcess != null && !this._currentProcess.HasExited)
                {
                    LOGGER.InfoFormat("Killing host manager process: {0}", this._currentProcess.StartInfo.FileName);
                    this._currentProcess.Kill();
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Error killing current process", e);
            }

            this._currentProcess = null;
        }
    }
}
