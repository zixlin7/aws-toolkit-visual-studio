using System;
using System.Collections.Generic;
using System.Linq;

using Amazon.AutoScaling;
using Amazon.AutoScaling.Model;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CloudFormation.Model;
using Amazon.AWSToolkit.CloudFormation.Nodes;
using Amazon.AWSToolkit.CloudFormation.View;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.EC2;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions.Manifest;
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

namespace Amazon.AWSToolkit.CloudFormation.Controllers
{
    public class ViewStackController : BaseConnectionContextCommand
    {
        private static ILog Logger = LogManager.GetLogger(typeof(ViewStackController));

        private readonly object UPDATE_EVENT_LOCK_OBJECT = new object();

        private ViewStackModel _model;

        private IAmazonAutoScaling _asClient;
        private IAmazonCloudFormation _cloudFormationClient;
        private IAmazonEC2 _ec2Client;
        private IAmazonElasticLoadBalancing _elbClient;
        private IAmazonCloudWatch _cwClient;
        private IAmazonRDS _rdsClient;

        public ViewStackController(ViewStackModel model, ToolkitContext toolkitContext, AwsConnectionSettings connectionSettings)
            : base(toolkitContext, connectionSettings)
        {
            _model = model;

            _cloudFormationClient = _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonCloudFormationClient>(ConnectionSettings.CredentialIdentifier, ConnectionSettings.Region);
            _elbClient = _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonElasticLoadBalancingClient>(ConnectionSettings.CredentialIdentifier, ConnectionSettings.Region);
            _asClient = _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonAutoScalingClient>(ConnectionSettings.CredentialIdentifier, ConnectionSettings.Region);
            _ec2Client = _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonEC2Client>(ConnectionSettings.CredentialIdentifier, ConnectionSettings.Region);
            _cwClient = _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonCloudWatchClient>(ConnectionSettings.CredentialIdentifier, ConnectionSettings.Region);
            _rdsClient = _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonRDSClient>(ConnectionSettings.CredentialIdentifier, ConnectionSettings.Region);
        }

        public override ActionResults Execute()
        {
            _toolkitContext.ToolkitHost.OpenInEditor(new ViewStackControl(this));

            return new ActionResults().WithSuccess(true);
        }

        public string StackName => _model.StackName;

        public ViewStackModel Model => _model;

        public IAmazonCloudWatch CloudWatchClient => _cwClient;

        public void LoadModel()
        {
            RefreshAll();
        }

        public void ConnectToInstance(RunningInstanceWrapper instance)
        {
            IAWSEC2 awsEc2 = _toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(IAWSEC2)) as IAWSEC2;
            awsEc2.ConnectToInstance(ConnectionSettings, instance.InstanceId);
        }

