using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;

using Amazon.EC2;
using Amazon.EC2.Model;

using log4net;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class CreateVolumeController
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(CreateVolumeController));
        const string NO_SNAPSHOT = "--- No Snapshot ---";
        
        ActionResults _results;
        IAmazonEC2 _ec2Client;
        CreateVolumeModel _model;
        string _region;
        RunningInstanceWrapper _instanceToAttach;

        public CreateVolumeController()
        {
        }

        public CreateVolumeController(RunningInstanceWrapper instanceToAttach)
        {
            this._instanceToAttach = instanceToAttach;
        }

        public ActionResults Execute(IAmazonEC2 ec2Client, string region)
        {
            _region = region;
            _model = new CreateVolumeModel() { InstanceToAttach = this._instanceToAttach };
            _ec2Client = ec2Client;

            var control = new CreateVolumeControl(this);
            control.SetupDeviceNameField();

            if (!ToolkitFactory.Instance.ShellProvider.ShowModal(control))
            {
                return ActionResults.CreateCancelled();
            }

            return _results ?? ActionResults.CreateFailed();
        }

        public void LoadModel()
        {
            Model.AvailabilityZoneList = AvailabilityZoneManager.Instance.AvailabilityZonesForRegion(_region, _ec2Client);
            if (this._model.InstanceToAttach == null)
                Model.AvailabilityZone = Model.AvailabilityZoneList[0];
            else
                Model.AvailabilityZone = Model.InstanceToAttach.NativeInstance.Placement.AvailabilityZone;

            loadSnapshots();
        }

        void loadSnapshots()
        {
            IList<SnapshotModel> snaps = new List<SnapshotModel>();
            snaps.Add(new SnapshotModel(NO_SNAPSHOT, "", "0"));

            var response = _ec2Client.DescribeSnapshots(new DescribeSnapshotsRequest() { OwnerIds = new List<string>() { "self" } });

            foreach (var snapshot in response.Snapshots)
            {
                string name = String.Empty;
                foreach (var tag in snapshot.Tags)
                {
                    if (tag.Key.ToLower().Equals("name"))
                    {
                        name = tag.Value;
                        break;
                    }
                }
                snaps.Add(new SnapshotModel(snapshot.SnapshotId, snapshot.Description, snapshot.VolumeSize.ToString(), name));
            }

            Model.AvailableSnapshots = snaps;
        }

        public CreateVolumeModel Model => _model;

        public void AdjustSizePerSnapshot()
        {
            if (_model.SnapshotId != null)
            {
                try
                {
                    _model.Size = Convert.ToInt32(_model.SizeOfSnapshot(_model.SnapshotId));
                }
                catch
                {
                    //Do nothing.
                }
            }
        }

        public string CreateVolume()
        {
            var request = new CreateVolumeRequest()
            {
                AvailabilityZone = _model.AvailabilityZone,
                Size = _model.Size
            };

            if (_model.SnapshotId != null && !_model.SnapshotId.Equals(NO_SNAPSHOT))
                request.SnapshotId = _model.SnapshotId;

            request.VolumeType = _model.VolumeType.TypeCode;
            if (_model.VolumeType.TypeCode.Equals(VolumeWrapper.ProvisionedIOPSTypeCode, StringComparison.OrdinalIgnoreCase))
                request.Iops = _model.Iops;

            var response = _ec2Client.CreateVolume(request);

            var volumeId = response.Volume.VolumeId;
            if (_model.TagsModel != null && !string.IsNullOrEmpty(_model.TagsModel.NameTag))
                _ec2Client.CreateTags(new CreateTagsRequest
                {
                    Resources = new List<string>() { response.Volume.VolumeId },
                    Tags = new List<Tag>()
                    {
                        new Tag()
                        {
                            Key = "Name",
                            Value = _model.TagsModel.NameTag
                        }
                    }
                });

            if (this._model.InstanceToAttach != null)
            {
                try
                {
                    string deviceName = _model.Device;
                    var attachRequest = new AttachVolumeRequest()
                    {
                        VolumeId = volumeId,
                        InstanceId = this._model.InstanceToAttach.NativeInstance.InstanceId,
                        Device = deviceName
                    };
                    this._ec2Client.AttachVolume(attachRequest);
                }
                catch
                {
                    var deleteRequest = new DeleteVolumeRequest() { VolumeId = volumeId };
                    try
                    {
                        this._ec2Client.DeleteVolume(deleteRequest);
                    }
                    catch (Exception e)
                    {
                        LOGGER.Error("Error deleting volume after failed attach: " + volumeId, e);
                    }
                    throw;
                }
            }

            this._results = new ActionResults().WithFocalname(volumeId).WithSuccess(true);
            return volumeId;
        }
    }
}
