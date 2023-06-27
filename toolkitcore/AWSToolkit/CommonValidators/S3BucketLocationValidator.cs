using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon.S3.Model;
using Amazon.S3;

namespace Amazon.AWSToolkit.CommonValidators
{
    /// <summary>
    /// Validates whether the region of the S3 bucket is the same as the given region
    /// </summary>
    public class S3BucketLocationValidator
    {
        public static string Validate(IAmazonS3 s3Client, string bucket, string region)
        {
            try
            {
                var request = new GetBucketLocationRequest() { BucketName = bucket };
                var response = s3Client.GetBucketLocation(request);
                var location = response.Location.Value;

                // https://docs.aws.amazon.com/AmazonS3/latest/API/API_GetBucketLocation.html
                // buckets in region us-east-1 are returned with the location value as null
                if (string.IsNullOrWhiteSpace(location))
                {
                    location = "us-east-1";
                }

                if (!string.Equals(location, region))
                {
                    return $"Bucket is not in the same region {location} as the currently selected region {region}";
                }

                return null;
            }

            catch (Exception ex)
            {
                // silently swallow any errors(for eg. due to permission issues) with this validation to unblock users in UI
                return null;
            }
        }
    }
}
