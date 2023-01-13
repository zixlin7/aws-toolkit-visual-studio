using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.EC2.Commands;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;
using Amazon.AWSToolkit.Navigator;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.EC2.Model;

using log4net;
using Amazon.AWSToolkit.EC2.Repositories;
using Amazon.AWSToolkit.EC2.ViewModels;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class ViewInstancesController : FeatureController<ViewInstancesModel>
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ViewInstancesController));

        private readonly ToolkitContext _toolkitContext;
        private IInstanceRepository _instanceRepository;
        ViewInstancesControl _control;

        public ViewInstancesController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        protected override void DisplayView()
        {
            if (!(_toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(IEc2RepositoryFactory)) is
                    IEc2RepositoryFactory factory))
            {
                Debug.Assert(!Debugger.IsAttached, $"Plugin factory {nameof(IEc2RepositoryFactory)} is missing. The Toolkit is unable to perform EC2 Instance operations.");
                throw new NotSupportedException("AWS Toolkit was unable to get details about EC2 instances");
            }

            _instanceRepository = factory.CreateInstanceRepository(AwsConnectionSettings);
            var viewModel = new ViewInstancesViewModel(Model, _instanceRepository);
            Model.ViewSystemLog = new GetInstanceLogCommand(viewModel, AwsConnectionSettings, _toolkitContext);

            this._control = new ViewInstancesControl(this);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);
        }

        public IList<RunningInstanceWrapper> LaunchInstance()
        {
            var launchController = new LaunchController(_toolkitContext);
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
            
            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
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

                ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
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
                Logger.Error("Error refreshing volumes", e);
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
            var results = controller.Execute(this.EC2Client, this.FeatureViewModel.Region.Id);
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
            ExecuteAndRecordChangeState(() => controller.Execute(this.EC2Client, instances), Ec2InstanceState.Terminate);
        }

        public void RebootInstances(IList<RunningInstanceWrapper> instances)
        {
            var controller = new RebootController();
            ExecuteAndRecordChangeState(() => controller.Execute(this.EC2Client, instances), Ec2InstanceState.Reboot);
        }

        public void StopInstances(IList<RunningInstanceWrapper> instances)
        {
            var controller = new StopController();
            ExecuteAndRecordChangeState(() => controller.Execute(this.EC2Client, instances), Ec2InstanceState.Stop);
        }

        public void StartInstances(IList<RunningInstanceWrapper> instances)
        {
            var controller = new StartController();
            ExecuteAndRecordChangeState(() => controller.Execute(this.EC2Client, instances), Ec2InstanceState.Start);
        }

        private void ExecuteAndRecordChangeState(Func<ActionResults> fnExecute, Ec2InstanceState instanceState)
        {
            var result = fnExecute();
            _toolkitContext.TelemetryLogger.RecordEc2ChangeState(new Ec2ChangeState()
            {
                AwsAccount = AwsConnectionSettings.GetAccountId(_toolkitContext.ServiceClientManager) ?? MetadataValue.NotSet,
                AwsRegion = AwsConnectionSettings.Region.Id,
                Ec2InstanceState = instanceState,
                Result = result.AsTelemetryResult(),
            });
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

        public void GetPassword(RunningInstanceWrapper instance)
        {
            var controller = new GetPasswordController();
            controller.Execute(this.FeatureViewModel, instance);
        }

        public void OpenRemoteDesktop(RunningInstanceWrapper instance)
        {
            var controller = new OpenRemoteDesktopController(_toolkitContext);
            controller.Execute(AwsConnectionSettings, instance);
        }

        public void OpenSSHSession(RunningInstanceWrapper instance)
        {
            var controller = new OpenSSHSessionController(_toolkitContext);
            controller.Execute(AwsConnectionSettings, instance);
        }

        public void OpenSCPSession(RunningInstanceWrapper instance)
        {
            var controller = new OpenSCPSessionController(_toolkitContext);
            controller.Execute(AwsConnectionSettings, instance);
        }
    }
}
