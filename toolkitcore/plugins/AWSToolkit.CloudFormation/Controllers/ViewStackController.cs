using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.CloudFormation.Nodes;
using Amazon.AWSToolkit.CloudFormation.Model;
using Amazon.AWSToolkit.CloudFormation.View;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;

using Amazon.AWSToolkit.EC2.Nodes;

using Amazon.AutoScaling;
using Amazon.AutoScaling.Model;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.CloudWatch;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.ElasticLoadBalancing;
using Amazon.ElasticLoadBalancing.Model;
using Amazon.RDS;
using Amazon.RDS.Model;

using log4net;
using System.Threading;

namespace Amazon.AWSToolkit.CloudFormation.Controllers
{
    public class ViewStackController : BaseContextCommand
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(ViewStackController));

        readonly object UPDATE_EVENT_LOCK_OBJECT = new object();

        IEC2InstancesViewModel _ec2InstanceViewModel;
        CloudFormationStackViewModel _stackModel;
        ViewStackModel _model;

        IAmazonAutoScaling _asClient;
        IAmazonCloudFormation _cloudFormationClient;
        IAmazonEC2 _ec2Client;
        IAmazonElasticLoadBalancing _elbClient;
        IAmazonCloudWatch _cwClient;
        IAmazonRDS _rdsClient;


        public override ActionResults Execute(IViewModel model)
        {
//            Thread.Sleep(4000);

            this._stackModel = model as CloudFormationStackViewModel;
            if (this._stackModel == null)
                return new ActionResults().WithSuccess(false);

            String region = this._stackModel.CloudFormationRootViewModel.CurrentEndPoint.RegionSystemName;
            RegionEndPointsManager.RegionEndPoints endPoints = RegionEndPointsManager.GetInstance().GetRegion(region);

            this._cloudFormationClient = this._stackModel.CloudFormationClient;

            var ec2Model = this._stackModel.AccountViewModel.FindSingleChild<IEC2RootViewModel>(false);
            if (ec2Model != null)
            {
                this._ec2InstanceViewModel = ec2Model.FindSingleChild<IEC2InstancesViewModel>(false);
            }

            var elbConfig = new AmazonElasticLoadBalancingConfig {ServiceURL = endPoints.GetEndpoint(RegionEndPointsManager.ELB_SERVICE_NAME).Url};
            this._elbClient = new AmazonElasticLoadBalancingClient(this._stackModel.AccountViewModel.Credentials, elbConfig);

            var asConfig = new AmazonAutoScalingConfig {ServiceURL = endPoints.GetEndpoint(RegionEndPointsManager.AUTOSCALING_SERVICE_NAME).Url};
            this._asClient = new AmazonAutoScalingClient(this._stackModel.AccountViewModel.Credentials, asConfig);

            var ec2Config = new AmazonEC2Config {ServiceURL = endPoints.GetEndpoint(RegionEndPointsManager.EC2_SERVICE_NAME).Url};
            this._ec2Client = new AmazonEC2Client(this._stackModel.AccountViewModel.Credentials, ec2Config);


            var cwConfig = new AmazonCloudWatchConfig {ServiceURL = endPoints.GetEndpoint(RegionEndPointsManager.CLOUDWATCH_SERVICE_NAME).Url};
            this._cwClient = new AmazonCloudWatchClient(this._stackModel.AccountViewModel.Credentials, cwConfig);

            var rdsConfig = new AmazonRDSConfig {ServiceURL = endPoints.GetEndpoint(RegionEndPointsManager.RDS_SERVICE_NAME).Url};
            this._rdsClient = new AmazonRDSClient(this._stackModel.AccountViewModel.Credentials, rdsConfig);

            this._model = new ViewStackModel(region, this._stackModel.StackName);
            var control = new ViewStackControl(this);

            ToolkitFactory.Instance.ShellProvider.OpenInEditor(control);

            return new ActionResults().WithSuccess(true);
        }

        public string StackName
        {
            get { return this._model.StackName; }
        }

        public ViewStackModel Model
        {
            get { return this._model; }
        }

        public CloudFormationStackViewModel StackModel
        {
            get { return this._stackModel; }
        }

        public IAmazonCloudWatch CloudWatchClient
        {
            get { return this._cwClient; }
        }

        public void LoadModel()
        {
            this.RefreshAll();
        }

        public void ConnectToInstance(RunningInstanceWrapper instance)
        {
            if (this._ec2InstanceViewModel == null)
            {
                var ec2Model = this._stackModel.AccountViewModel.FindSingleChild<IEC2RootViewModel>(false);
                if (ec2Model != null)
                {
                    this._ec2InstanceViewModel = ec2Model.FindSingleChild<IEC2InstancesViewModel>(false);
                }
            }

            if (this._ec2InstanceViewModel == null)
                return;

            this._ec2InstanceViewModel.ConnectToInstance(instance.InstanceId);
        }

        public void ConnectToInstance()
        {
            if (this._ec2InstanceViewModel == null)
                return;

            var instanceIds = new List<string>();
            foreach (var instance in this.Model.Instances)
            {
                instanceIds.Add(instance.InstanceId);
            }

            this._ec2InstanceViewModel.ConnectToInstance(instanceIds);
        }

        public void DeleteStack()
        {
            var request = new DeleteStackRequest(){StackName = this._model.StackName};
            this._cloudFormationClient.DeleteStack(request);
        }

        public void CancelUpdate()
        {
            var request = new CancelUpdateStackRequest() { StackName = this._model.StackName };
            this._cloudFormationClient.CancelUpdateStack(request);
        }

        public void RefreshAll()
        {
            RefreshStackProperties();
            RefreshEvents();
            RefreshStackResources();
        }

        public bool RefreshStackProperties()
        {
            var requestStack = new DescribeStacksRequest() { StackName = this._model.StackName };
            var responseStack = this._cloudFormationClient.DescribeStacks(requestStack);

            if (responseStack.Stacks.Count != 1)
                return false;

            var stack = responseStack.Stacks[0];

            var requestTemplate = new GetTemplateRequest() { StackName = this._model.StackName };
            var responseTemplate = this._cloudFormationClient.GetTemplate(requestTemplate);
            var wrapper = CloudFormationTemplateWrapper.FromString(responseTemplate.TemplateBody);
            wrapper.LoadAndParse();

            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((Action)(() => 
            {
                
                this._model.StackId = stack.StackId;
                this._model.Status = stack.StackStatus;
                this._model.StatusReason = stack.StackStatusReason;
                this._model.Created = stack.CreationTime;
                this._model.SNSTopic = string.Join(", ", stack.NotificationARNs.ToArray());
                this._model.Tags = stack.Tags;
                if (stack.TimeoutInMinutes > 0)
                    this._model.CreateTimeout = stack.TimeoutInMinutes.ToString();
                else
                    this._model.CreateTimeout = "None";

                this._model.RollbackOnFailure = !stack.DisableRollback;
                this._model.Description = stack.Description;

                foreach (var parameter in stack.Parameters)
                {
                    if (!wrapper.Parameters.ContainsKey(parameter.ParameterKey))
                        continue;

                    var wrapperParameter = wrapper.Parameters[parameter.ParameterKey];
                    wrapperParameter.OverrideValue = parameter.ParameterValue;
                }

                this._model.Outputs.Clear();
                foreach (var output in stack.Outputs.OrderBy(x => x.OutputKey))
                    this._model.Outputs.Add(output);

                this._model.TemplateWrapper = wrapper;
            }));

            return true;
        }

        public void Poll()
        {
            var requestStack = new DescribeStacksRequest() { StackName = this._model.StackName };
            var responseStack = this._cloudFormationClient.DescribeStacks(requestStack);

            if (responseStack.Stacks.Count != 1)
                return;

            var stack = responseStack.Stacks[0];

            bool stateChange = !string.Equals(this._model.Status, stack.StackStatus);

            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((Action)(() =>
            {
                this._model.Status = stack.StackStatus;
                this._model.StatusReason = stack.StackStatusReason;

                this._model.Outputs.Clear();
                foreach (var output in stack.Outputs.OrderBy(x => x.OutputKey))
                    this._model.Outputs.Add(output);
            }));

            RefreshEvents();

            if (stateChange)
                RefreshStackResources();
        }


        public void RefreshEvents()
        {
            string mostRecentId = "";
            if (this._model.Events.Count > 0)
                mostRecentId = this._model.Events[0].NativeStackEvent.EventId;

            bool noNewEvents = false;
            List<StackEvent> events = new List<StackEvent>();
            DescribeStackEventsResponse response = null;            
            do
            {
                var request = new DescribeStackEventsRequest() { StackName = this._model.StackName };
                if (response != null)
                    request.NextToken = response.NextToken;

                response = this._cloudFormationClient.DescribeStackEvents(request);
                foreach (var evnt in response.StackEvents)
                {
                    if (string.Equals(evnt.EventId, mostRecentId))
                    {
                        noNewEvents = true;
                        break;
                    }

                    events.Add(evnt);
                }

            }while(!noNewEvents && !string.IsNullOrEmpty(response.NextToken));

            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((Action)(() => 
            {
                foreach (var evnt in events.OrderBy(x => x.Timestamp))
                {
                    var wrapper = new StackEventWrapper(evnt);
                    this._model.UnfilteredEvents.Insert(0, wrapper);

                    if (wrapper.PassClientFilter(this._model.EventTextFilter))
                        this._model.Events.Insert(0, wrapper);
                }
            }));
        }

        public void ReapplyFilter()
        {
            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke((Action)(() =>
            {
                lock (UPDATE_EVENT_LOCK_OBJECT)
                {
                    this._model.Events.Clear();

                    foreach (var evnt in this._model.UnfilteredEvents)
                    {
                        if (evnt.PassClientFilter(this._model.EventTextFilter))
                            this._model.Events.Add(evnt);
                    }
                }
            }));
        }

        public void RefreshStackResources()
        {
            List<StackResource> stackResources = GetStackResources();
            if (stackResources == null)
                return;

            Dictionary<string, object> fetchedDescribes = new Dictionary<string, object>();
            var instanceIds = getListOfInstanceIds(stackResources, fetchedDescribes);
            loadInstances(instanceIds);

            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((Action)(() =>
            {
                this.Model.Resources.Clear();
                foreach (var resource in stackResources)
                {
                    this.Model.Resources.Add(new ResourceWrapper(resource));
                }
            }));
            
        }

        internal List<StackResource> GetStackResources()
        {
            try
            {
                return AmazonCloudFormationClientExt.GetStackResources(this._cloudFormationClient, this.Model.StackName);
            }
            catch (AmazonCloudFormationException e)
            {
                LOGGER.ErrorFormat("Exception requested stack resources: {0}", e.Message);
            }
            return null;
        }

        void loadInstances(HashSet<string> instanceIds)
        {
            if (instanceIds == null || instanceIds.Count == 0)
                return;

            var describeInstancesRequest = new DescribeInstancesRequest() { InstanceIds = instanceIds.ToList() };
            var describeInstanceResponse = this._ec2Client.DescribeInstances(describeInstancesRequest);

            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke((Action)(() =>
            {
                this.Model.Instances.Clear();

                foreach (var reserveration in describeInstanceResponse.Reservations)
                {
                    foreach (var instance in reserveration.Instances)
                    {
                        var wrapper = new RunningInstanceWrapper(reserveration, instance);
                        this.Model.Instances.Add(wrapper);
                    }
                }
            }));
        }

        HashSet<string> getListOfInstanceIds(List<StackResource> stackResources, Dictionary<string, object> fetchedDescribes)
        {
            return AmazonCloudFormationClientExt.GetListOfInstanceIdsForStack(this._asClient, this._elbClient, stackResources, fetchedDescribes);
        }

        public void ViewDeploymentLog(RunningInstanceWrapper instance)
        {
            var controller = new ViewDeploymentLogController();
            controller.Execute(instance);
        }

        public AutoScalingGroupWrapper GetAutoScalingGroupDetails(string name)
        {
            var request = new DescribeAutoScalingGroupsRequest { AutoScalingGroupNames = new List<string> { name } };
            var response = this._asClient.DescribeAutoScalingGroups(request);
            if (response.AutoScalingGroups.Count != 1)
                return null;

            AutoScalingGroup group = response.AutoScalingGroups[0];
            return new AutoScalingGroupWrapper(group);
        }

        public LoadBalancerDescriptionWrapper GetLoadBalancerDetails(string name)
        {
            var request = new Amazon.ElasticLoadBalancing.Model.DescribeLoadBalancersRequest() { LoadBalancerNames = new List<string>() { name } };
            var response = this._elbClient.DescribeLoadBalancers(request);
            if (response.LoadBalancerDescriptions.Count != 1)
                return null;

            LoadBalancerDescription load = response.LoadBalancerDescriptions[0];
            return new LoadBalancerDescriptionWrapper(load);
        }

        public DBInstanceWrapper GetRDSInstanceDetails(string dbidentifier)
        {
            var request = new DescribeDBInstancesRequest() { DBInstanceIdentifier = dbidentifier };
            var response = this._rdsClient.DescribeDBInstances(request);
            if (response.DBInstances.Count != 1)
                return null;

            DBInstance dbInstance = response.DBInstances[0];
            return new DBInstanceWrapper(dbInstance);
        }

        public RunningInstanceWrapper GetInstanceDetails(string instanceId)
        {
            RunningInstanceWrapper instance = this.Model.Instances.FirstOrDefault(x => x.InstanceId == instanceId);
            return instance;
        }
    }
}
