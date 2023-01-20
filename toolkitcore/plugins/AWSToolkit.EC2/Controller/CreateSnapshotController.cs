using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;

using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class CreateSnapshotController
    {
        ActionResults _results;
        IAmazonEC2 _ec2Client;
        CreateSnapshotModel _model;
        VolumeWrapper _volume;

        public ActionResults Execute(IAmazonEC2 ec2Client, VolumeWrapper volume)
        {
            _model = new CreateSnapshotModel();
            _model.VolumeId = volume.VolumeId;
            _ec2Client = ec2Client;
            _volume = volume;

            _results = new ActionResults().WithSuccess(true);

            var control = new CreateSnapshotControl(this);
            if (!ToolkitFactory.Instance.ShellProvider.ShowModal(control))
            {
                return ActionResults.CreateCancelled();
            }

            return _results;
        }

        public CreateSnapshotModel Model => _model;

        public string CreateSnapshot()
        {
            var request = new CreateSnapshotRequest() { VolumeId = _model.VolumeId };
            if (_model.Description != null)
                request.Description = _model.Description;

            var response = _ec2Client.CreateSnapshot(request);

            _results.Parameters.Add("SnapshotId", response.Snapshot.SnapshotId);

            _ec2Client.CreateTags(new CreateTagsRequest()
            {
                Resources = new List<string>(){ response.Snapshot.SnapshotId},
                Tags = new List<Tag>()
                {
                    new Tag(){Key = "Name", Value = _model.Name}
                }
            });

            return response.Snapshot.SnapshotId;
        }
    }
}
