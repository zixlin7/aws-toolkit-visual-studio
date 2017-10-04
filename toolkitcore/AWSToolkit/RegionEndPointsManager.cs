using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Reflection;
using System.Xml.Linq;

using Amazon.AWSToolkit.VersionInfo;
using Amazon.Runtime.Internal.Settings;

namespace Amazon.AWSToolkit
{
    public class RegionEndPointsManager
    {
        public const string EC2_SERVICE_NAME = "EC2";
        public const string ECS_SERVICE_NAME = "ECS";
        public const string ECR_SERVICE_NAME = "ECR";
        public const string ELB_SERVICE_NAME = "ELB";
        public const string AUTOSCALING_SERVICE_NAME = "AutoScaling";
        public const string CLOUDWATCH_SERVICE_NAME = "CloudWatch";
        public const string CLOUDWATCH_EVENT_SERVICE_NAME = "CloudWatchEvents";
        public const string ELASTICBEANSTALK_SERVICE_NAME = "ElasticBeanstalk";
        public const string CLOUDFORMATION_SERVICE_NAME = "CloudFormation";
        public const string RDS_SERVICE_NAME = "RDS";
        public const string S3_SERVICE_NAME = "S3";
        public const string IAM_SERVICE_NAME = "IAM";
        public const string SNS_SERVICE_NAME = "SNS";
        public const string SQS_SERVICE_NAME = "SQS";
        public const string LAMBDA_SERVICE_NAME = "Lambda";
        public const string KINESIS_SERVICE_NAME = "Kinesis";
        public const string DYNAMODB_SERVICE_NAME = "DynamoDB";
        public const string DYNAMODB_STREAM_SERVICE_NAME = "DynamoDBStream";
        public const string CLOUDWATCH_LOGS_NAME = "Logs";
        public const string KMS_SERVICE_NAME = "KMS";
        public const string CODECOMMIT_SERVICE_NAME = "CodeCommit";

        public const string US_EAST_1 = "us-east-1";
        const string DEFAULT_REGION = "us-west-2";
        const string SETTINGS_KEY = "lastselectedregion";
        static RegionEndPointsManager _instance;

        Dictionary<string, RegionEndPoints> _regions;
        Exception _errorLoading;
        LocalRegionEndPoints _localRegions = new LocalRegionEndPoints();

        static RegionEndPointsManager()
        {
            _instance = new RegionEndPointsManager();
            _instance.loadEndPoints();
        }

        public static RegionEndPointsManager Instance
        {
            get { return _instance; }
        }

        public void Refresh()
        {
            this._errorLoading = null;
            loadEndPoints();
        }

        public RegionEndPoints GetRegion(string systemName)
        {
            RegionEndPoints region = null;
            this._regions.TryGetValue(systemName, out region);
            return region;
        }

        public IEnumerable<RegionEndPoints> Regions
        {
            get { return this._regions.Values; }
        }

        public LocalRegionEndPoints LocalRegion
        {
            get { return this._localRegions; }
        }

        public RegionEndPoints GetDefaultRegionEndPoints()
        {
            var regionSystemName = PersistenceManager.Instance.GetSetting(SETTINGS_KEY);
            if (string.IsNullOrEmpty(regionSystemName))
                regionSystemName = DEFAULT_REGION;

            var region = Instance.GetRegion(regionSystemName);
            return region;
        }

        public void SetDefaultRegionEndPoints(RegionEndPoints region)
        {
            PersistenceManager.Instance.SetSetting(SETTINGS_KEY, region.SystemName);
        }

        public bool FailedToLoad
        {
            get { return ErrorLoading != null; }
        }

        public Exception ErrorLoading
        {
            get { return this._errorLoading; }
        }

        private void loadEndPoints()
        {
            try
            {
                this._regions = new Dictionary<string, RegionEndPoints>();
                using (TextReader reader = openEndPointConfigurationFile())
                {
                    XDocument xdoc = XDocument.Load(reader);
                    var query = from p in xdoc.Elements("regions").Elements("region")
                                select new
                                {
                                    SystemName = p.Element("systemname").Value,
                                    DisplayName = p.Element("displayname").Value,
                                    FlagIcon = p.Element("flag-icon").Value,
                                    MinToolkitVersion = p.Element("min-toolkit-version") != null ? p.Element("min-toolkit-version").Value : null,
                                    Restrictions = p.Element("restrictions") != null ? p.Element("restrictions").Value.Split(',') : null
                                };

                    foreach (var regionName in query)
                    {
                        var subQuery = from s in xdoc.Elements("regions").Elements("region").Elements("services").Elements("service")
                                       where s.Parent.Parent.Element("systemname").Value == regionName.SystemName
                                       select new
                                           {
                                               Name = (string)s.Attribute("name"),
                                               URL = s.Value,
                                               Signer = (string)s.Attribute("signer"),
                                               AuthRegion = (string)s.Attribute("authregion")
                                           };

                        IDictionary<string, EndPoint> endpoints = new Dictionary<string, EndPoint>();
                        foreach (var endpoint in subQuery)
                        {
                            endpoints[endpoint.Name] = new EndPoint(regionName.SystemName, endpoint.URL, endpoint.Signer, endpoint.AuthRegion);
                        }

                        if (string.IsNullOrEmpty(regionName.MinToolkitVersion) || !VersionManager.IsVersionGreaterThanToolkit(regionName.MinToolkitVersion))
                        {
                            var region = new RegionEndPoints(regionName.SystemName, regionName.DisplayName, regionName.FlagIcon, endpoints, regionName.Restrictions);
                            this._regions.Add(region.SystemName, region);
                        }
                    }
                }
                
                this._regions[this._localRegions.SystemName] = this._localRegions;
            }
            catch (Exception e)
            {
                this._errorLoading = e;
            }
        }        

