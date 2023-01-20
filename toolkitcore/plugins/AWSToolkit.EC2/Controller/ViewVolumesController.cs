using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;
using Amazon.AWSToolkit.EC2.View.DataGrid;
using Amazon.AWSToolkit.Navigator;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.EC2.Model;

using log4net;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class ViewVolumesController : FeatureController<ViewVolumesModel>
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewVolumesController));

        private readonly ToolkitContext _toolkitContext;
        ViewVolumesControl _control;

        public ViewVolumesController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        protected override void DisplayView()
        {
            this._control = new ViewVolumesControl(this);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);
        }

        public void LoadModel()
        {
            RefreshVolumeList();
        }

        public void RefreshVolumeList()
        {
            var response = this.EC2Client.DescribeVolumes(new DescribeVolumesRequest());            

            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
                {
                    this.Model.Volumes.Clear();
                    foreach (var volume in response.Volumes)
                    {
                        this.Model.Volumes.Add(new VolumeWrapper(volume));
                    }
                }));
        }

        public void ResetSelection(IList<VolumeWrapper> volumes)
        {
            this.Model.SelectedVolumes.Clear();

            foreach (var vol in volumes)
            {
                this.Model.SelectedVolumes.Add(vol);
            }
            if (this.Model.SelectedVolumes.Count != 1)
            {
                this.Model.FocusVolume = null;
            }
            else
            {
                this.Model.FocusVolume = this.Model.SelectedVolumes[0];
                if (null == this.Model.FocusVolume.Snapshots)
                {
                    ThreadPool.QueueUserWorkItem((WaitCallback)(x => RefreshFocusSnapshots()));
                }
            }
        }

        public void RefreshFocusSnapshots()
        {
            if (null == this.Model.FocusVolume)
                return;

            var volume = this.Model.FocusVolume;

            try
            {
                var request = new DescribeSnapshotsRequest()
                {
                    Filters = new List<Filter>()
                    {
                        new Filter(){ Name = "volume-id", Values = new List<string>(){this.Model.FocusVolume.VolumeId}}
                    }
                };

                var response = EC2Client.DescribeSnapshots(request);

                ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
                {
                    if (null == volume.Snapshots)
                        volume.Snapshots = new ObservableCollection<SnapshotWrapper>();
                    else
                        volume.Snapshots.Clear();

                    foreach (var snapshot in response.Snapshots)
                    {
                        volume.Snapshots.Add(new SnapshotWrapper(snapshot));
                    }
                }));
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing snapshots", e);
            }
        }

        public ActionResults CreateVolume(ICustomizeColumnGrid grid)
        {
            var controller = new CreateVolumeController();
            var result = controller.Execute(this.EC2Client, this.Region.Id);

            if (result.Success)
            {
                RefreshVolumeList();

                var selectedVolume = Model.Volumes.FirstOrDefault(v => result.FocalName.Equals(v.VolumeId));
                if (selectedVolume != null)
                {
                    grid.SelectAndScrollIntoView(selectedVolume);
                }
            }
            return result;
        }

        public void CreateSnapshot(VolumeWrapper volume)
        {
            try
            {
                var controller = new CreateSnapshotController();
                var result = controller.Execute(EC2Client, volume);
                if (result.Success)
                {
                    ThreadPool.QueueUserWorkItem((WaitCallback)(x => RefreshFocusSnapshots()));
                }

                RecordCreateSnapshot(result);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error creating snapshot", e);
                _toolkitContext.ToolkitHost.ShowError("Create Snapshot Error", $"Error creating snapshot: {e.Message}");
                RecordCreateSnapshot(ActionResults.CreateFailed(e));
            }
        }

        public void DeleteSnapshots(IList<SnapshotWrapper> snapshots)
        {
            int count = 0;

            try
            {
                count = snapshots.Count;
                var controller = new DeleteSnapshotController();
                var result = controller.Execute(EC2Client, snapshots);
                RefreshFocusSnapshots();

                RecordDeleteSnapshots(count, result);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing list of snapshots", e);
                _toolkitContext.ToolkitHost.ShowError($"Error deleting snapshots: {e.Message}");
                RecordDeleteSnapshots(count, ActionResults.CreateFailed(e));
            }
        }

        public ActionResults DeleteVolume(IList<VolumeWrapper> volumes)
        {
            var controller = new DeleteVolumeController();
            var results = controller.Execute(EC2Client, volumes);
            if (results.Success)
            {
                this.RefreshVolumeList();
            }

            return results;
        }

        public ActionResults AttachVolume(VolumeWrapper volume)
        {
            var controller = new AttachVolumeController();
            var results = controller.Execute(EC2Client, volume);
            if (results.Success)
            {
                this.RefreshVolumeList();
            }

            return results;
        }

        public ActionResults DetachVolumeFocusInstance(IList<VolumeWrapper> volumes, bool force)
        {
            var controller = new DetachVolumeController(force);
            var results = controller.Execute(this.EC2Client, volumes);
            if (results.Success)
            {
                this.RefreshVolumeList();
            }

            return results;
        }

        public void RecordCreateVolume(ActionResults result)
        {
            var data = CreateMetricData<Ec2CreateVolume>(result, _toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();
            _toolkitContext.TelemetryLogger.RecordEc2CreateVolume(data);
        }

        public void RecordDeleteVolume(int count, ActionResults result)
        {
            var data = CreateMetricData<Ec2DeleteVolume>(result, _toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();
            data.Value = count;
            _toolkitContext.TelemetryLogger.RecordEc2DeleteVolume(data);
        }

        public void RecordEditVolumeAttachment(bool attached, ActionResults result)
        {
            var data = CreateMetricData<Ec2EditVolumeAttachment>(result, _toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();
            data.Enabled = attached;
            _toolkitContext.TelemetryLogger.RecordEc2EditVolumeAttachment(data);
        }

        private void RecordCreateSnapshot(ActionResults result)
        {
            var data = CreateMetricData<Ec2CreateSnapshot>(result, _toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();
            _toolkitContext.TelemetryLogger.RecordEc2CreateSnapshot(data);
        }

        private void RecordDeleteSnapshots(int count, ActionResults result)
        {
            var data = CreateMetricData<Ec2DeleteSnapshot>(result, _toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();
            data.Value = count;
            _toolkitContext.TelemetryLogger.RecordEc2DeleteSnapshot(data);
        }
    }
}
