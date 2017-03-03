/*******************************************************************************
* Copyright 2009-2013 Amazon.com, Inc. or its affiliates. All Rights Reserved.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Diagnostics;
using System.Threading;

using Amazon;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.S3;
using Amazon.S3.Model;


namespace $safeprojectname$
{
    class Program
    {

        public const string S3_READONLY_POLICY =
                                "{" +
                                "	\"Statement\" : [{" +
                                "			\"Action\" : [\"s3:Get*\"]," +
                                "			\"Effect\" : \"Allow\"," +
                                "			\"Resource\" : \"*\"" +
                                "		}" +
                                "	]" +
                                "}";

        public static void Main(string[] args)
        {
            var iamClient = new AmazonIdentityManagementServiceClient();

            // Create the IAM user
            var readOnlyUser = iamClient.CreateUser(new CreateUserRequest
            {
                UserName = "S3 Read Only Access"
            }).User;

            // Assign the read only policy to the new user
            iamClient.PutUserPolicy(new PutUserPolicyRequest
            {
                UserName = readOnlyUser.UserName,
                PolicyName = "S3ReadOnlyAccess",
                PolicyDocument = S3_READONLY_POLICY
            });

            // Create an access key for the IAM user that can be used by the SDK
            var accessKey = iamClient.CreateAccessKey(new CreateAccessKeyRequest
            {
                UserName = readOnlyUser.UserName
            }).AccessKey;

            // Create an S3 client with the new IAM user's access key
            var s3Client = new AmazonS3Client(accessKey.AccessKeyId, accessKey.SecretAccessKey);

            // Example of making an S3 Get request using the S3 client configured for the IAM user
            var request = new GetObjectRequest
            {
                BucketName = "your-bucket",
                Key = "your-text-file.txt"
            };
            using (var response = s3Client.GetObject(request))
            {
                response.WriteResponseStreamToFile(@"c:\temp\your-text-file.txt");
            }
        }
    }
}