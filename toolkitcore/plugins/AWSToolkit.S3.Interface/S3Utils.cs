using System;

using Amazon.AWSToolkit.Regions;
using Amazon.S3;
using Amazon.S3.Model;

namespace Amazon.AWSToolkit.S3
{
    public static class S3Utils
    {
        private static readonly string S3ServiceName =
            new AmazonS3Config().RegionEndpointServiceName;

        public static void BuildS3ClientForBucket(Account.AccountViewModel account, IAmazonS3 startingClient, string bucketName, IRegionProvider regionProvider, out IAmazonS3 regionSpecificClient, ref string overrideRegion)
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

                var region = regionProvider.GetRegion(overrideRegion);
                if (region == null || !regionProvider.IsServiceAvailable(S3ServiceName, overrideRegion))
                {
                    regionSpecificClient = startingClient;
                    return;
                }
                regionSpecificClient = account.CreateServiceClient<AmazonS3Client>(region);
            }
            catch (Exception)
            {
                regionSpecificClient = startingClient;
                return;
            }
        }
    }
}
