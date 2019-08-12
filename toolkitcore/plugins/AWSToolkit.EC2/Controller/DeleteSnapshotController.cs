using Amazon.AWSToolkit.EC2.Model;

using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    class DeleteSnapshotController : BulkChangeController<IAmazonEC2, SnapshotWrapper>
    {
        protected override string Action => "Delete";

        protected override string ConfirmMessage => "Are you sure you want to delete the snapshot(s):";

        protected override void PerformAction(IAmazonEC2 ec2Client, SnapshotWrapper instance)
        {
            ec2Client.DeleteSnapshot(new DeleteSnapshotRequest() { SnapshotId = instance.NativeSnapshot.SnapshotId });
        }
    }
}
