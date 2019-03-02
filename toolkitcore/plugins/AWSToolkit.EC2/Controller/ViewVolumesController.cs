using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.EC2.Nodes;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;

using Amazon.EC2.Model;

using log4net;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class ViewVolumesController : FeatureController<ViewVolumesModel>
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewVolumesController));

        ViewVolumesControl _control;
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

        public VolumeWrapper CreateVolume()
        {
            var controller = new CreateVolumeController();
            var result = controller.Execute(this.EC2Client, this.RegionSystemName);
            if (result.Success)
            {
                RefreshVolumeList();
                foreach (var vol in Model.Volumes)
                {
                    if (result.FocalName.Equals(vol.VolumeId))
                        return vol;
                }
            }
            return null;
        }

        public void CreateSnapshot(VolumeWrapper volume)
        {
            var controller = new CreateSnapshotController();
            var result = controller.Execute(EC2Client, volume);
            if (result.Success)
            {
                ThreadPool.QueueUserWorkItem((WaitCallback)(x => RefreshFocusSnapshots()));
            }
        }

        public void DeleteSnapshots(IList<SnapshotWrapper> snapshots)
        {
            var controller = new DeleteSnapshotController();
            var results = controller.Execute(EC2Client, snapshots);
            RefreshFocusSnapshots();
        }

        public void DeleteVolume(IList<VolumeWrapper> volumes)
        {
            var controller = new DeleteVolumeController();
            var results = controller.Execute(EC2Client, volumes);
            if (results.Success)
            {
                this.RefreshVolumeList();
            }
        }

        public void AttachVolume(VolumeWrapper volume)
        {
            var controller = new AttachVolumeController();
            var results = controller.Execute(EC2Client, volume);
            if (results.Success)
            {
                this.RefreshVolumeList();
            }
        }

        public void DetachVolumeFocusInstance(IList<VolumeWrapper> volumes, bool force)
        {
            var controller = new DetachVolumeController(force);
            var results = controller.Execute(this.EC2Client, volumes);
            if (results.Success)
            {
                this.RefreshVolumeList();
            }
        }
    }
}
