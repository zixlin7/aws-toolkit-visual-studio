using LitJson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using BuildCommon;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;

namespace BuildTasks
{
    // Legacy method:  This has been replaced by automatic EC2QuickLaunch.json file generation.
    // See https://w.amazon.com/bin/view/AWS/DevEx/IDEToolkits/HostedFilesDevelopment/ for details.
    public class EC2QuickLaunchGeneratorTask : BuildTaskBase
    {
        // each regional bucket contains one of these files, which we assemble into one single file
        // for the toolkit to consume
        const string QUICKSTART_PROD_FILE = "quickstart_prod";

        // the pattern will be suffixed with each internal region name, to get to the quickstart file
        const string REGION_TOKEN = "{region}";
        const string REGIONAL_BUCKET_NAME_PATTERN = "aws-ec2-ux-quickstart-" + REGION_TOKEN;

        class RegionInfo
        {
            /// <summary>
            /// The region the data is for
            /// </summary>
            public RegionEndpoint Region { get; set; }
            /// <summary>
            /// Non-standard credentials to use if necessary (eg for BJS); if null/empty, our
            /// default credentials are used.
            /// </summary>
            public string CredentialSetOverride { get; set; }
        }

        readonly SortedDictionary<string, RegionInfo> SupportedRegions = new SortedDictionary<string, RegionInfo>
        {
            { "iad", new RegionInfo { Region = RegionEndpoint.USEast1 } },
            { "cmh", new RegionInfo { Region = RegionEndpoint.USEast2 } },
            { "sfo", new RegionInfo { Region = RegionEndpoint.USWest1 } },
            { "pdx", new RegionInfo { Region = RegionEndpoint.USWest2 } },
            { "nrt", new RegionInfo { Region = RegionEndpoint.APNortheast1 } },
            { "icn", new RegionInfo { Region = RegionEndpoint.APNortheast2 } },
            { "bom", new RegionInfo { Region = RegionEndpoint.APSouth1 } },
            { "sin", new RegionInfo { Region = RegionEndpoint.APSoutheast1 } },
            { "syd", new RegionInfo { Region = RegionEndpoint.APSoutheast2 } },
            { "fra", new RegionInfo { Region = RegionEndpoint.EUCentral1 } },
            { "dub", new RegionInfo { Region = RegionEndpoint.EUWest1 } },
            { "lhr", new RegionInfo { Region = RegionEndpoint.EUWest2} },
            { "cdg", new RegionInfo { Region = RegionEndpoint.EUWest3} },
            { "gru", new RegionInfo { Region = RegionEndpoint.SAEast1 } },
            { "yul", new RegionInfo { Region = RegionEndpoint.CACentral1 } },
            { "bjs", new RegionInfo { Region = RegionEndpoint.CNNorth1, CredentialSetOverride = "BJS.SDKUpload" } },
            { "zhy", new RegionInfo { Region = RegionEndpoint.CNNorthWest1, CredentialSetOverride = "BJS.SDKUpload" } },
        };

        const string EC2_QUICKLAUNCH_FILE = "EC2QuickLaunch.json";

        const string TOOLKIT_LOCATION = "https://aws-vs-toolkit.s3.amazonaws.com/EC2QuickLaunch.json";

        public override bool Execute()
        {
            CheckWaitForDebugger();

            Log.LogMessage("Starting inspection of EC2 team's quicklaunch content");

            if (File.Exists(EC2_QUICKLAUNCH_FILE))
            {
                Console.WriteLine("Found existing json content; deleting...");
                File.Delete(EC2_QUICKLAUNCH_FILE);
            }

            var regionalContents = new List<string>();

            Console.WriteLine("Received EC2 team's regional content from S3...");
            foreach (var r in SupportedRegions.Keys)
            {
                Console.WriteLine("...fetching content for region '{0}'", SupportedRegions[r].Region.SystemName);
                try
                {
                    var content = getContentFromRegionalBucket(r, SupportedRegions[r]);
                    regionalContents.Add(content);
                }
                catch (Exception e)
                {
                    Log.LogError("ERROR: Failed to fetch quickstart data for region {0}, exception {1}", SupportedRegions[r].Region.SystemName, e);
                    return false;
                }
            }

            var ec2TransformedContent = transformContent(regionalContents);
            Console.WriteLine("Transformed EC2 content...");

            var toolkitContent = getContentFromS3(TOOLKIT_LOCATION);
            Console.WriteLine("Received Toolkit's content from S3...");

            if (string.Equals(toolkitContent, ec2TransformedContent))
            {
                Log.LogMessage("EC2 quick launch config is up to date");
                return true;
            }

            Log.LogError("ERROR: EC2 quick launch config is out of date");
            Log.LogError(ec2TransformedContent);

            var cwd = Directory.GetCurrentDirectory();
            var outputFile = Path.Combine(cwd, EC2_QUICKLAUNCH_FILE);
            Console.WriteLine("\r\nLatest content written to file {0}\r\n", outputFile);
            File.WriteAllText(outputFile, ec2TransformedContent, Encoding.UTF8);

            return false;
        }

