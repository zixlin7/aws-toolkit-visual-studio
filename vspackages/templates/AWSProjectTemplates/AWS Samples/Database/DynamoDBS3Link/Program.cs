using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;

using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2;
using Amazon.S3.Model;
using Amazon.S3;
using System.Collections.Specialized;

namespace $safeprojectname$
{
    static class Program
    {
        public static string BucketName = "S3LinkBucket";
        private static IAmazonS3 client;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static void Main(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine("Setting up S3 client");
            using (Program.client = new AmazonS3Client())
            {
                Console.WriteLine();
                Console.WriteLine("Creating sample bucket");
                CreateABucket();
            }
            
            Console.WriteLine();
            Console.WriteLine("Setting up DynamoDB client");
            AmazonDynamoDBClient client = new AmazonDynamoDBClient();
            
            Console.WriteLine();
            Console.WriteLine("Creating sample tables");
            TableOperations.CreateSampleTables(client);

            Console.WriteLine();
            Console.WriteLine("Creating the context object");
            DynamoDBContext context = new DynamoDBContext(client);

            Console.WriteLine();
            Console.WriteLine("Running S3Link sample");
            S3LinkOperations.RunOperations(context);

            Console.WriteLine();
            Console.WriteLine("Removing sample tables");
            TableOperations.DeleteSampleTables(client);

            Console.WriteLine();
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
        }

        static void CreateABucket()
        {
            try
            {
                PutBucketRequest request = new PutBucketRequest();
                request.BucketName = BucketName;
                Program.client.PutBucket(request);
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null && (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") || amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    Console.WriteLine("Please check the provided AWS Credentials.");
                    Console.WriteLine("If you haven't signed up for Amazon S3, please visit http://aws.amazon.com/s3");
                }
                else
                {
                    Console.WriteLine("An Error, number {0}, occurred when creating a bucket with the message '{1}", amazonS3Exception.ErrorCode, amazonS3Exception.Message);
                }
            }
        }
    }
}