        public void ConnectToInstance()
        {
            var instanceIds = new List<string>();
            foreach (var instance in Model.Instances)
            {
                instanceIds.Add(instance.InstanceId);
            }

            IAWSEC2 awsEc2 = _toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(IAWSEC2)) as IAWSEC2;
            awsEc2.ConnectToInstance(ConnectionSettings, instanceIds);
        }

        public void DeleteStack()
        {
            var request = new DeleteStackRequest(){StackName = _model.StackName};
            _cloudFormationClient.DeleteStack(request);
        }

        public void CancelUpdate()
        {
            var request = new CancelUpdateStackRequest() { StackName = _model.StackName };
            _cloudFormationClient.CancelUpdateStack(request);
        }

        public void RefreshAll()
        {
            RefreshStackProperties();
            RefreshEvents();
            RefreshStackResources();
        }

        public bool RefreshStackProperties()
        {
            var requestStack = new DescribeStacksRequest() { StackName = _model.StackName };
            var responseStack = _cloudFormationClient.DescribeStacks(requestStack);

            if (responseStack.Stacks.Count != 1)
            {
                return false;
            }

            var stack = responseStack.Stacks[0];

            var requestTemplate = new GetTemplateRequest() { StackName = _model.StackName };
            var responseTemplate = this._cloudFormationClient.GetTemplate(requestTemplate);
            var wrapper = CloudFormationTemplateWrapper.FromString(responseTemplate.TemplateBody);
            wrapper.LoadAndParse();

            _toolkitContext.ToolkitHost.BeginExecuteOnUIThread(() => 
            {
                
                _model.StackId = stack.StackId;
                _model.Status = stack.StackStatus;
                _model.StatusReason = stack.StackStatusReason;
                _model.Created = stack.CreationTime;
                _model.SNSTopic = string.Join(", ", stack.NotificationARNs.ToArray());
                _model.Tags = stack.Tags;
                if (stack.TimeoutInMinutes > 0)
                {
                    _model.CreateTimeout = stack.TimeoutInMinutes.ToString();
                }
                else
                {
                    _model.CreateTimeout = "None";
                }

                _model.RollbackOnFailure = !stack.DisableRollback;
                _model.Description = stack.Description;

                foreach (var parameter in stack.Parameters)
                {
                    if (!wrapper.Parameters.ContainsKey(parameter.ParameterKey))
                    {
                        continue;
                    }

                    var wrapperParameter = wrapper.Parameters[parameter.ParameterKey];
                    wrapperParameter.OverrideValue = parameter.ParameterValue;
                }

                _model.Outputs.Clear();
                foreach (var output in stack.Outputs.OrderBy(x => x.OutputKey))
                {
                    _model.Outputs.Add(output);
                }

                _model.TemplateWrapper = wrapper;
            });

            return true;
        }

        public void Poll()
        {
            var requestStack = new DescribeStacksRequest() { StackName = _model.StackName };
            var responseStack = _cloudFormationClient.DescribeStacks(requestStack);

            if (responseStack.Stacks.Count != 1)
            {
                return;
            }

            var stack = responseStack.Stacks[0];

            bool stateChange = !string.Equals(_model.Status, stack.StackStatus);

            _toolkitContext.ToolkitHost.BeginExecuteOnUIThread(() =>
            {
                _model.Status = stack.StackStatus;
                _model.StatusReason = stack.StackStatusReason;

                _model.Outputs.Clear();
                foreach (var output in stack.Outputs.OrderBy(x => x.OutputKey))
                    _model.Outputs.Add(output);
            });

            RefreshEvents();

            if (stateChange)
            {
                RefreshStackResources();
            }
        }

        public void RefreshEvents()
        {
            string mostRecentId = "";
            if (_model.Events.Count > 0)
            {
                mostRecentId = _model.Events[0].NativeStackEvent.EventId;
            }

            bool noNewEvents = false;
            List<StackEvent> events = new List<StackEvent>();
            DescribeStackEventsResponse response = null;            
            do
            {
                var request = new DescribeStackEventsRequest() { StackName = _model.StackName };
                if (response != null)
                {
                    request.NextToken = response.NextToken;
                }

                response = _cloudFormationClient.DescribeStackEvents(request);
                foreach (var evnt in response.StackEvents)
                {
                    if (string.Equals(evnt.EventId, mostRecentId))
                    {
                        noNewEvents = true;
                        break;
                    }

                    events.Add(evnt);
                }

            } while(!noNewEvents && !string.IsNullOrEmpty(response.NextToken));

            _toolkitContext.ToolkitHost.BeginExecuteOnUIThread(() => 
            {
                foreach (var evnt in events.OrderBy(x => x.Timestamp))
                {
                    var wrapper = new StackEventWrapper(evnt);
                    _model.UnfilteredEvents.Insert(0, wrapper);

                    if (wrapper.PassClientFilter(this._model.EventTextFilter))
                        _model.Events.Insert(0, wrapper);
                }
            });
        }

        public void ReapplyFilter()
        {
            _toolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
            {
                lock (UPDATE_EVENT_LOCK_OBJECT)
                {
                    _model.Events.Clear();

                    foreach (var evnt in _model.UnfilteredEvents)
                    {
                        if (evnt.PassClientFilter(_model.EventTextFilter))
                        {
                            _model.Events.Add(evnt);
                        }
                    }
                }
            });
        }

        public void RefreshStackResources()
        {
            List<StackResource> stackResources = GetStackResources();
            if (stackResources == null)
            {
                return;
            }

            Dictionary<string, object> fetchedDescribes = new Dictionary<string, object>();
            var instanceIds = getListOfInstanceIds(stackResources, fetchedDescribes);
            loadInstances(instanceIds);

            _toolkitContext.ToolkitHost.BeginExecuteOnUIThread(() =>
            {
                Model.Resources.Clear();
                foreach (var resource in stackResources)
                {
                    Model.Resources.Add(new ResourceWrapper(resource));
                }
            });
            
        }

        internal List<StackResource> GetStackResources()
        {
            try
            {
                return AmazonCloudFormationClientExt.GetStackResources(_cloudFormationClient, Model.StackName);
            }
            catch (AmazonCloudFormationException e)
            {
                Logger.ErrorFormat("Exception requested stack resources: {0}", e.Message);
            }
            return null;
        }

        private void loadInstances(HashSet<string> instanceIds)
        {
            if (instanceIds == null || instanceIds.Count == 0)
            {
                return;
            }

            var describeInstancesRequest = new DescribeInstancesRequest() { InstanceIds = instanceIds.ToList() };
            var describeInstanceResponse = _ec2Client.DescribeInstances(describeInstancesRequest);

            _toolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
            {
                Model.Instances.Clear();

                foreach (var reserveration in describeInstanceResponse.Reservations)
                {
                    foreach (var instance in reserveration.Instances)
                    {
                        var wrapper = new RunningInstanceWrapper(reserveration, instance);
                        Model.Instances.Add(wrapper);
                    }
                }
            });
        }

        private HashSet<string> getListOfInstanceIds(List<StackResource> stackResources, Dictionary<string, object> fetchedDescribes)
        {
            return AmazonCloudFormationClientExt.GetListOfInstanceIdsForStack(_asClient, _elbClient, stackResources, fetchedDescribes);
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
            {
                return null;
            }

            AutoScalingGroup group = response.AutoScalingGroups[0];
            return new AutoScalingGroupWrapper(group);
        }

        public LoadBalancerDescriptionWrapper GetLoadBalancerDetails(string name)
        {
            var request = new Amazon.ElasticLoadBalancing.Model.DescribeLoadBalancersRequest() { LoadBalancerNames = new List<string>() { name } };
            var response = _elbClient.DescribeLoadBalancers(request);
            if (response.LoadBalancerDescriptions.Count != 1)
            {
                return null;
            }

            LoadBalancerDescription load = response.LoadBalancerDescriptions[0];
            return new LoadBalancerDescriptionWrapper(load);
        }

        public DBInstanceWrapper GetRDSInstanceDetails(string dbidentifier)
        {
            var request = new DescribeDBInstancesRequest() { DBInstanceIdentifier = dbidentifier };
            var response = _rdsClient.DescribeDBInstances(request);
            if (response.DBInstances.Count != 1)
            {
                return null;
            }

            DBInstance dbInstance = response.DBInstances[0];
            return new DBInstanceWrapper(dbInstance);
        }

        public RunningInstanceWrapper GetInstanceDetails(string instanceId)
        {
            RunningInstanceWrapper instance = Model.Instances.FirstOrDefault(x => x.InstanceId == instanceId);
            return instance;
        }
    }
}
