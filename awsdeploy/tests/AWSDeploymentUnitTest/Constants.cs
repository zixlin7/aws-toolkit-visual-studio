using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.Common.TestCredentials;

namespace AWSDeploymentUnitTest
{
    public class Constants
    {
        private static TestCredentials defaultCredentials = TestCredentials.DefaultCredentials;

        public static string ACCESS_KEY_ID = defaultCredentials.AccessKey;
        public static string SECRET_KEY_ID = defaultCredentials.SecretKey;
    }
}
