using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.EC2.Nodes;
using Amazon.AWSToolkit.EC2.Model;

using Amazon.EC2;
using Amazon.EC2.Model;


namespace Amazon.AWSToolkit.EC2.Controller
{
    public class DeregisterAMIController : BulkChangeController<IAmazonEC2, ImageWrapper>
    {
        protected override string Action
        {
            get { return "De-register"; }
        }

        protected override string ConfirmMessage
        {
            get { return "Are you sure you want to de-register the image(s):"; }
        }

        protected override void PerformAction(IAmazonEC2 ec2Client, ImageWrapper image)
        {
            ec2Client.DeregisterImage(new DeregisterImageRequest() { ImageId = image.NativeImage.ImageId });
        }
    }
}
