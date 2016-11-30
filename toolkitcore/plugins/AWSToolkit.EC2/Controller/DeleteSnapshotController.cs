using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.EC2.Nodes;
using Amazon.AWSToolkit.EC2.Model;

using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    class DeleteSnapshotController : BulkChangeController<IAmazonEC2, SnapshotWrapper>
    {
        protected override string Action
        {
            get { return "Delete"; }
        }

        protected override string ConfirmMessage
        {
            get { return "Are you sure you want to delete the snapshot(s):"; }
        }

        protected override void PerformAction(IAmazonEC2 ec2Client, SnapshotWrapper instance)
        {
            ec2Client.DeleteSnapshot(new DeleteSnapshotRequest() { SnapshotId = instance.NativeSnapshot.SnapshotId });
        }
    }
}
