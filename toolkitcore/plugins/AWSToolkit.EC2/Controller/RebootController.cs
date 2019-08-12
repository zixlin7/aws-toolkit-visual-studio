using System.Collections.Generic;
using Amazon.AWSToolkit.EC2.Model;

using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class RebootController : BulkChangeController<IAmazonEC2, RunningInstanceWrapper>
    {
        protected override string Action => "Reboot";

        protected override void PerformAction(IAmazonEC2 ec2Client, RunningInstanceWrapper instance)
        {
            ec2Client.RebootInstances(new RebootInstancesRequest() { InstanceIds = new List<string>() { instance.NativeInstance.InstanceId } });
        }

        protected override string ConfirmMessage => "Are you sure you want to reboot the instance(s):";
    }
}
