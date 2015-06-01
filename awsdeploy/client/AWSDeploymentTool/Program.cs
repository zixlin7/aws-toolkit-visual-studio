using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using AWSDeployment;
using System.IO;

using Amazon.Util;

namespace AWSDeploymentTool
{
    class Program
    {
        public const int INVALID_ARGS = 1;

        const string programName = "awsdeploy";

        static bool 
            silent   = false,
            redeploy = false,
            wait     = false,
            verbose  = false,
            updateStack = false;

        static string 
            logFilePath = null,
            configPath  = null;

        static Dictionary<string, string> configOverrides = new Dictionary<string, string>();
        static DeploymentEngineBase deployment;
        static DeploymentObserver observer;

        static void Main(string[] args)
        {
            Amazon.Util.Internal.InternalSDKUtils.SetUserAgent("AWSToolkit.DeploymentTool", Assembly.GetExecutingAssembly().GetName().Version.ToString());

            ProcessArgs(args);

            if (silent)
            {
                if (verbose)
                    Console.Error.WriteLine("Making best effort to be silent AND verbose...");

                observer = !string.IsNullOrEmpty(logFilePath) ? new DeploymentToolLogObserver(logFilePath) : new DeploymentObserver();
            }
            else
            {
                if (!string.IsNullOrEmpty(logFilePath))
                {
                    observer = new DeploymentToolCombinedObserver(logFilePath, verbose);
                }
                else
                {
                    observer = new DeploymentToolConsoleObserver(verbose);
                }
            }

            if (!File.Exists(configPath))
            {
                Console.Error.WriteLine(String.Format("Specified config file not found: {0}", configPath));
                Environment.Exit(INVALID_ARGS);
            }

            try
            {
                deployment = DeploymentConfigurationReader.ReadDeploymentFromFile(configPath, configOverrides, redeploy, observer);
                deployment.Observer = observer;

                if (redeploy)
                {
                    deployment.Redeploy();

                    if (updateStack)
                        deployment.UpdateStack();
                }
                else if (updateStack)
                    deployment.UpdateStack();
                else
                    deployment.Deploy();
            }
            catch (DeploymentConfigurationReader.ConfigurationReaderException e)
            {
                Console.Error.WriteLine("Failed to parse deployment configuration file: {0}", e.Message);
                Environment.Exit(DeploymentEngineBase.DEPLOYMENT_FAILED);
            }
            catch
            {
                // The deployment class itself will message about why the deployment failed, but it 
                // re-throws the exception.
                Environment.Exit(DeploymentEngineBase.DEPLOYMENT_FAILED);
            }

            if (wait)
            {
                observer.Status("Waiting for deployment to complete.");
                int status = deployment.WaitForCompletion();
                Environment.Exit(status);
            }
        }

        static void ProcessArgs(IList<string> args)
        {
            if (args.Count < 1)
                Usage();

            for(int i = 0; i < args.Count; i++)
            {
                var arg = args[i];

                if (arg.StartsWith("-D") || arg.StartsWith("/D"))
                    ParseConfigOverrideSwitch(arg);
                else if (arg.Equals("-s") || arg.Equals("-silent") || arg.Equals("/s") || arg.Equals("/silent"))
                    silent = true;
                else if (arg.Equals("-v") || arg.Equals("-verbose") || arg.Equals("/v") || arg.Equals("/verbose"))
                    verbose = true;
                else if (arg.Equals("-r") || arg.Equals("-redeploy") || arg.Equals("/r") || arg.Equals("/redeploy"))
                    redeploy = true;
                else if (arg.Equals("-u") || arg.Equals("-updateStack") || arg.Equals("/u") || arg.Equals("/updateStack"))
                    updateStack = true;
                else if (arg.Equals("-w") || arg.Equals("-wait") || arg.Equals("/w") || arg.Equals("/wait"))
                    wait = true;
                else if (arg.Equals("-l") || arg.Equals("-log") || arg.Equals("/l") || arg.Equals("/log"))
                    logFilePath = args[++i];
                else if (i == args.Count - 1)
                    configPath = args[i];
            }

            if (string.IsNullOrEmpty(configPath))
                Usage();
        }

        static void ParseConfigOverrideSwitch(string arg)
        {
            int eqIndex = arg.IndexOf('=');

            if (eqIndex < 1)
                Usage();

            string k = arg.Substring(2, eqIndex - 2);
            string v = arg.Substring(eqIndex + 1);
            configOverrides[k] = v;
        }

        static void Usage()
        {
            Console.Error.WriteLine(String.Format("Usage: {0} [-options] configFile", programName));
            Console.Error.WriteLine("  Options:");
            Console.Error.WriteLine("    /s, /silent,   -s, -silent: Do not output messages to the console.");
            Console.Error.WriteLine("    /v, /verbose,  -v, -verbose: Give more detail about deployment to the console.");
            Console.Error.WriteLine("    /r, /redeploy, -r, -redeploy: Deploy to existing AWS CloudFormation stack or AWS Elastic Beanstalk environment.");
            Console.Error.WriteLine("    /u, /updateStack, -u, -updateStack: Perform an AWS CloudFormation update stack operation.");
            Console.Error.WriteLine("    /w, /wait,     -w, -wait: Block until deployment is complete.");
            Console.Error.WriteLine("    /l <logfile>, /log <logfile>, -l <logfile>, -log <logfile>: Log debugging information to logfile");
            Console.Error.WriteLine("    /D<key>=<value>, -D<key>=<value>: Override configuration setting from the command line.");
            Environment.Exit(1);
        }
    }
}
