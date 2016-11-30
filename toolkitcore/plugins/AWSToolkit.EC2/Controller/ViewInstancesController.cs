using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.EC2.Nodes;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;

using Amazon.EC2;
using Amazon.EC2.Model;

using log4net;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class ViewInstancesController : FeatureController<ViewInstancesModel>
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewInstancesController));

        ViewInstancesControl _control;

        protected override void DisplayView()
        {
            this._control = new ViewInstancesControl(this);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);
        }

        public IList<RunningInstanceWrapper> LaunchInstance()
        {
            var launchController = new LaunchController();
            var results = launchController.Execute(base.FeatureViewModel);
            if (results.Success)
            {
                this.RefreshInstances();

                var newIds = results.Parameters[EC2Constants.RESULTS_PARAMS_NEWIDS] as IEnumerable<string>;
                if (newIds == null)
                    return new List<RunningInstanceWrapper>();

                var newInstances = new List<RunningInstanceWrapper>();
                foreach (var instance in this.Model.RunningInstances)
                {
                    foreach (var newId in newIds)
                    {
                        if (string.Equals(newId, instance.NativeInstance.InstanceId))
                        {
                            newInstances.Add(instance);
                        }
                    }                    
                }
                return newInstances;
            }

            return new List<RunningInstanceWrapper>();
        }

        public void LoadModel()
        {
            RefreshInstances();
        }

        public void RefreshInstances()
        {
            var ipResponse = this.EC2Client.DescribeAddresses(new DescribeAddressesRequest());

            Dictionary<string, AddressWrapper> ipMap = new Dictionary<string, AddressWrapper>();
            ipResponse.Addresses.ForEach(x =>
                {
                    if (!string.IsNullOrEmpty(x.InstanceId))
                        ipMap[x.InstanceId] = new AddressWrapper(x);
                });

            var response = this.EC2Client.DescribeInstances(new DescribeInstancesRequest());
            
            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke((Action)(() =>
                {
                    this.Model.RunningInstances.Clear();
                    foreach (var reservation in response.Reservations)
                    {
                        foreach (var instance in reservation.Instances)
                        {
                            AddressWrapper address = null;
                            ipMap.TryGetValue(instance.InstanceId, out address);
                            this.Model.RunningInstances.Add(new RunningInstanceWrapper(reservation, instance, address));
                        }
                    }
                }));
        }

        public void UpdateSelection(IList<RunningInstanceWrapper> instances)
        {
            this.Model.SelectedInstances.Clear();
            foreach (var instance in instances)
            {
                this.Model.SelectedInstances.Add(instance);
            }

            if (this.Model.SelectedInstances.Count != 1)
                this.Model.FocusInstance = null;
            else
            {
                this.Model.FocusInstance = this.Model.SelectedInstances[0];
                if (this.Model.FocusInstance.Volumes == null)
                {
                    ThreadPool.QueueUserWorkItem((WaitCallback)(x => RefreshFocusVolumes()));
                }
            }
        }

        public void RefreshFocusVolumes()
        {
            if (this.Model.FocusInstance == null)
                return;
            var instance = this.Model.FocusInstance;

            try
            {
                var request = new DescribeVolumesRequest()
                {
                    Filters = new List<Filter>()
                    {
                        new Filter(){Name = "attachment.instance-id", Values = new List<string>(){instance.NativeInstance.InstanceId}}
                    }
                };

                var response = this.EC2Client.DescribeVolumes(request);

                ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((Action)(() =>
                {
                    if (instance.Volumes == null)
                        instance.Volumes = new ObservableCollection<VolumeWrapper>();
                    else
                        instance.Volumes.Clear();

                    foreach (var volume in response.Volumes)
                    {
                        if (isStillAttach(instance.NativeInstance.InstanceId, volume))
                            instance.Volumes.Add(new VolumeWrapper(volume));
                    }
                }));
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing volumes", e);
            }
        }

        bool isStillAttach(string instanceId, Volume volume)
        {
            foreach (var attachment in volume.Attachments)
            {
                if (string.Equals(instanceId, attachment.InstanceId))
                {
                    if (!string.Equals(attachment.State, EC2Constants.VOLUME_ATTACTMENT_STATUS_DETACHING))
                    {
                        return true;
                    }

                    return false;
                }
            }

            return false;
        }

        public void CreateVolumeFocusInstance()
        {
            CreateVolumeController controller = new CreateVolumeController(this.Model.FocusInstance);
            var results = controller.Execute(this.EC2Client, this.FeatureViewModel.RegionSystemName);
            if (results.Success)
            {
                this.RefreshFocusVolumes();
            }
        }

        public void DetachVolumeFocusInstance(IList<VolumeWrapper> volumes)
        {
            var controller = new DetachVolumeController(this.Model.FocusInstance, false);
            var results = controller.Execute(this.EC2Client, volumes);
            if (results.Success)
            {
                this.RefreshFocusVolumes();
            }
        }

        public void TerminateInstances(IList<RunningInstanceWrapper> instances)
        {
            var controller = new TerminateController();
            controller.Execute(this.EC2Client, instances);
        }

        public void RebootInstances(IList<RunningInstanceWrapper> instances)
        {
            var controller = new RebootController();
            controller.Execute(this.EC2Client, instances);
        }

        public void StopInstances(IList<RunningInstanceWrapper> instances)
        {
            var controller = new StopController();
            controller.Execute(this.EC2Client, instances);
        }

        public void StartInstances(IList<RunningInstanceWrapper> instances)
        {
            var controller = new StartController();
            controller.Execute(this.EC2Client, instances);
        }

        public void CreateImage(RunningInstanceWrapper instance)
        {
            var controller = new CreateImageController();
            controller.Execute(this.EC2Client, instance);
        }

        public void ChangeInstanceType(RunningInstanceWrapper instance)
        {
            var controller = new ChangeInstanceTypeController();
            controller.Execute(this.EC2Client, instance);
        }

        public RunningInstanceWrapper AssociatingElasticIP(RunningInstanceWrapper instance)
        {
            var controller = new AttachElasticIPToInstanceController();
            var results = controller.Execute(this.EC2Client, instance);
            if (!results.Success)
                return null;

            this.RefreshInstances();
            foreach (var item in this.Model.RunningInstances)
            {
                if (string.Equals(item.NativeInstance.InstanceId, instance.NativeInstance.InstanceId))
                    return item;
            }

            return null;
        }

        public RunningInstanceWrapper DisassociateElasticIP(RunningInstanceWrapper instance)
        {
            DisassociateAddressRequest request = null;
            if (string.IsNullOrEmpty(instance.VpcId))
                request = new DisassociateAddressRequest() { PublicIp = instance.NativeInstance.PublicIpAddress };
            else
            {
                var descResponse = this.EC2Client.DescribeAddresses(new DescribeAddressesRequest() { PublicIps = new List<string>() { instance.NativeInstance.PublicIpAddress } });
                if (descResponse.Addresses.Count != 1)
                    return null;

                request = new DisassociateAddressRequest() { AssociationId = descResponse.Addresses[0].AssociationId };
            }

            this.EC2Client.DisassociateAddress(request);

            this.RefreshInstances();
            foreach (var item in this.Model.RunningInstances)
            {
                if (string.Equals(item.NativeInstance.InstanceId, instance.NativeInstance.InstanceId))
                    return item;
            }

            return null;
        }

        public void ChangeShutdownBehavior(RunningInstanceWrapper instance)
        {
            var controller = new ChangeShutdownBehaviorController();
            controller.Execute(this.EC2Client, instance);
        }

        public void ChangeTerminationProtection(RunningInstanceWrapper instance)
        {
            var controller = new ChangeTerminationProtectionController();
            controller.Execute(this.EC2Client, instance);
        }

        public void ChangeUserData(RunningInstanceWrapper instance)
        {
            var controller = new ChangeUserDataController();
            controller.Execute(this.EC2Client, instance);
        }

        public void GetConsoleOutput(RunningInstanceWrapper instance)
        {
            var controller = new GetConsoleOutputController();
            controller.Execute(this.EC2Client, instance);
        }

        public void GetPassword(RunningInstanceWrapper instance)
        {
            var controller = new GetPasswordController();
            controller.Execute(this.FeatureViewModel, instance);
        }

        public void OpenRemoteDesktop(RunningInstanceWrapper instance)
        {
            var controller = new OpenRemoteDesktopController();
            controller.Execute(this.FeatureViewModel, instance);
        }

        public void OpenSSHSession(RunningInstanceWrapper instance)
        {
            var controller = new OpenSSHSessionController();
            controller.Execute(this.FeatureViewModel, instance);
        }

        public void OpenSCPSession(RunningInstanceWrapper instance)
        {
            var controller = new OpenSCPSessionController();
            controller.Execute(this.FeatureViewModel, instance);
        }
    }
}
