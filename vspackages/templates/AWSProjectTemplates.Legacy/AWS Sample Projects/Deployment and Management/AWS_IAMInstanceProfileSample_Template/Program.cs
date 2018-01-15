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
using System.Linq;
using System.Text;
using System.Threading;

using Amazon;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.EC2.Util;


namespace $safeprojectname$
{

    /// <summary>
    /// This sample shows how to launch an Amazon EC2 instance with an Instance Profile from AWS Identity and Access Management.
    /// AWS SDKs running on the EC2 instance can use the Instance Profile for credentials.  For the .AWS SDK for .NET this is done
    /// by creating a client without passing in any credentials.
    /// </summary>
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

        static void Main(string[] args)
        {
            var iamClient = new AmazonIdentityManagementServiceClient();

            // Create the IAM role
            var role = iamClient.CreateRole(new CreateRoleRequest
            {
                RoleName = "S3ReadOnlyAccess",
                AssumeRolePolicyDocument = @"{""Statement"":[{""Principal"":{""Service"":[""ec2.amazonaws.com""]},""Effect"":""Allow"",""Action"":[""sts:AssumeRole""]}]}"
            }).Role;

            // Assign the S3 read only access to the role
            iamClient.PutRolePolicy(new PutRolePolicyRequest
            {
                RoleName = role.RoleName,
                PolicyName = "S3ReadOnlyAccess",
                PolicyDocument = S3_READONLY_POLICY
            });

            // Create the instance profile which will be assign to the EC2 instance when it launches
            var profile = iamClient.CreateInstanceProfile(new CreateInstanceProfileRequest
            {
                InstanceProfileName = "S3ReadOnlyAccess"
            }).InstanceProfile;

            // Assign the role to the instance profile.
            iamClient.AddRoleToInstanceProfile(new AddRoleToInstanceProfileRequest
            {
                InstanceProfileName = profile.InstanceProfileName,
                RoleName = role.RoleName
            });

            // Wait a bit for the instance profile to propaged to all the regions.
            Thread.Sleep(10000);

            var ec2Client = new AmazonEC2Client();

            // Launch an EC2 instance with the instance profiles.
            ec2Client.RunInstances(new RunInstancesRequest
            {
                ImageId = ImageUtilities.FindImage(ec2Client, ImageUtilities.WINDOWS_2012_BASE).ImageId,
                InstanceType = "t1.micro",
                MinCount = 1,
                MaxCount = 1,
                SecurityGroups = new List<string> { "default" },
                IamInstanceProfile = new IamInstanceProfileSpecification { Arn = profile.Arn }
            });
        }
    }
}