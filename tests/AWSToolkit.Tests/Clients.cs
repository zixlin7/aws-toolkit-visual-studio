using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.S3;
using Amazon.SQS;
using Amazon.CloudFormation;
using Amazon.Common.TestCredentials;

namespace Amazon.AWSToolkit.Tests
{
    public static class Clients
    {
        private static TestCredentials defaultCredentials = TestCredentials.DefaultCredentials;

        public static string ACCESS_KEY_ID = defaultCredentials.AccessKey;
        public static string SECRET_KEY_ID = defaultCredentials.SecretKey;

        static AmazonS3Client _s3Client;
        public static AmazonS3Client S3Client
        {
            get
            {
                if (_s3Client == null)
                {
                    AmazonS3Config config = new AmazonS3Config();
                    _s3Client = new AmazonS3Client(ACCESS_KEY_ID, SECRET_KEY_ID, config);
                }
                return _s3Client;
            }
        }

        static AmazonSQSClient _sqsClient;
        public static AmazonSQSClient SQSClient
        {
            get
            {
                if (_sqsClient == null)
                {
                    AmazonSQSConfig config = new AmazonSQSConfig();
                    _sqsClient = new AmazonSQSClient(ACCESS_KEY_ID, SECRET_KEY_ID, config);
                }
                return _sqsClient;
            }
        }

        static AmazonCloudFormationClient _cfClient;
        public static AmazonCloudFormationClient CloudFormationClient
        {
            get
            {
                if (_cfClient == null)
                {
                    AmazonCloudFormationConfig config = new AmazonCloudFormationConfig { RegionEndpoint = RegionEndpoint.USEast1 };
                    _cfClient = new AmazonCloudFormationClient(ACCESS_KEY_ID, SECRET_KEY_ID, config);
                }
                return _cfClient;
            }
        }

    }
}
