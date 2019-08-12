using Amazon.AWSToolkit.EC2.Model;

using Amazon.EC2;
using Amazon.EC2.Model;


namespace Amazon.AWSToolkit.EC2.Controller
{
    public class DeleteSecurityGroupController : BulkChangeController<IAmazonEC2, SecurityGroupWrapper>
    {
        protected override string Action => "Delete";

        protected override string ConfirmMessage => "Are you sure you want to delete the security group(s)";

        protected override void PerformAction(IAmazonEC2 ec2Client, SecurityGroupWrapper group)
        {
            ec2Client.DeleteSecurityGroup(new DeleteSecurityGroupRequest() { GroupId = group.NativeSecurityGroup.GroupId });
        }
    }
}
