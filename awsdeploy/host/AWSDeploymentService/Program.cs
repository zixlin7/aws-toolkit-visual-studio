using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Security.Principal;
using System.ServiceProcess;
using System.IO;
using System.Text;

namespace AWSDeploymentService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            List<ServiceBase> ServicesToRun = new List<ServiceBase>();
            string userName = WindowsIdentity.GetCurrent().Name;
            if (String.Equals(userName,"NT AUTHORITY\\LOCAL SERVICE"))
            {
                ServicesToRun.Add(new HarpStringService());
            }
            else
            {
                ServicesToRun.Add(new MagicHarpService());
            }
            ServiceBase.Run(ServicesToRun.ToArray());
        }
    }
}
