using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.S3;
using Amazon.S3.Model;

using Amazon.AWSToolkit.S3.Nodes;

namespace Amazon.AWSToolkit.S3
{
    public static class S3Utils
    {
        public static void BuildS3ClientForBucket(Account.AccountViewModel account, IAmazonS3 startingClient, string bucketName, out IAmazonS3 regionSpecificClient, ref string overrideRegion)
        {

            try
            {
                var request = new GetBucketLocationRequest() { BucketName = bucketName };
                var response = startingClient.GetBucketLocation(request);

                switch (response.Location)
                {
                    case "":
                    case "US":
                        overrideRegion = "us-east-1";
                        break;
                    case "EU":
                        overrideRegion = "eu-west-1";
                        break;
                    default:
                        overrideRegion = response.Location;
                        break;

                }

                var region = RegionEndPointsManager.GetInstance().GetRegion(overrideRegion);
                if (region == null || region.GetEndpoint(RegionEndPointsManager.S3_SERVICE_NAME) == null)
                {
                    regionSpecificClient = startingClient;
                    return;
                }

                var endPoint = region.GetEndpoint(RegionEndPointsManager.S3_SERVICE_NAME);
                var config = BuildS3Config(endPoint);
                regionSpecificClient = new AmazonS3Client(account.Credentials, config);
            }
            catch (Exception e)
            {
                regionSpecificClient = startingClient;
                return;
            }
        }

        public static AmazonS3Config BuildS3Config(RegionEndPointsManager.EndPoint endpoint)
        {
            var config = new AmazonS3Config();
            endpoint.ApplyToClientConfig(config);
            return config;
        }

        public static AmazonS3Config BuildS3Config(string endpointUrl)
        {
            var config = new AmazonS3Config { ServiceURL = endpointUrl };
            return config;
        }
    }
}
