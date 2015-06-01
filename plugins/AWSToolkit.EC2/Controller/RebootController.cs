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
    public class RebootController : BulkChangeController<IAmazonEC2, RunningInstanceWrapper>
    {
        protected override string Action
        {
            get { return "Reboot"; }
        }

        protected override void PerformAction(IAmazonEC2 ec2Client, RunningInstanceWrapper instance)
        {
            ec2Client.RebootInstances(new RebootInstancesRequest() { InstanceIds = new List<string>() { instance.NativeInstance.InstanceId } });
        }

        protected override string ConfirmMessage
        {
            get { return "Are you sure you want to reboot the instance(s):"; }
        }
    }
}