        static string transformContent(IEnumerable<string> content)
        {
            var consolidated = new StringBuilder("{");

            foreach (var c in content)
            {
                var t = TransformEC2Json(c);

                if (consolidated.Length > 1)
                    consolidated.Append(",");
                consolidated.Append(t.Substring(1, t.Length - 2));
            }

            consolidated.Append("}");

            JsonData document = JsonMapper.ToObject(consolidated.ToString());

            Action<JsonData> addDefaultImage = null;
            addDefaultImage = node =>
            {
                if (node.IsArray)
                {
                    foreach (JsonData child in node)
                    {
                        addDefaultImage(child);
                    }
                }
                if (node.IsObject)
                {

                    if (node.SafeGet("imageId32") == null && node.SafeGet("imageId64") == null)
                    {
                        foreach (var key in (node as System.Collections.IDictionary).Keys)
                        {
                            var child = node[(string)key];
                            if (child.IsObject || child.IsArray)
                                addDefaultImage(child);
                        }
                    }
                    else
                    {
                        if (node.SafeGet("imageId64") != null)
                            node["imageId"] = node["imageId64"];
                        else if (node.SafeGet("imageId32") != null)
                            node["imageId"] = node["imageId32"];
                    }
                }
            };

            addDefaultImage(document);
            return document.ToJson();
        }

        static string getContentFromS3(string url)
        {
            Console.WriteLine("Fetching EC2 team's content from {0}...", url);
            var httpRequest = WebRequest.Create(url) as HttpWebRequest;

            var result = httpRequest.BeginGetResponse(null, null);
            if (!result.AsyncWaitHandle.WaitOne(30 * 1000))
            {
                Console.WriteLine("Timeing out trying to reach URL: " + url);
                System.Environment.Exit(-2);
            }


            using (var response = httpRequest.EndGetResponse(result) as HttpWebResponse)
            {
                Console.WriteLine("...opening response stream...");
                var stream = response.GetResponseStream();
                string content;
                using (var reader = new StreamReader(stream))
                {
                    content = reader.ReadToEnd();
                }

                Console.WriteLine("...returning content...");
                return content;
            }
        }

        static string getContentFromRegionalBucket(string internalName, RegionInfo regionInfo)
        {
            var bucketName = REGIONAL_BUCKET_NAME_PATTERN.Replace(REGION_TOKEN, internalName);

            string content = null;

            using (var s3Client = GetS3Client(regionInfo.Region, regionInfo.CredentialSetOverride))
            {
                var response = s3Client.GetObject(bucketName, QUICKSTART_PROD_FILE);
                using (var reader = new StreamReader(response.ResponseStream))
                {
                    content = reader.ReadToEnd();
                }
            }

            return content;
        }

        static AmazonS3Client GetS3Client(RegionEndpoint region, string credentialSet)
        {
            AWSCredentials credentials;
            if (string.IsNullOrEmpty(credentialSet))
            {
                Console.WriteLine("Attempting to load default credentials");
                credentials = UploadCredentials.DefaultAWSCredentials;
            }
            else
            {
                Console.WriteLine("Attempting to load credentials with ID '{0}'", credentialSet);
                credentials = UploadCredentials.AWSCredentials(credentialSet);
                if (credentials == null)
                    throw new ArgumentException("Unable to find credentials with name " + credentialSet);
            }

            Console.WriteLine("Constructed S3 client from credentials with access key {0}", credentials.GetCredentials().AccessKey);
            return new AmazonS3Client(credentials, region);
        }

        static string TransformEC2Json(string original)
        {
            var t = original
                        .Replace("freeTier': False", "freeTier': 'False'")
                        .Replace("freeTier': True", "freeTier': 'True'")
                        .Replace("freeTier\": false", "freeTier\": 'False'")
                        .Replace("freeTier\": true", "freeTier\": 'True'")
                        .Replace("isMarketplace': False", "isMarketplace': 'False'")
                        .Replace("isMarketplace': True", "isMarketplace': 'True'")
                        .Replace("isMarketplace\": false", "isMarketplace\": 'False'")
                        .Replace("isMarketplace\": true", "isMarketplace\": 'True'")
                        .Replace(" u'", " '")
                        .Replace("{u'", "{'")
                        .Replace("[u'", "['");

            // latest EC2 files have description and title as sub-objects, so hoist out
            // to root level as simple strings for backwards compatibility
            JsonData document = JsonMapper.ToObject(t.ToString());

            // expect single root key to be the region, then the amiList to be a keyed array
            // within
            if (document.Count != 1)
            {
                var msg = string.Format("Expected single object (the region) in document, received {0}\nData = {1}", document.Count, t);
                throw new InvalidDataException(msg);
            }

            var amiList = document[0]["amiList"];
            foreach (JsonData ami in amiList)
            {
                var originalDescription = ami["description"];
                var descriptionText = originalDescription["en"];
                ami["description"] = descriptionText;

                var originalTitle = ami["title"];
                var titleText = originalTitle["en"];
                ami["title"] = titleText;
            }

            return document.ToJson();
        }
    }
}
