using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Amazon.AWSToolkit.Util.Tests
{
    public static class TestUtil
    {
        public static Stream LoadInvalidXmlEndpointsFile(string fileName)
        {
            var stream = typeof(TestUtil).Assembly.GetManifestResourceStream("Amazon.AWSToolkit.Util.Tests.Resources." + fileName);
            return stream;
        }

    }
}
