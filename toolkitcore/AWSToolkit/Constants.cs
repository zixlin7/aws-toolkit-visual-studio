using System;
using System.Reflection;

using Amazon.Runtime;
using Amazon.Util;

namespace Amazon.AWSToolkit
{
    public static class Constants
    {
        /// <summary>
        /// This Guid MUST stay in sync with VsixGuid in buildtools\Package.Build.targets
        ///
        /// If this repo needs to produce different Toolkit products in the future, ifdef could be used
        /// by setting up BuildConstants in buildtools\Common.Build.CSharp.settings
        ///
        /// Previous VS Toolkit GUIDs:
        /// VS 2013: 9510184f-8135-4f8a-ab8a-23be77c345e2
        /// VS 2015: f2884b07-5122-4e23-acd7-4d93df18709e
        /// VS 2017 (current Toolkit): 12ed248b-6d4a-47eb-be9e-8eabea0ff119
        /// VS 2022: 0B82CB16-0E52-4363-9BC0-61E758689176
        /// </summary>
        public static class ToolkitPackageGuids
        {
            public const string Vs20172019AsString = "12ed248b-6d4a-47eb-be9e-8eabea0ff119";
            public static readonly Guid Vs20172019 = new Guid(Vs20172019AsString);

            public const string Vs2022AsString = "0B82CB16-0E52-4363-9BC0-61E758689176";
            public static readonly Guid Vs2022 = new Guid(Vs2022AsString);
        }

        public const string PublishToAwsPackageGuidStr = "e9576543-4ce6-4081-afcb-4a6ee4246e52";
        public static readonly Guid PublishToAwsPackageGuid = new Guid(PublishToAwsPackageGuidStr);

        public const string AWS_SERVERLESS_TEMPLATE_DEFAULT_FILENAME = "serverless.template";

        public static readonly TimeSpan? DEFAULT_S3_TIMEOUT = TimeSpan.FromMilliseconds(int.MaxValue);

        public const int MAX_ClOUDWATCH_DATAPOINTS = 168;
        public const int MIN_CLOUDWATCH_PERIOD = 60 * 5;

        public const int TEXT_FILTER_IDLE_TIMER = 750;

        public const string AWSEXPLORER_DESCRIBE_USER_AGENT = "AWSExplorerDescribeCall";

        public const string AWSToolkitRegistryKey = @"SOFTWARE\Amazon Web Services\AWS Toolkit";

        public const string BEANSTALK_RDS_SECURITY_GROUP_POSTFIX = "-rds-associations";

        public const string VS_SOLUTION_ITEM_KIND_GUID = "{66A26722-8FB5-11D2-AA7E-00C04F688DDE}";

        public static string GetIAMRoleAssumeRolePolicyDocument(string serviceName, string regionId)
        {
            const string IAM_ROLE_ASSUME_ROLE_POLICY_DOCUMENT =
                "{{\"Statement\":[{{\"Principal\":{{\"Service\":[\"{0}\"]}},\"Effect\":\"Allow\",\"Action\":[\"sts:AssumeRole\"]}}]}}";

            var principal = GetServicePrincipalForAssumeRole(regionId, serviceName);
            var policy = string.Format(IAM_ROLE_ASSUME_ROLE_POLICY_DOCUMENT, principal);
            return policy;
        }

        /// <summary>
        /// Produce the service principal text for a Policy Document for a given service
        /// </summary>
        /// <param name="regionId">Used to resolve which partition is being used</param>
        /// <param name="serviceName">Service to get principal for. See <see cref="ClientConfig.RegionEndpointServiceName"/> or endpoints.json for values.</param>
        /// <returns></returns>
        public static string GetServicePrincipalForAssumeRole(string regionId, string serviceName)
        {
            var serviceEndpoint = RegionEndpoint.GetBySystemName(regionId).GetEndpointForService(serviceName);

            var domainPos = serviceEndpoint.Hostname.IndexOf("amazonaws");
            if (domainPos == -1 || serviceName.Equals("elasticbeanstalk", StringComparison.OrdinalIgnoreCase))
            {
                return $"{serviceName}.amazonaws.com";
            }

            var hostUrl = serviceEndpoint.Hostname
                .Substring(domainPos)
                .TrimEnd(new char[] {'/'});

            return $"{serviceName}.{hostUrl}";
        }

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
