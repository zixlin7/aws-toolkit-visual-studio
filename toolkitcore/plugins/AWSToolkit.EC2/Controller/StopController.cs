using System.Collections.Generic;
using Amazon.AWSToolkit.EC2.Model;

using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class StopController : BulkChangeController<IAmazonEC2, RunningInstanceWrapper>
    {
        protected override string Action => "Stop";

        protected override string ConfirmMessage => "Are you sure you want to stop the instance(s):";

        protected override void PerformAction(IAmazonEC2 ec2Client, RunningInstanceWrapper instance)
        {
            ec2Client.StopInstances(new StopInstancesRequest() { InstanceIds = new List<string>() { instance.NativeInstance.InstanceId } });
        }
    }
}
