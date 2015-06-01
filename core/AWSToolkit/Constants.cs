using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using Amazon.Runtime;
using Amazon.Util;
using Amazon.S3;

namespace Amazon.AWSToolkit
{
    public static class Constants
    {
        public const string SERVICE_ENDPOINT_FILE = @"ServiceEndPoints.xml";
        public const string VERSION_INFO_FILE = @"VersionInfo.xml";

        public static readonly TimeSpan? DEFAULT_S3_TIMEOUT = TimeSpan.FromMilliseconds(int.MaxValue);

        public const int MAX_ClOUDWATCH_DATAPOINTS = 168;
        public const int MIN_CLOUDWATCH_PERIOD = 60 * 5;

        public const int TEXT_FILTER_IDLE_TIMER = 750;

        public const string AWSEXPLORER_DESCRIBE_USER_AGENT = "AWSExplorerDescribeCall";

        public const string AWSToolkitRegistryKey = @"SOFTWARE\Amazon Web Services\AWS Toolkit";

        public const string BEANSTALK_RDS_SECURITY_GROUP_POSTFIX = "-rds-associations";

        public const string IAM_ROLE_EC2_ASSUME_ROLE_POLICY_DOCUMENT = 
            "{\"Statement\":[{\"Principal\":{\"Service\":[\"ec2.amazonaws.com\"]},\"Effect\":\"Allow\",\"Action\":[\"sts:AssumeRole\"]}]}";

        static string _versionNumber;
        public static string VERSION_NUMBER
        {
            get
            {
                if (_versionNumber == null)
                {
                    _versionNumber = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                }

                return _versionNumber;
            }
        }

        // Per-shell global constants for shells known at this time
        public static class AWSStudioHostShell
        {
            public static readonly string ShellName = "AWSToolkit.Studio";
        }

        public static class VS2008HostShell
        {
            public static readonly string ShellName = "AWSToolkit.VS2008";
        }

        public static class VS2010HostShell
        {
            public static readonly string ShellName = "AWSToolkitPackage.VS2010";
        }

        public static class VS2012HostShell
        {
            public static readonly string ShellName = "AWSToolkitPackage.VS2012";
        }

        public static class VS2013HostShell
        {
            public static readonly string ShellName = "AWSToolkitPackage.VS2013";
        }

        public static class VS2015HostShell
        {
            public static readonly string ShellName = "AWSToolkitPackage.VS2015";
        }

        public static void AWSExplorerDescribeUserAgentRequestEventHandler(object sender, RequestEventArgs args)
        {
            if (args is WebServiceRequestEventArgs)
            {
                string currentUserAgent = ((WebServiceRequestEventArgs)args).Headers[AWSSDKUtils.UserAgentHeader];
                ((WebServiceRequestEventArgs)args).Headers[AWSSDKUtils.UserAgentHeader] = currentUserAgent + " " + AWSEXPLORER_DESCRIBE_USER_AGENT;
            }
            else if (args is HeadersRequestEventArgs)
            {
                string currentUserAgent = ((HeadersRequestEventArgs)args).Headers[AWSSDKUtils.UserAgentHeader];
                ((HeadersRequestEventArgs)args).Headers[AWSSDKUtils.UserAgentHeader] = currentUserAgent + " " + AWSEXPLORER_DESCRIBE_USER_AGENT;
            }
        }
    }
}
