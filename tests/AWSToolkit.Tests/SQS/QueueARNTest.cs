using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Amazon.SQS;

namespace Amazon.AWSToolkit.Tests.SQS
{
    [TestClass]
    public class QueueARNTest
    {
        [TestMethod]
        public void ConvertQueueURLToARN()
        {
            string arn = Clients.SQSClient.GetQueueARN("us-east-1", "https://queue.amazonaws.com/599169622985/MyQueue");
            Assert.AreEqual("arn:aws:sqs:us-east-1:599169622985:MyQueue", arn);
        }
    }
}
