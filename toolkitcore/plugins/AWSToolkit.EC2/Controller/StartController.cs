using System.Collections.Generic;
using Amazon.AWSToolkit.EC2.Model;

using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class StartController : BulkChangeController<IAmazonEC2, RunningInstanceWrapper>
    {
        protected override string Action => "Start";

        protected override string ConfirmMessage => "Are you sure you want to start the instance(s):";

        protected override void PerformAction(IAmazonEC2 ec2Client, RunningInstanceWrapper instance)
        {
            ec2Client.StartInstances(new StartInstancesRequest() { InstanceIds = new List<string>() { instance.NativeInstance.InstanceId } });
        }
    }
}
