using System.Collections.Generic;

using Amazon.AWSToolkit.CommonValidators;
using Amazon.S3.Model;
using Amazon.S3;

using Moq;

using Xunit;

using Amazon;

namespace AWSToolkit.Tests.CommonValidators
{
    public class S3BucketLocationValidatorTests
    {
        private readonly Mock<IAmazonS3> _s3Client = new Mock<IAmazonS3>();
        private readonly string _sampleBucket = "testBucket";


        [Fact]
        public void ValidBucketLocation_WhenUSEast1()
        {
            SetupS3Client(null);
            var value = S3BucketLocationValidator.Validate(_s3Client.Object, _sampleBucket,
                RegionEndpoint.USEast1.SystemName);
            Assert.Null(value);
        }

        [Fact]
        public void ValidBucketLocation_WhenEU()
        {
            // S3Region.EUWest1 sets up an S3 region using `S3Region("EU")` and returns a location of "EU"
            SetupS3Client(S3Region.EUWest1);

            // SystemName for the EUWest1 RegionEndpoint is "eu-west-1"
            var value = S3BucketLocationValidator.Validate(_s3Client.Object, _sampleBucket,
                RegionEndpoint.EUWest1.SystemName);

            Assert.Null(value);
        }


        public static IEnumerable<object[]> ValidLocationData =>
            new List<object[]>
            {
                new object[] { S3Region.CNNorth1, RegionEndpoint.CNNorth1.SystemName },
                new object[] { S3Region.USWest2, RegionEndpoint.USWest2.SystemName },
                new object[] { S3Region.APSoutheast1, RegionEndpoint.APSoutheast1.SystemName },
            };

        [Theory]
        [MemberData(nameof(ValidLocationData))]
        public void ValidBucketLocation(S3Region bucketLocation, string expectedLocation)
        {
            SetupS3Client(bucketLocation);
            var value = S3BucketLocationValidator.Validate(_s3Client.Object, _sampleBucket, expectedLocation);
            Assert.Null(value);
        }

        public static IEnumerable<object[]> InvalidLocationData =>
            new List<object[]>
            {
                new object[] { S3Region.CNNorth1, RegionEndpoint.USEast2.SystemName },
                new object[] { S3Region.USWest2, RegionEndpoint.USEast2.SystemName },
                new object[] { S3Region.APSoutheast1, RegionEndpoint.USEast2.SystemName },
            };


        [Theory]
        [MemberData(nameof(InvalidLocationData))]
        public void InvalidBucketLocation(S3Region bucketLocation, string expectedLocation)
        {
            SetupS3Client(bucketLocation);
            var value = S3BucketLocationValidator.Validate(_s3Client.Object, _sampleBucket, expectedLocation);
            Assert.NotNull(value);
        }

        [Fact]
        public void Validator_DoesNotThrowOnError()
        {
            _s3Client.Setup(mock =>
                mock.GetBucketLocation(It.IsAny<GetBucketLocationRequest>())).Throws(new AmazonS3Exception("error"));
            var value = S3BucketLocationValidator.Validate(_s3Client.Object, _sampleBucket,
                RegionEndpoint.USEast1.SystemName);
            Assert.Null(value);
        }

        private void SetupS3Client(S3Region region)
        {
            var response = new GetBucketLocationResponse() { Location = region };
            _s3Client.Setup(mock =>
                mock.GetBucketLocation(It.IsAny<GetBucketLocationRequest>())).Returns(response);
        }
    }
}
