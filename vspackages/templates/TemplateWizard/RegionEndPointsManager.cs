using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Reflection;
using System.Xml.Linq;
using Amazon.AWSToolkit.Persistence;

namespace TemplateWizard
{
    public class RegionEndPointsManager
    {
        const string CLOUDFRONT_LOCATION_OF_REGIONS_FILE = "https://d3rrggjwfhwld2.cloudfront.net/ServiceEndPoints.xml";
        const string S3_LOCATION_OF_REGIONS_FILE = "https://aws-vs-toolkit.s3.amazonaws.com/ServiceEndPoints.xml";

        public IList<RegionEndpoint> GetRegions()
        {
            IList<RegionEndpoint> regions;
            try
            {
                string content = GetServiceEndpointContent();

                XDocument xdoc = XDocument.Parse(content);
                var query = from p in xdoc.Elements("regions").Elements("region")
                            select new RegionEndpoint
                            {
                                SystemName = p.Element("systemname").Value,
                                DisplayName = p.Element("displayname").Value
                            };

                regions = query.ToList();
            }
            catch
            {
                regions = new List<RegionEndpoint>
                {
                    new RegionEndpoint("us-east-1", "US East (Virginia)"),
                    new RegionEndpoint("us-west-1", "US West (N. California)"),
                    new RegionEndpoint("us-west-2", "US West (Oregon)"),
                    new RegionEndpoint("eu-west-1", "EU West (Ireland)"),
                    new RegionEndpoint("ap-northeast-1", "Asia Pacific (Tokyo)"),
                    new RegionEndpoint("ap-southeast-1", "Asia Pacific (Singapore)"),
                    new RegionEndpoint("ap-southeast-2", "Asia Pacific (Sydney)"),
                    new RegionEndpoint("sa-east-1", "South America (Sao Paulo)")
                };
            }

            return regions;
        }



        private string GetServiceEndpointContent()
        {
            HttpWebResponse response = null;

            var configuredLocation = PersistenceManager.Instance.GetSetting("HostedFilesLocation");
            if (!string.IsNullOrEmpty(configuredLocation))
            {
                if (configuredLocation.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                {
                    using (var reader = new StreamReader(Path.Combine(configuredLocation.Substring(7), "ServiceEndPoints.xml")))
                    {
                        return reader.ReadToEnd();
                    }
                }

                if (configuredLocation.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var httpRequest = WebRequest.Create(CLOUDFRONT_LOCATION_OF_REGIONS_FILE) as HttpWebRequest;
                        response = httpRequest.GetResponse() as HttpWebResponse;
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            if (response == null)
            {
                try
                {
                    var httpRequest = WebRequest.Create(CLOUDFRONT_LOCATION_OF_REGIONS_FILE) as HttpWebRequest;
                    response = httpRequest.GetResponse() as HttpWebResponse;
                }
                catch
                {
                    var httpRequest = WebRequest.Create(S3_LOCATION_OF_REGIONS_FILE) as HttpWebRequest;
                    response = httpRequest.GetResponse() as HttpWebResponse;
                }
            }

            using (response)
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                return reader.ReadToEnd();
            }
        }

        public class RegionEndpoint
        {
            public RegionEndpoint() { }

            public RegionEndpoint(string systemName, string displayName)
            {
                SystemName = systemName;
                DisplayName = displayName;
            }

            public string DisplayName
            {
                get;
                set;
            }

            public string SystemName
            {
                get;
                set;
            }

            public override string ToString()
            {
                return string.Format("{0} [{1}]", DisplayName, SystemName);
            }
        }
    }
}