        private TextReader openEndPointConfigurationFile()
        {
            string location = null;
            if (location == null)
            {
                location = Constants.SERVICE_ENDPOINT_FILE;
            }

            if (string.Equals(location, Constants.SERVICE_ENDPOINT_FILE))
            {
                return new StringReader(S3FileFetcher.Instance.GetFileContent(location, S3FileFetcher.CacheMode.Never));
            }
            else
            {
                return new StreamReader(location);
            }
        }


        public class RegionEndPoints
        {
            protected IDictionary<string, EndPoint> _endpoints;
            HashSet<string> _restrictions;

            internal RegionEndPoints(string systemName, string displayName, string flagIconName, IDictionary<string, EndPoint> endpoints, string[] restrictions)
            {
                this.SystemName = systemName;
                this.DisplayName = displayName;
                this.FlagIconName = flagIconName;
                this._endpoints = endpoints;

                this._restrictions = new HashSet<string>();
                if (restrictions != null)
                {
                    foreach (var item in restrictions)
                    {
                        if (!this._restrictions.Contains(item))
                            this._restrictions.Add(item);
                    }
                }
            }

            public bool HasRestrictions
            {
                get{return this._restrictions.Count > 0;}
            }

            public bool ContainAnyRestrictions(HashSet<string> restrictionsToCompare)
            {
                if (this._restrictions == null || this._restrictions.Count == 0)
                    return false;

                foreach (var restriction in restrictionsToCompare)
                {
                    if (this._restrictions.Contains(restriction))
                    {
                        return true;
                    }
                }

                return false;
            }


            public string DisplayName
            {
                get;
                private set;
            }

            public string SystemName
            {
                get;
                private set;
            }

            public string FlagIconName
            {
                get;
                private set;
            }

            public Stream FlagIcon
            {
                get
                {
                    Stream stream = S3FileFetcher.Instance.OpenFileStream(FlagIconName, S3FileFetcher.CacheMode.Permanent);
                    return stream;
                }
            }

            public virtual EndPoint GetEndpoint(string serviceName)
            {
                EndPoint endpoint = null;
                this._endpoints.TryGetValue(serviceName, out endpoint);
                return endpoint;
            }

            public virtual string GetPrincipalForAssumeRole(string serviceName)
            {
                var endpoint = GetEndpoint(serviceName);

                var pos = endpoint.Url.IndexOf("amazonaws");
                if (pos == -1 || serviceName.Equals("elasticbeanstalk", StringComparison.OrdinalIgnoreCase))
                    return string.Concat(serviceName.ToLowerInvariant(), ".amazonaws.com");

                var host = endpoint.Url.Substring(pos);
                if (host.EndsWith("/"))
                    host = host.Substring(0, host.Length - 1);

                return string.Format("{0}.{1}", serviceName.ToLowerInvariant(), host);
            }
        }

        public class LocalRegionEndPoints : RegionEndPoints            
        {
            internal LocalRegionEndPoints()
                : base("local", "Local (localhost)", "flags/local.png", new Dictionary<string, EndPoint>(), new string[]{})
            {
            }


            public void RegisterEndPoint(string serviceName, string url, string signer = null, string authRegion = null)
            {
                this._endpoints[serviceName] = new EndPoint(this.SystemName, url, signer, authRegion);
            }

            public void UpdateServiceEndPoint(string serviceName, string url, string signer = null, string authRegion = null)
            {
                this._endpoints[serviceName] = new EndPoint(this.SystemName, url, signer, authRegion);
            }
        }

        public class EndPoint
        {
            readonly string _regionSystemName;
            readonly string _url;
            readonly string _signer;
            private readonly string _authRegion;

            public EndPoint(string regionSystemName, string url, string signer = null, string authRegion = null)
            {
                this._regionSystemName = regionSystemName;
                this._url = url;
                this._signer = signer;
                this._authRegion = authRegion;
            }

            public string RegionSystemName
            {
                get { return this._regionSystemName; }
            }

            public string Url
            {
                get { return this._url; }
            }

            public string Signer
            {
                get { return this._signer; }
            }

            public string AuthRegion
            {
                get { return this._authRegion; }
            }
        }
    }
}
