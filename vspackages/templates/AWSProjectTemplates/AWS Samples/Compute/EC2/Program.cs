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
using System.Collections.Generic;
using System.Threading;

using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.EC2.Util;

using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;

using Amazon.Auth.AccessControlPolicy;
using Amazon.Auth.AccessControlPolicy.ActionIdentifiers;

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Statement = Amazon.Auth.AccessControlPolicy.Statement;

namespace $safeprojectname$
{
    /// <summary>
    /// This sample shows how to launch an Amazon EC2 instance with a PowerShell script that is executed when the 
    /// instance becomes available and access Amazon S3.
    /// </summary>
    class Program
    {
        // This script will show how to use the AWS Tools for PowerShell to create a bucket and write to it.
        // It will be set as the user data in the EC2 Instance created which will run once the EC2 instance is 
        // fully launched.
        const string USER_DATA_SCRIPT =
            "<powershell>\n" +
            "Import-Module \"C:\\Program Files (x86)\\AWS Tools\\PowerShell\\AWSPowerShell\\AWSPowerShell.psd1\"\n" +
            "Set-DefaultAWSRegion {0}\n" +
            "New-Item c:\\Data -type directory\n" +
            "Add-Content -path c:\\Data\\results.txt -value \"Results from lots of data processing\"\n" +
            "New-S3Bucket -BucketName {1}\n" +
            "Write-S3Object -BucketName {1} -File c:\\Data\\results.txt -Key results.txt\n" +
            "shutdown.exe /s\n" +
            "</powershell>";

        static readonly string RESOURCDE_POSTFIX = DateTime.Now.Ticks.ToString();

        public static void Main(string[] args)
        {
            var bucketName = "ec2-sample-" + RESOURCDE_POSTFIX;

            var ec2Client = new AmazonEC2Client();

            // Get latest 2012 Base AMI
            var imageId = ImageUtilities.FindImage(ec2Client, ImageUtilities.WINDOWS_2012_BASE).ImageId;
            Console.WriteLine("Using Image ID: {0}", imageId);

            // Create an IAM role with a profile that the Instance will use to run commands against AWS
            var instanceProfileArn = CreateInstanceProfile();
            Console.WriteLine("Created Instance Profile: {0}", instanceProfileArn);

            // Sleep for a little to make sure te profile is fully propagated.
            Thread.Sleep(15000);

            // Create key pair which will be used to demonstrate how get the Windows Administrator password.
            var keyPair = ec2Client.CreateKeyPair(new CreateKeyPairRequest { KeyName = "ec2-sample" + RESOURCDE_POSTFIX }).KeyPair;

            var runRequest = new RunInstancesRequest
            {
                ImageId = imageId,
                MinCount = 1,
                MaxCount = 1,
                KeyName = keyPair.KeyName,
                IamInstanceProfile = new IamInstanceProfileSpecification { Arn = instanceProfileArn },

                // Add the region for the S3 bucket and the name of the bucket to create
                UserData = EncodeToBase64(string.Format(USER_DATA_SCRIPT, RegionEndpoint.USWest2.SystemName, bucketName))
            };
            var instanceId = ec2Client.RunInstances(runRequest).Reservation.Instances[0].InstanceId;
            Console.WriteLine("Launch Instance {0}", instanceId);


            // Create the name tag
            ec2Client.CreateTags(new CreateTagsRequest
            {
                Resources = new List<string> { instanceId },
                Tags = new List<Amazon.EC2.Model.Tag> { new Amazon.EC2.Model.Tag { Key = "Name", Value = "Processor" } }
            });
            Console.WriteLine("Adding Name Tag to instance");


            Console.WriteLine("Waiting for EC2 Instance to stop");
            // The script put in the user data will shutdown the instance when it is complete.  Wait
            // till the instance has stopped which signals the script is done so the instance can be terminated.
            Instance instance = null;
            var instanceDescribeRequest = new DescribeInstancesRequest { InstanceIds = new List<string> { instanceId } };
            do
            {
                Thread.Sleep(10000);
                instance = ec2Client.DescribeInstances(instanceDescribeRequest).Reservations[0].Instances[0];

                if (instance.State.Name == "stopped")
                {
                    // Demonstrate how to get the Administrator password using the keypair.
                    var passwordResponse = ec2Client.GetPasswordData(new GetPasswordDataRequest
                    {
                        InstanceId = instanceId
                    });

                    // Make sure we actually got a password
                    if (passwordResponse.PasswordData != null)
                    {
                        var password = passwordResponse.GetDecryptedPassword(keyPair.KeyMaterial);
                        Console.WriteLine("The Windows Administrator password is: {0}", password);
                    }
                }
            } while (instance.State.Name == "pending" || instance.State.Name == "running");

            // Terminate instance
            ec2Client.TerminateInstances(new TerminateInstancesRequest
            {
                InstanceIds = new List<string>() { instanceId }
            });

            // Delete key pair created for sample.
            ec2Client.DeleteKeyPair(new DeleteKeyPairRequest { KeyName = keyPair.KeyName });

            var s3Client = new AmazonS3Client();
            var listResponse = s3Client.ListObjects(new ListObjectsRequest
            {
                BucketName = bucketName
            });
            if (listResponse.S3Objects.Count > 0)
            {
                Console.WriteLine("Found results file {0} in S3 bucket {1}", listResponse.S3Objects[0].Key, bucketName);
            }

            // Delete bucket created for sample.
            AmazonS3Util.DeleteS3BucketWithObjects(s3Client, bucketName);
            Console.WriteLine("Deleted S3 bucket created for sample.");

            DeleteInstanceProfile();
            Console.WriteLine("Delete Instance Profile created for sample.");

            Console.WriteLine("Instance terminated, push enter to exit the program");
            Console.Read();
        }

