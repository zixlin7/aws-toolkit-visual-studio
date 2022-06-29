using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.S3.IO;
using Amazon.S3.Model;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Integration.Plugins.S3
{
    public class S3IntegrationTest
    {
        private static readonly string RegionId = "us-west-2";

        private AmazonS3Client _s3Client;

        // Doesn't validate all rules, but covers most bases for random names.
        // https://docs.aws.amazon.com/AmazonS3/latest/userguide/bucketnamingrules.html
        private static readonly Regex BucketNameRegex = new Regex(@"^[a-z0-9][a-z0-9.-]{1,61}[a-z0-9]$");

        /// <summary>
        /// This test ensures the workaround in <see cref="Amazon.S3.IO.S3File"/> is working as expected.
        /// </summary>
        /// <remarks>
        /// The AWS SDK for .NET strips a leading slash from the source and destination keys during a CopyObject call.
        /// To workaround this issue, S3File.Copy() pads an extra slash to the beginning of each key.  If the behavior
        /// of the AWS SDK for .NET should change, this integration test is designed to catch it.
        ///
        /// See https://github.com/aws/aws-sdk-net/issues/1720 for additional details.
        /// </remarks>
        [Fact]
        public async Task CopyObjectShouldStripLeadingSlashFromSourceAndDestinationKeys()
        {
            var bucketName = await PutBucket();
            try
            {
                var sourceDir = "/sourceDir/";
                var destinationDir = "/destinationDir/";
                var sourceFile = S3Path.Combine(sourceDir, "file");
                var destinationFile = S3Path.Combine(destinationDir, "file");

                await PutObject(bucketName, sourceFile);
                await PutObject(bucketName, destinationDir);

                // Sanity check bucket contents as expected
                var keys = await ListObjects(bucketName);
                Assert.Equal(2, keys.Count);
                Assert.Contains(sourceFile, keys);
                Assert.Contains(destinationDir, keys);

                // The method to be tested
                S3File.Copy(await GetS3Client(), bucketName, sourceFile, bucketName, destinationFile);

                // Ensure copy worked as expected with no lingering other directories or files
                keys = await ListObjects(bucketName);
                Assert.Equal(3, keys.Count);
                Assert.Contains(sourceFile, keys);
                Assert.Contains(destinationDir, keys);
                Assert.Contains(destinationFile, keys);
            }
            finally
            {
                await DeleteBucket(bucketName);
            }
        }

        #region Private Helper Method(s)

        private async Task<string> PutBucket(string bucketName = null)
        {
            if (bucketName == null)
            {
                bucketName = GenerateBucketName();
            }

            var s3Client = await GetS3Client();
            s3Client.PutBucket(bucketName);
            
            return bucketName;
        }

        private async Task DeleteBucket(string bucketName)
        {
            var s3Client = await GetS3Client();

            while (true)
            {
                var keys = await ListObjects(bucketName);
                if (keys.Count == 0)
                {
                    break;
                }
                DeleteObjects(bucketName, keys);
            }

            s3Client.DeleteBucket(bucketName);
        }

        private async Task PutObject(string bucketName, string key)
        {
            var s3Client = await GetS3Client();
            var request = new PutObjectRequest()
            {
                BucketName = bucketName,
                Key = key
            };

            s3Client.PutObject(request);
        }

        private async Task DeleteObjects(string bucketName, List<string> keys)
        {
            var s3Client = await GetS3Client();
            var request = new DeleteObjectsRequest()
            {
                BucketName = bucketName,
                Objects = keys.Select(k => new KeyVersion() {Key = k}).ToList()
            };

            s3Client.DeleteObjects(request);
        }

        private async Task<List<string>> ListObjects(string bucketName)
        {
            var s3Client = await GetS3Client();
            var request = new ListObjectsV2Request() { BucketName = bucketName };

            var response = s3Client.ListObjectsV2(request);

            return response.S3Objects.Select(o => o.Key).ToList();
        }

        private string GenerateBucketName(string prefix = nameof(S3IntegrationTest))
        {
            var bucketName = $"{nameof(S3IntegrationTest)}-{Guid.NewGuid()}".ToLower();
            AssertValidBucketName(bucketName);
            return bucketName;
        }

        private void AssertValidBucketName(string bucketName)
        {
            Assert.True(BucketNameRegex.IsMatch(bucketName), $"Bucket name '{bucketName}' is invalid.  See https://docs.aws.amazon.com/AmazonS3/latest/userguide/bucketnamingrules.html");
        }

        private async Task<AmazonS3Client> GetS3Client()
        {
            if (_s3Client == null)
            {
                var credentials = await GetCredentials();
                _s3Client = new AmazonS3Client(credentials, RegionEndpoint.GetBySystemName(RegionId));
            }

            return _s3Client;
        }

        private Task<AWSCredentials> GetCredentials()
        {
            var chain = new CredentialProfileStoreChain();
            chain.TryGetAWSCredentials("default", out var awsCredentials);
            return Task.FromResult(awsCredentials);
        }

        #endregion
    }
}
