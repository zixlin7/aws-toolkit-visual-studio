using System;
using System.Diagnostics;
using System.Threading;

using Microsoft.Build.Utilities;
using BuildCommon;
using System.Text;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Linq;

namespace BuildTasks
{
    public class CheckAssemblies : BuildTaskBase
    {
        public string AssembliesLocation { get; set; }
        public string ExpectedPublicKeyToken { get; set; }

        public override bool Execute()
        {
            CheckWaitForDebugger();

            if (string.IsNullOrEmpty(AssembliesLocation))
                throw new ArgumentNullException("AssembliesLocation");
            if (string.IsNullOrEmpty(ExpectedPublicKeyToken))
                throw new ArgumentNullException("ExpectedPublicKeyToken");

            var success = true;
            var folder = new DirectoryInfo(AssembliesLocation);
            var assemblies = folder.GetFiles("*.dll", SearchOption.AllDirectories);
            foreach(var assembly in assemblies)
            {
                var assemblyLocation = assembly.FullName;
                var token = GetPublicKeyToken(assemblyLocation);
                
                if (assembly.DirectoryName.Contains("unity"))
                {
                    this.Log.LogMessage("Skipping Unity Assembly: {0}, Token: {1}", assemblyLocation, token);
                    continue;
                }

                if (!string.Equals(token, ExpectedPublicKeyToken, StringComparison.Ordinal))
                {
                    Log.LogError("Assembly {0}: public key token {1} does not equal expected token {2}",
                        assemblyLocation, token, ExpectedPublicKeyToken);
                    success = false;
                }
            }

            return success;
        }

        private static string GetPublicKeyToken(string assemblyLocation)
        {
            var tokenBytes = Assembly.LoadFile(assemblyLocation).GetName().GetPublicKeyToken();
            var token = string.Join(string.Empty,
                tokenBytes.Select(b => b.ToString("x2")));
            return token;
        }
    }
}