        /// <summary>
        /// Create the instance profile that will give permission for the EC2 instance to make request to Amazon S3.
        /// </summary>
        /// <returns></returns>
        static string CreateInstanceProfile()
        {
            var roleName = "ec2-sample-" + RESOURCDE_POSTFIX;
            var client = new AmazonIdentityManagementServiceClient();
            client.CreateRole(new CreateRoleRequest
            {
                RoleName = roleName,
                AssumeRolePolicyDocument = @"{""Statement"":[{""Principal"":{""Service"":[""ec2.amazonaws.com""]},""Effect"":""Allow"",""Action"":[""sts:AssumeRole""]}]}"
            });

            var statement = new Statement(Statement.StatementEffect.Allow);
            statement.Actions.Add(S3ActionIdentifiers.AllS3Actions);
            statement.Resources.Add(new Resource("*"));

            var policy = new Policy();
            policy.Statements.Add(statement);

            client.PutRolePolicy(new PutRolePolicyRequest
            {
                RoleName = roleName,
                PolicyName = "S3Access",
                PolicyDocument = policy.ToJson()
            });

            var response = client.CreateInstanceProfile(new CreateInstanceProfileRequest
            {
                InstanceProfileName = roleName
            });

            client.AddRoleToInstanceProfile(new AddRoleToInstanceProfileRequest
            {
                InstanceProfileName = roleName,
                RoleName = roleName
            });

            return response.InstanceProfile.Arn;
        }

        /// <summary>
        /// Delete the instance profile created for the sample.
        /// </summary>
        static void DeleteInstanceProfile()
        {
            var roleName = "ec2-sample-" + RESOURCDE_POSTFIX;
            var client = new AmazonIdentityManagementServiceClient();

            client.DeleteRolePolicy(new DeleteRolePolicyRequest
            {
                RoleName = roleName,
                PolicyName = "S3Access"
            });

            client.RemoveRoleFromInstanceProfile(new RemoveRoleFromInstanceProfileRequest
            {
                InstanceProfileName = roleName,
                RoleName = roleName
            });

            client.DeleteRole(new DeleteRoleRequest
            {
                RoleName = roleName
            });

            client.DeleteInstanceProfile(new DeleteInstanceProfileRequest
            {
                InstanceProfileName = roleName
            });
        }

        static string EncodeToBase64(string str)
        {
            byte[] toEncodeAsBytes = System.Text.Encoding.UTF8.GetBytes(str);
            string returnValue = System.Convert.ToBase64String(toEncodeAsBytes);
            return returnValue;
        }
    }
}
