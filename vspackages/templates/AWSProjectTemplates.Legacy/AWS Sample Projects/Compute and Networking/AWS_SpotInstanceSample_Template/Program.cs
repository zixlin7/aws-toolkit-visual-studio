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
using System.Threading;

using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.EC2.Util;

namespace $safeprojectname$
{
    /// <summary>
    /// This sample shows how to create EC2 spot instance request for low cost computing and monitor the request.
    /// </summary>
    class Program
    {
        public static void Main(string[] args)
        {
            var ec2Client = new AmazonEC2Client();

            // Get latest 2012 Base AMI
            var imageId = ImageUtilities.FindImage(ec2Client, ImageUtilities.WINDOWS_2012_BASE).ImageId;

            // Initializes a Spot Instance Request for 1 x t1.micro instance with a bid price of $0.03.
            var requestRequest = new RequestSpotInstancesRequest
            {
                SpotPrice = "0.03",
                InstanceCount = 1
            };

            // Setup the specifications of the launch. This includes the instance type (e.g. t1.micro)
            // and the latest Amazon Linux AMI id available. Note, you should always use the latest
            // Amazon Linux AMI id or another of your choosing.
            var launchSpecification = new LaunchSpecification()
            {
                ImageId = imageId,
                InstanceType = "t1.micro"
            };

            // Add the security group to the request.
            launchSpecification.SecurityGroups = new List<string>() { "default" };

            // Add the launch specifications to the request.
            requestRequest.LaunchSpecification = launchSpecification;

            // Call the RequestSpotInstance API.
            var requestResponse = ec2Client.RequestSpotInstances(requestRequest);

            // Setup an arraylist to collect all of the request ids we want to watch hit the running
            // state.
            var spotInstanceRequestIds = new List<String>();

            // Add all of the request ids to the hashset, so we can determine when they hit the
            // active state.
            foreach (var spotInstanceRequest in requestResponse.SpotInstanceRequests)
            {
                Console.WriteLine("Created Spot Request: " + spotInstanceRequest.SpotInstanceRequestId);
                spotInstanceRequestIds.Add(spotInstanceRequest.SpotInstanceRequestId);
            }

            // Create a variable that will track whether there are any requests still in the open state.
            bool anyOpen;

            // Initialize variables.
            var instanceIds = new List<String>();

            do
            {
                // Create the describeRequest with tall of the request id to monitor (e.g. that we started).
                var describeRequest = new DescribeSpotInstanceRequestsRequest
                {
                    SpotInstanceRequestIds = spotInstanceRequestIds
                };

                // Initialize the anyOpen variable to false ??? which assumes there are no requests open unless
                // we find one that is still open.
                anyOpen = false;

                try
                {
                    // Retrieve all of the requests we want to monitor.
                    var describeResponse = ec2Client.DescribeSpotInstanceRequests(describeRequest);

                    // Look through each request and determine if they are all in the active state.
                    foreach (var spotInstanceRequest in describeResponse.SpotInstanceRequests)
                    {

                        // If the state is open, it hasn't changed since we attempted to request it.
                        // There is the potential for it to transition almost immediately to closed or
                        // cancelled so we compare against open instead of active.
                        if (spotInstanceRequest.State.Equals("open"))
                        {
                            anyOpen = true;
                            break;
                        }

                        // Add the instance id to the list we will eventually terminate.
                        instanceIds.Add(spotInstanceRequest.InstanceId);
                    }
                }
                catch (AmazonEC2Exception)
                {
                    // If we have an exception, ensure we don't break out of the loop.
                    // This prevents the scenario where there was blip on the wire.
                    anyOpen = true;
                }

                // Sleep for 60 seconds.
                Thread.Sleep(60 * 1000);
            } while (anyOpen);

            try
            {
                // Cancel requests.
                var cancelRequest = new CancelSpotInstanceRequestsRequest { SpotInstanceRequestIds = spotInstanceRequestIds };
                ec2Client.CancelSpotInstanceRequests(cancelRequest);
            }
            catch (AmazonEC2Exception e)
            {
                // Write out any exceptions that may have occurred.
                Console.WriteLine("Error cancelling instances");
                Console.WriteLine("Caught Exception: " + e.Message);
                Console.WriteLine("Reponse Status Code: " + e.StatusCode);
                Console.WriteLine("Error Code: " + e.ErrorCode);
                Console.WriteLine("Request ID: " + e.RequestId);
            }

            try
            {
                // Terminate instances.
                var terminateRequest = new TerminateInstancesRequest { InstanceIds = instanceIds };
                ec2Client.TerminateInstances(terminateRequest);
            }
            catch (AmazonEC2Exception e)
            {
                // Write out any exceptions that may have occurred.
                Console.WriteLine("Error terminating instances");
                Console.WriteLine("Caught Exception: " + e.Message);
                Console.WriteLine("Reponse Status Code: " + e.StatusCode);
                Console.WriteLine("Error Code: " + e.ErrorCode);
                Console.WriteLine("Request ID: " + e.RequestId);
            }


            Console.WriteLine("Program ended, push enter to exit the program");
            Console.Read();
        }
    }
}
