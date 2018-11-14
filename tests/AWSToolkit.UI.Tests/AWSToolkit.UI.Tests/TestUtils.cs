using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AWSToolkitUITests
{
    public static class TestUtils
    {
#if DEBUG
        private const string awsStudioExePath = "Deployment\\awsstudio\\debug\\awsstudio.exe";
#else
        private const string awsStudioExePath = "Deployment\\awsstudio\\release\\awsstudio.exe";
#endif
        /// <summary>
        /// Walks the folder hierarchy upwards to try and find the 'root' marker file - this
        /// folder location can then be used to derive the location of built deployment artifacts
        /// such as awsstudio.exe
        /// </summary>
        /// <param name="initialFolder"></param>
        /// <returns></returns>
        public static string FindRepoRoot(string initialFolder)
        {
            const string rootMarker = "root";

            var di = new DirectoryInfo(initialFolder);
            while (!File.Exists(Path.Combine(di.FullName, rootMarker)) && di.Parent != null)
            {
                di = di.Parent;
            }

            return di.FullName;
        }

        private static string _awsStudioExe;
        public static string AWSStudioExe
        {
            get
            {
                if (string.IsNullOrEmpty(_awsStudioExe))
                {
                    _awsStudioExe = Path.Combine(FindRepoRoot(Assembly.GetExecutingAssembly().Location), awsStudioExePath);
                }

                return _awsStudioExe;
            }
        }
    }
}
