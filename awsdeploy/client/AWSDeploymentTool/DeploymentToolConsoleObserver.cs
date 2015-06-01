using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AWSDeployment;

namespace AWSDeploymentTool
{
    internal class DeploymentToolConsoleObserver : DeploymentObserver
    {
        public bool Verbose { get; set; }

        public DeploymentToolConsoleObserver(bool verbose)
        {
            Verbose = verbose;
        }

        public override void Status(string messageFormat, params object[] list)
        {
            Console.WriteLine(String.Format(messageFormat, list));
        }

        public override void Info(string messageFormat, params object[] list) 
        {
            if (Verbose)
                Console.WriteLine(String.Format(messageFormat, list));
        }

        public override void Warn(string messageFormat, params object[] list) 
        {
            Console.WriteLine(String.Format("[Warning]: " + messageFormat, list));
        }

        public override void Error(string messageFormat, params object[] list) 
        {
            Console.WriteLine(String.Format("[Error]: " + messageFormat, list));
        }
    }
}
