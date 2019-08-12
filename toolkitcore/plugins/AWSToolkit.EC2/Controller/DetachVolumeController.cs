using Amazon.AWSToolkit.EC2.Model;

using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class DetachVolumeController : BulkChangeController<IAmazonEC2, VolumeWrapper>
    {
        RunningInstanceWrapper _attachInstance;
        bool _force;
        public DetachVolumeController(bool force)
        {
            this._force = force;
        }

        public DetachVolumeController(RunningInstanceWrapper attachInstance, bool force)
            : this(force)
        {
            this._attachInstance = attachInstance;
        }
        
        protected override string Action
        {
            get 
            {
                if (this._force)
                    return "Force Detach";

                return "Detach"; 
            }
        }

        protected override string ConfirmMessage
        {
            get 
            {
                if (this._force)
                    return "Are you sure you want to detach the volume(s) by force:"; 

                return "Are you sure you want to detach the volume(s):"; 
            }
        }

        protected override void PerformAction(IAmazonEC2 ec2Client, VolumeWrapper volumn)
        {
            var request = new DetachVolumeRequest() { VolumeId = volumn.NativeVolume.VolumeId };

            if (this._attachInstance != null)
            {
                request.InstanceId = this._attachInstance.NativeInstance.InstanceId;
            }

            ec2Client.DetachVolume(request);
        }
    }
}
