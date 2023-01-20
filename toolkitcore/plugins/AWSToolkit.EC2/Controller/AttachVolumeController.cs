using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;

using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class AttachVolumeController
    {
        AttachVolumeModel _model;
        IAmazonEC2 _ec2Client;
        ActionResults _results;
        VolumeWrapper _volume;

        public ActionResults Execute(IAmazonEC2 ec2Client, VolumeWrapper volume)
        {
            _volume = volume;
            _ec2Client = ec2Client;
            _model = new AttachVolumeModel() { VolumeId = _volume.VolumeId };
            _results = new ActionResults().WithSuccess(true);
            var control = new AttachVolumeControl(this);

            if (!ToolkitFactory.Instance.ShellProvider.ShowModal(control))
            {
                return ActionResults.CreateCancelled();
            }

            return _results;
        }

        public void LoadModel()
        {
            var request = new DescribeInstancesRequest()
            {
                Filters = new List<Filter>()
                {
                    new Filter(){Name = "availability-zone", Values = new List<string>(){_volume.NativeVolume.AvailabilityZone}}
                }
            };
            var response = _ec2Client.DescribeInstances(request);

            List<RunningInstanceWrapper> availableInstances = new List<RunningInstanceWrapper>();

            foreach (var reservation in response.Reservations)
            {
                foreach (var instance in reservation.Instances)
                {
                    availableInstances.Add(new RunningInstanceWrapper(reservation, instance));
                }
            }
            _model.AvailableInstances = availableInstances;
            if (_model.AvailableInstances.Count > 0)
                _model.Instance = _model.AvailableInstances[0];

            if (_model.Instance != null && _model.Instance.UnmappedDeviceSlots.Count > 0)
                _model.Device = _model.Instance.UnmappedDeviceSlots[0];
        }

        public AttachVolumeModel Model => _model;

        public void AttachVolume()
        {
            var request = new AttachVolumeRequest()
            {
                InstanceId = _model.Instance.NativeInstance.InstanceId,
                VolumeId = _model.VolumeId,
                Device = _model.Device
            };

            var response = _ec2Client.AttachVolume(request);
            string status = response.Attachment.State;
        }
         
    }
}
