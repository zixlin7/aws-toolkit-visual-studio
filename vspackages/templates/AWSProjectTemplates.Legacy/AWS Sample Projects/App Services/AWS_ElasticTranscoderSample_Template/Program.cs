/*******************************************************************************
* Copyright 2009-2018 Amazon.com, Inc. or its affiliates. All Rights Reserved.
* 
* Licensed under the Apache License, Version 2.0 (the "License"). You may
* not use this file except in compliance with the License. A copy of the
* License is located at
* 
* http://aws.amazon.com/apache2.0/
* 
* or in the "license" file accompanying this file. This file is
* distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
* KIND, either express or implied. See the License for the specific
* language governing permissions and limitations under the License.
*******************************************************************************/
using System;
using System.IO;
using Amazon.ElasticTranscoder;
using Amazon.ElasticTranscoder.Model;

using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;


namespace $safeprojectname$
{
    class Program
    {
        // Policy assigned to the IAM role the pipeline is configured for.
        const string IAM_ROLE_POLICY =
            "{" +
            "    \"Version\" : \"2008-10-17\"," +
            "    \"Statement\" : [{" +
            "            \"Sid\" : \"1\"," +
            "            \"Effect\" : \"Allow\"," +
            "            \"Action\" : [\"s3:ListBucket\", \"s3:Put*\", \"s3:Get*\", \"s3:*MultipartUpload*\"]," +
            "            \"Resource\" : \"*\"" +
            "        }, {" +
            "            \"Sid\" : \"2\"," +
            "            \"Effect\" : \"Allow\"," +
            "            \"Action\" : \"sns:Publish\"," +
            "            \"Resource\" : \"*\"" +
            "        }, {" +
            "            \"Sid\" : \"3\"," +
            "            \"Effect\" : \"Deny\"," +
            "            \"Action\" : [\"s3:*Policy*\", \"sns:*Permission*\", \"s3:*Acl*\", \"sns:*Delete*\", \"s3:*Delete*\", \"sns:*Remove*\"]," +
            "            \"Resource\" : \"*\"" +
            "        }" +
            "    ]" +
            "}";

        // This field is added to the resources created to make sure their names are unique.
        static readonly string UNIQUE_POSTFIX = "-" + DateTime.Now.Ticks;

        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine("Incorrect commandline arguments");
                Console.Error.WriteLine("{0} <original-video> <output-filename> <notification-emailaddress>", Path.GetFileName(typeof(Program).Assembly.Location));
                return;
            }

            var sourceFile = args[0];
            var outputFilename = args[1];
            var email = args[2];

            // Create a topic the the pipeline to used for notifications
            var topicArn = CreateTopic(email);
            Console.WriteLine("Topic Created: {0}", topicArn);

            // Create the IAM role for the pipeline
            var role = CreateIamRole();
            Console.WriteLine("IAM Role Created: {0}", role.Arn);

            // Setup the S3 object keys for the input and output objects.
            var inputS3Key = "input/" + Path.GetFileName(sourceFile);
            var outputS3Key = "output/" + outputFilename;

            // Create a bucket that will be used for both inputs and outputs
            var bucketName = CreateBucket();
            Console.WriteLine("S3 Bucket Created: {0}", bucketName);
            Console.WriteLine("Uploading input file");

            // Upload the video to S3
            UploadVideo(bucketName, sourceFile, inputS3Key);
            Console.WriteLine("Input file uploaded");

            var etsClient = new AmazonElasticTranscoderClient();

            var notifications = new Notifications()
            {
                Completed = topicArn,
                Error = topicArn,
                Progressing = topicArn,
                Warning = topicArn
            };

            // Create the Elastic Transcoder pipeline for transcoding jobs to be submitted to.
            var pipeline = etsClient.CreatePipeline(new CreatePipelineRequest
            {
                Name = "MyVideos" + UNIQUE_POSTFIX,
                InputBucket = bucketName,
                OutputBucket = bucketName,
                Notifications = notifications,
                Role = role.Arn
            }).Pipeline;
            Console.WriteLine("Pipeline Created: {0}", pipeline.Name);


            // Create a job to transcode the input file
            etsClient.CreateJob(new CreateJobRequest
            {
                PipelineId = pipeline.Id,
                Input = new JobInput
                {
                    AspectRatio = "auto",
                    Container = "auto",
                    FrameRate = "auto",
                    Interlaced = "auto",
                    Resolution = "auto",
                    Key = inputS3Key
                },
                Output = new CreateJobOutput
                {
                    ThumbnailPattern = "",
                    Rotate = "0",
                    // Generic 720p: Go to http://docs.aws.amazon.com/elastictranscoder/latest/developerguide/create-job.html#PresetId to see a list of some
                    // of the support presets or call the ListPresets operation to get the full list of available presets
                    PresetId = "1351620000000-000010",
                    Key = outputS3Key
                }
            });

            Console.WriteLine("Job Submitted, Once completed output file can be found at {0} in bucket {1}", outputS3Key, bucketName);
            Console.WriteLine("Push enter to end program");
            Console.Read();
        }


        /// <summary>
        /// Utility method for creating at topic and subscribe the email address to it.
        /// </summary>
        /// <param name="emailAddress"></param>
        /// <returns></returns>
        static string CreateTopic(string emailAddress)
        {
            var snsClient = new AmazonSimpleNotificationServiceClient();
            var topicArn = snsClient.CreateTopic(new CreateTopicRequest
            {
                Name = "TranscodeEvents" + UNIQUE_POSTFIX
            }).TopicArn;

            snsClient.Subscribe(new SubscribeRequest
            {
                TopicArn = topicArn,
                Protocol = "email",
                Endpoint = emailAddress
            });

            return topicArn;
        }

        /// <summary>
        /// Create the IAM role that is used by the pipeline
        /// </summary>
        /// <returns></returns>
        static Role CreateIamRole()
        {
            var iamClient = new AmazonIdentityManagementServiceClient();
            var role = iamClient.CreateRole(new CreateRoleRequest
            {
                RoleName = "TranscodeRole" + UNIQUE_POSTFIX,
                AssumeRolePolicyDocument = "{\"Version\":\"2008-10-17\",\"Statement\":[{\"Effect\":\"Allow\",\"Principal\":{\"Service\":[\"elastictranscoder.amazonaws.com\"]},\"Action\":[\"sts:AssumeRole\"]}]}"
            }).Role;

            iamClient.PutRolePolicy(new PutRolePolicyRequest
            {
                PolicyName = "Default",
                RoleName = role.RoleName,
                PolicyDocument = IAM_ROLE_POLICY
            });

            return role;
        }

        /// <summary>
        /// Create the Amazon S3 bucket where the input file will be uploaded to and the transcoded output file will be put in.
        /// </summary>
        /// <returns></returns>
        static string CreateBucket()
        {
            var bucketName = "transcoder" + UNIQUE_POSTFIX;
            var s3Client = new AmazonS3Client();
            s3Client.PutBucket(new PutBucketRequest
            {
                BucketName = bucketName,
                UseClientRegion = true
            });

            return bucketName;
        }

        /// <summary>
        /// Upload the video to the Amazon S3 bucket.
        /// </summary>
        /// <param name="bucket"></param>
        /// <param name="filePath"></param>
        /// <param name="s3Key"></param>
        static void UploadVideo(string bucket, string filePath, string s3Key)
        {
            var s3Client = new AmazonS3Client();
            var utility = new TransferUtility(s3Client);

            utility.Upload(filePath, bucket, s3Key);
        }
    }
}