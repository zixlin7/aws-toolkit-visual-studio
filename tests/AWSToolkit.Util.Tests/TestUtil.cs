using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AWSToolkit.Util.Tests
{
    public static class TestUtil
    {
        public const string TestFileFolder = "HostedFileTesting";

        public static Stream LoadInvalidXmlEndpointsFile(string fileName)
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AWSToolkit.Util.Tests.Resources." + fileName);
            return stream;
        }

    }
}
