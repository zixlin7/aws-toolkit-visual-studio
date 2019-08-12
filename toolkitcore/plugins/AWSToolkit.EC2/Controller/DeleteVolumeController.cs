using Amazon.AWSToolkit.EC2.Model;

using Amazon.EC2;
using Amazon.EC2.Model;


namespace Amazon.AWSToolkit.EC2.Controller
{
    public class DeleteVolumeController : BulkChangeController<IAmazonEC2, VolumeWrapper>
    {
        protected override string Action => "Delete";

        protected override string ConfirmMessage => "Are you sure you want to delete the volume(s)";

        protected override void PerformAction(IAmazonEC2 ec2Client, VolumeWrapper volume)
        {
            ec2Client.DeleteVolume(new DeleteVolumeRequest() { VolumeId = volume.VolumeId });
        }
    }
}
