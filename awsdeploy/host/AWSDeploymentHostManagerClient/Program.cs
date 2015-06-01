using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;

using System.Security.Cryptography;

using ThirdParty.Json.LitJson;

// Usage: AWSDeploymentHostManagerClient.exe [-h host] [-i instance_id] [-r reservation_id] [-v] TaskName [param=value [param=value] ...]] 
// -v means verbose.
// -h host defaults to localhost
// -i instance_id defaults to i-00000000
// -r reservation_id defaults to r-00000000

namespace AWSDeploymentHostManagerClient
{
    class Program
    {
        static void Main(string[] args)
        {
            HostManagerClientRequest request = new HostManagerClientRequest();

            // Parse Arguments
           
            int i = 0;
            while(i < args.Length && args[i][0] == '-')
            {
                switch (args[i][1])
                {
                    case 'h':
                        request.Hostname = args[++i];
                        break;
                    case 'v':
                        request.Verbose = true;
                        break;
                    case 'i':
                        request.InstanceId = args[++i];
                        break;
                    case 'r':
                        request.ReservationId = args[++i];
                        break;
                    default:
                        Usage();
                        break;
                }
                i++;
            }

            if (i < args.Length)
            {
                request.TaskName = args[i];
                i++;
            }
            else
                Usage();

            while (i < args.Length)
            {
                int endKey = args[i].IndexOf('=');
                if (endKey + 1 < args[i].Length)
                    request.Parameters[args[i].Substring(0, endKey)] = args[i].Substring(endKey + 1);
                else
                    Usage();
                i++;
            }

            Console.WriteLine(request.SendRequest());
        }

        private static void Usage()
        {
            Console.WriteLine("Usage: AWSDeploymentHostManagerClient.exe [-h host] [-i instance_id] [-r reservation_id] [-v] TaskName [param=value [param=value] ...]]");
            Environment.Exit(1);
        }

        // From the PoV of the client the senses of [De|En]crypt Re[quest|sponse] are reversed.
        

    }
}
