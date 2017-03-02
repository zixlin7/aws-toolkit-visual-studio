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

using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace $safeprojectname$
{
    class Program
    {
        public static void Main(string[] args)
        {
            var sns = new AmazonSimpleNotificationServiceClient();
            string emailAddress = string.Empty;

            while (string.IsNullOrEmpty(emailAddress))
            {
                Console.Write("Please enter an email address to use: ");
                emailAddress = Console.ReadLine();
            }

            try
            {
                // Create topic
                Console.WriteLine("Creating topic...");
                var topicArn = sns.CreateTopic(new CreateTopicRequest
                {
                    Name = "SampleSNSTopic"
                }).TopicArn;

                // Set display name to a friendly value
                Console.WriteLine();
                Console.WriteLine("Setting topic attributes...");
                sns.SetTopicAttributes(new SetTopicAttributesRequest
                {
                    TopicArn = topicArn,
                    AttributeName = "DisplayName",
                    AttributeValue = "Sample Notifications"
                });

                // List all topics
                Console.WriteLine();
                Console.WriteLine("Retrieving all topics...");
                var listTopicsRequest = new ListTopicsRequest();
                ListTopicsResponse listTopicsResponse;
                do
                {
                    listTopicsResponse = sns.ListTopics(listTopicsRequest);
                    foreach (var topic in listTopicsResponse.Topics)
                    {
                        Console.WriteLine(" Topic: {0}", topic.TopicArn);

                        // Get topic attributes
                        var topicAttributes = sns.GetTopicAttributes(new GetTopicAttributesRequest
                        {
                            TopicArn = topic.TopicArn
                        }).Attributes;
                        if (topicAttributes.Count > 0)
                        {
                            Console.WriteLine(" Topic attributes");
                            foreach (var topicAttribute in topicAttributes)
                            {
                                Console.WriteLine(" -{0} : {1}", topicAttribute.Key, topicAttribute.Value);
                            }
                        }
                        Console.WriteLine();
                    }
                    listTopicsRequest.NextToken = listTopicsResponse.NextToken;
                } while (listTopicsResponse.NextToken != null);

                // Subscribe an endpoint - in this case, an email address
                Console.WriteLine();
                Console.WriteLine("Subscribing email address {0} to topic...", emailAddress);
                sns.Subscribe(new SubscribeRequest
                {
                    TopicArn = topicArn,
                    Protocol = "email",
                    Endpoint = emailAddress
                });

                // When using email, recipient must confirm subscription
                Console.WriteLine();
                Console.WriteLine("Please check your email and press enter when you are subscribed...");
                Console.ReadLine();

                // Publish message
                Console.WriteLine();
                Console.WriteLine("Publishing message to topic...");
                sns.Publish(new PublishRequest
                {
                    Subject = "Test",
                    Message = "Testing testing 1 2 3",
                    TopicArn = topicArn
                });

                // Verify email receieved
                Console.WriteLine();
                Console.WriteLine("Please check your email and press enter when you receive the message...");
                Console.ReadLine();

                // Delete topic
                Console.WriteLine();
                Console.WriteLine("Deleting topic...");
                sns.DeleteTopic(new DeleteTopicRequest
                {
                    TopicArn = topicArn
                });
            }
            catch (AmazonSimpleNotificationServiceException ex)
            {
                Console.WriteLine("Caught Exception: " + ex.Message);
                Console.WriteLine("Response Status Code: " + ex.StatusCode);
                Console.WriteLine("Error Code: " + ex.ErrorCode);
                Console.WriteLine("Error Type: " + ex.ErrorType);
                Console.WriteLine("Request ID: " + ex.RequestId);
            }

            Console.WriteLine();
            Console.WriteLine("Press enter to exit...");
            Console.ReadLine();
        }
    }
}