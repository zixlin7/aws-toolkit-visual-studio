using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.EC2.Commands;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.Repositories;
using Amazon.AWSToolkit.EC2.View;
using Amazon.AWSToolkit.EC2.ViewModels;
using Amazon.AWSToolkit.Navigator;
using Amazon.EC2.Model;

using log4net;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class ViewInstancesController : FeatureController<ViewInstancesModel>
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ViewInstancesController));

        private readonly ToolkitContext _toolkitContext;
        private IInstanceRepository _instanceRepository;
        private ViewInstancesViewModel _viewModel;
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
            var elasticIpRepository = factory.CreateElasticIpRepository(AwsConnectionSettings);

            _viewModel = new ViewInstancesViewModel(Model, _instanceRepository, elasticIpRepository, _toolkitContext);
            Model.ViewSystemLog = new GetInstanceLogCommand(_viewModel, AwsConnectionSettings, _toolkitContext);
            Model.CreateImage = new CreateImageFromInstanceCommand(_viewModel, AwsConnectionSettings, _toolkitContext);
            Model.ChangeTerminationProtection = new ChangeTerminationProtectionCommand(_viewModel, AwsConnectionSettings, _toolkitContext);
            Model.ChangeUserData = new ChangeUserDataCommand(_viewModel, AwsConnectionSettings, _toolkitContext);
            Model.ChangeInstanceType = new ChangeInstanceTypeCommand(_viewModel, AwsConnectionSettings, _toolkitContext);
            Model.ChangeShutdownBehavior = new ChangeShutdownBehaviorCommand(_viewModel, AwsConnectionSettings, _toolkitContext);
            Model.AttachElasticIp = new AttachElasticIpToInstanceCommand(_viewModel, AwsConnectionSettings, _toolkitContext);
            Model.DetachElasticIp = new DetachElasticIpFromInstanceCommand(_viewModel, AwsConnectionSettings, _toolkitContext);

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
            // This is currently the cleanest way we have to properly run async code from a sync location.
            _toolkitContext.ToolkitHost.ExecuteOnUIThread(async () => await _viewModel.ReloadInstancesAsync());
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
