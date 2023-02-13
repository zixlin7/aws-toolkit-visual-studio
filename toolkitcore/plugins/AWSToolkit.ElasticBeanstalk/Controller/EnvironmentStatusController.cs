using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon.AutoScaling;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.EC2;
using Amazon.AWSToolkit.ElasticBeanstalk.Model;
using Amazon.AWSToolkit.ElasticBeanstalk.Models;
using Amazon.AWSToolkit.ElasticBeanstalk.Utils;
using Amazon.AWSToolkit.ElasticBeanstalk.View;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.SimpleWorkers;
using Amazon.AWSToolkit.Tasks;
using Amazon.AWSToolkit.ViewModels.Charts;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.EC2;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;
using Amazon.ElasticLoadBalancing;

using log4net;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Controller
{
    public class EnvironmentStatusController : BaseConnectionContextCommand
    {
        private readonly object UPDATE_EVENT_LOCK_OBJECT = new object();

        private static ILog Logger = LogManager.GetLogger(typeof(EnvironmentStatusController));

        private readonly IAmazonAutoScaling _asClient;
        private readonly IAmazonElasticLoadBalancing _elbClient;
        private readonly IAmazonEC2 _ec2Client;
        private readonly IAmazonCloudWatch _cwClient;
        private readonly CloudWatchMetrics _cloudWatchMetrics;
        private readonly IAmazonElasticBeanstalk _beanstalkClient;
        private readonly BeanstalkEnvironmentModel _beanstalkEnvironment;

        private EnvironmentStatusModel _statusModel;
        private EnvironmentStatusControl _control;

        public EnvironmentStatusController(BeanstalkEnvironmentModel beanstalkEnvironment,
            ToolkitContext toolkitContext, AwsConnectionSettings connectionSettings)
            : base(toolkitContext, connectionSettings)
        {
            _beanstalkEnvironment = beanstalkEnvironment;

            _beanstalkClient = _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonElasticBeanstalkClient>(ConnectionSettings.CredentialIdentifier, ConnectionSettings.Region);
            _cwClient = _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonCloudWatchClient>(ConnectionSettings.CredentialIdentifier, ConnectionSettings.Region);
            _cloudWatchMetrics = new CloudWatchMetrics(_cwClient);
            _asClient = _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonAutoScalingClient>(ConnectionSettings.CredentialIdentifier, ConnectionSettings.Region);
            _elbClient = _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonElasticLoadBalancingClient>(ConnectionSettings.CredentialIdentifier, ConnectionSettings.Region);
            _ec2Client = _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonEC2Client>(ConnectionSettings.CredentialIdentifier, ConnectionSettings.Region);
        }

        public override ActionResults Execute()
        {
            if (_beanstalkEnvironment == null)
            {
                return new ActionResults().WithSuccess(false);
            }

            _statusModel = new EnvironmentStatusModel(_beanstalkEnvironment.Id, _beanstalkEnvironment.Name);
            _control = new EnvironmentStatusControl(this) {DisplayNotificationOnReady = true};

            _toolkitContext.ToolkitHost.OpenInEditor(_control);

            return new ActionResults().WithSuccess(true);
        }

        public EnvironmentStatusModel Model => _statusModel;

        public void LoadModel()
        {
            refreshEnvironmentProperties();
            refreshEvents();
            refreshConfigSettings(false);
            refreshOptions();
            setTabsForEnvironmentType(false);
            this.Model.ConfigModel.IsConfigDirty = false;
        }

        public void Refresh()
        {
            Refresh(false);
        }

        public void Refresh(bool polling)
        {
            try
            {
                refreshEnvironmentProperties();
                refreshEvents();

                if (!polling)
                {
                    refreshConfigSettings(false);
                }

            }
            catch (Exception e)
            {
                if (!polling)
                {
                    _toolkitContext.ToolkitHost.ShowError("Error Refreshing",
                        string.Format("Error refreshing environment {0}: {1}", this.Model.EnvironmentName, e.Message));
                }
            }
        }

        public ActionResults RestartApp()
        {
            var command = new RestartAppController(_beanstalkEnvironment,
                _toolkitContext, ConnectionSettings);
            return command.Execute();
        }

        public ActionResults RebuildEnvironment()
        {
            var command = new RebuildEnvironmentController(_beanstalkEnvironment,
                _toolkitContext, ConnectionSettings);
            return command.Execute();
        }

        public ActionResults TerminateEnvironment()
        {
            var command = new TerminateEnvironmentController(_beanstalkEnvironment,
                _toolkitContext, ConnectionSettings);
            return command.Execute();
        }

        public void ConnectToInstance()
        {
            var instanceIds = getListOfInstances();
            IAWSEC2 awsEc2 = _toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(IAWSEC2)) as IAWSEC2;
            awsEc2?.ConnectToInstance(ConnectionSettings, instanceIds);
        }

        public void ChangeEnvironmentType(string requestedType)
        {
            var command = new ChangeEnvironmentTypeController(_beanstalkEnvironment,
                _toolkitContext, ConnectionSettings)
            {
                RequestedEnvironmentType = requestedType,
                VPCId = this._statusModel.ConfigModel.GetValue("aws:ec2:vpc", "VPCId")
            };

            if (command.Execute().Success)
            {
                // decided cleanest approach is to dump and reload parts of the model
                refreshEnvironmentProperties();
                refreshOptions();
                refreshConfigSettings(true);
                setTabsForEnvironmentType(true);
                this.Model.ConfigModel.IsConfigDirty = false;
            }
        }

        IList<string> getListOfInstances()
        {
            var response = _beanstalkClient.DescribeEnvironmentResources(
                new DescribeEnvironmentResourcesRequest(){EnvironmentId = _beanstalkEnvironment.Id});


            IList<string> instances = new List<string>();
            foreach (var instance in response.EnvironmentResources.Instances)
            {
                instances.Add(instance.Id);
            }

            return instances;
        }


        public ActionResults ApplyConfigSettings()
        {
            var settings = Model.ConfigModel.GetSettings();
            try
            {
                var response = _beanstalkClient.ValidateConfigurationSettings(new ValidateConfigurationSettingsRequest()
                {
                    ApplicationName = Model.ApplicationName,
                    EnvironmentName = Model.EnvironmentName,
                    OptionSettings = settings
                });

                messageCount(response.Messages, out var warnings, out var errors);
                if (errors > 0)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Can not apply new settings because of the following error(s).\r\n");
                    foreach (var message in response.Messages)
                    {
                        sb.AppendLine("* " + message.Message);
                    }

                    throw new ToolkitException(sb.ToString(), ToolkitException.CommonErrorCode.UnexpectedError);
                }

                var xraySetting = settings.FirstOrDefault(x => string.Equals(x.OptionName, "XRayEnabled", StringComparison.Ordinal));

                _beanstalkClient.UpdateEnvironment(new UpdateEnvironmentRequest()
                {
                    EnvironmentId = Model.EnvironmentId,
                    OptionSettings = settings
                });

                Model.ConfigModel.IsConfigDirty = false;
                return new ActionResults().WithSuccess(true);
            }
            catch (Exception e)
            {
                _toolkitContext.ToolkitHost.ShowError("Error Applying Changes",
                    $"Error applying changes to the environment {Model.EnvironmentName}:{Environment.NewLine}{e.Message}");
                Logger.Error("Error applying changes for environment", e);
                return ActionResults.CreateFailed(e);
            }
        }

        static internal void messageCount(List<ValidationMessage> messages, out int warnings, out int errors)
        {
            warnings = 0;
            errors = 0;
            foreach(var message in messages)
            {
                if (message.Severity == BeanstalkConstants.VALIDATION_WARNING)
                    warnings++;
                if (message.Severity == BeanstalkConstants.VALIDATION_ERROR)
                    errors++;
            }
        }

        void refreshEnvironmentProperties()
        {
            var request = new DescribeEnvironmentsRequest() { EnvironmentIds = new List<string>() { this._statusModel.EnvironmentId } };

            var response = this._beanstalkClient.DescribeEnvironments(request);
            if (response.Environments.Count == 0)
                return;

            var environment = response.Environments[0];

            _toolkitContext.ToolkitHost.BeginExecuteOnUIThread((Action)(() =>
                {
                    this._statusModel.EnvironmentName = environment.EnvironmentName;
                    this._statusModel.Description = environment.Description;
                    this._statusModel.DateCreated = environment.DateCreated;
                    this._statusModel.DateUpdated = environment.DateUpdated;
                    this._statusModel.ApplicationName = environment.ApplicationName;
                    this._statusModel.VersionLabel = environment.VersionLabel;

                    if (!string.IsNullOrEmpty(environment.SolutionStackName))
                        this._statusModel.ContainerType = environment.SolutionStackName;
                    else
                        this._statusModel.ContainerType = environment.TemplateName;

                    this._statusModel.EndPointURL = string.Format("http://{0}/", environment.CNAME);
                    this._statusModel.Status = environment.Status;
                    this._statusModel.Health = environment.Health;
                    this._statusModel.CNAME = environment.CNAME;
                }));
        }

        void refreshEvents()
        {
            var request = new DescribeEventsRequest() { EnvironmentId = this._statusModel.EnvironmentId };

            if (this._statusModel.LastEventTimestamp != DateTime.MinValue)
                request.StartTimeUtc = this._statusModel.LastEventTimestamp.ToUniversalTime().AddMilliseconds(10);

            var response = this._beanstalkClient.DescribeEvents(request);

            _toolkitContext.ToolkitHost.BeginExecuteOnUIThread((Action)(() =>
            {
                lock (UPDATE_EVENT_LOCK_OBJECT)
                {
                    foreach (var evnt in response.Events.OrderBy(x => x.EventDate))
                    {
                        if (evnt.EventDate > this._statusModel.LastEventTimestamp)
                            this._statusModel.LastEventTimestamp = evnt.EventDate;

                        var wrapper = new EventWrapper(evnt);
                        this._statusModel.UnfilteredEvents.Insert(0, wrapper);

                        if (wrapper.PassClientFilter(this._statusModel.TextFilter, false))
                            this._statusModel.Events.Insert(0, wrapper);
                    }
                }
            }));
        }

        public void RefreshResources()
        {
            try
            {
                EnvironmentResourceDescription resources = GetEnvironmentResourceDescription();

                List<Amazon.EC2.Model.Instance> ris = new List<Amazon.EC2.Model.Instance>();
                List<string> iNames = new List<string>();   
                foreach (Instance instance in resources.Instances)
                {
                    iNames.Add(instance.Id);
                }
                if (iNames.Count != 0)
                {
                    Amazon.EC2.Model.DescribeInstancesResponse iResponse = this._ec2Client.DescribeInstances(
                        new Amazon.EC2.Model.DescribeInstancesRequest() { InstanceIds = iNames });
                    foreach (Amazon.EC2.Model.Reservation reservation in iResponse.Reservations)
                    {
                        ris.AddRange(reservation.Instances);
                    }
                }

                List<Amazon.ElasticLoadBalancing.Model.LoadBalancerDescription> elbs = new List<Amazon.ElasticLoadBalancing.Model.LoadBalancerDescription>();
                List<string> lbNames = new List<string>();
                foreach (LoadBalancer lb in resources.LoadBalancers)
                {
                    lbNames.Add(lb.Name);
                }
                if (lbNames.Count != 0)
                {
                    Amazon.ElasticLoadBalancing.Model.DescribeLoadBalancersResponse lbResponse = this._elbClient.DescribeLoadBalancers(
                        new Amazon.ElasticLoadBalancing.Model.DescribeLoadBalancersRequest() { LoadBalancerNames = lbNames });
                    elbs.AddRange(lbResponse.LoadBalancerDescriptions);
                }

                Dictionary<string, Amazon.AutoScaling.Model.LaunchConfiguration> lcs = new Dictionary<string, Amazon.AutoScaling.Model.LaunchConfiguration>();
                foreach (LaunchConfiguration lc in resources.LaunchConfigurations)
                {
                    lcs.Add(lc.Name, null);
                }
                if (lcs.Count != 0)
                {
                    Amazon.AutoScaling.Model.DescribeLaunchConfigurationsResponse lcResponse = this._asClient.DescribeLaunchConfigurations(
                        new Amazon.AutoScaling.Model.DescribeLaunchConfigurationsRequest
                        {
                            LaunchConfigurationNames = lcs.Keys.ToList<string>()
                        });

                    foreach (var lc in lcResponse.LaunchConfigurations)
                        lcs[lc.LaunchConfigurationName] = lc;

                    while (!String.IsNullOrEmpty(lcResponse.NextToken))
                    {
                        lcResponse = this._asClient.DescribeLaunchConfigurations(
                            new Amazon.AutoScaling.Model.DescribeLaunchConfigurationsRequest
                            {
                                LaunchConfigurationNames = lcs.Keys.ToList<string>(),
                                NextToken = lcResponse.NextToken
                            });
                        foreach (var lc in lcResponse.LaunchConfigurations)
                            lcs[lc.LaunchConfigurationName] = lc;
                    }
                }

                List<Amazon.AutoScaling.Model.AutoScalingGroup> asgs = new List<Amazon.AutoScaling.Model.AutoScalingGroup>();
                List<string> asgNames = new List<string>();
                foreach (AutoScalingGroup asg in resources.AutoScalingGroups)
                {
                    asgNames.Add(asg.Name);
                }
                if (asgNames.Count != 0)
                {
                    Amazon.AutoScaling.Model.DescribeAutoScalingGroupsResponse asgResponse = this._asClient.DescribeAutoScalingGroups(
                        new Amazon.AutoScaling.Model.DescribeAutoScalingGroupsRequest
                        {
                            AutoScalingGroupNames = asgNames
                        });
                    asgs.AddRange(asgResponse.AutoScalingGroups);
                    while (!String.IsNullOrEmpty(asgResponse.NextToken))
                    {
                        asgResponse = this._asClient.DescribeAutoScalingGroups(
                            new Amazon.AutoScaling.Model.DescribeAutoScalingGroupsRequest
                            {
                                AutoScalingGroupNames = asgNames,
                                NextToken = asgResponse.NextToken
                            });
                        asgs.AddRange(asgResponse.AutoScalingGroups);
                    }
                }

                List<Amazon.CloudWatch.Model.MetricAlarm> mas = new List<Amazon.CloudWatch.Model.MetricAlarm>();
                List<string> alarmNames = new List<string>();
                foreach (Amazon.ElasticBeanstalk.Model.Trigger t in resources.Triggers)
                {
                    alarmNames.Add(String.Format("{0}{1}", t.Name, "-lower"));
                    alarmNames.Add(String.Format("{0}{1}", t.Name, "-upper"));
                }
                if (alarmNames.Count != 0)
                {
                    Amazon.CloudWatch.Model.DescribeAlarmsResponse alarmsResponse = this._cwClient.DescribeAlarms(
                        new Amazon.CloudWatch.Model.DescribeAlarmsRequest() { AlarmNames = alarmNames });
                    mas.AddRange(alarmsResponse.MetricAlarms);
                    while (!String.IsNullOrEmpty(alarmsResponse.NextToken))
                    {
                        alarmsResponse = this._cwClient.DescribeAlarms(
                            new Amazon.CloudWatch.Model.DescribeAlarmsRequest()
                            {
                                AlarmNames = alarmNames,
                                NextToken = alarmsResponse.NextToken
                            });
                        mas.AddRange(alarmsResponse.MetricAlarms);
                    }
                }

                _toolkitContext.ToolkitHost.BeginExecuteOnUIThread((Action)(() =>
                {
                    try
                    {
                        this._statusModel.ResourcesUpdated = DateTime.Now;

                        this._statusModel.Instances.Clear();
                        foreach (Amazon.EC2.Model.Instance ri in ris)
                        {
                            this._statusModel.Instances.Insert(0, new InstanceWrapper(ri));
                        }

                        this._statusModel.LoadBalancers.Clear();
                        foreach (Amazon.ElasticLoadBalancing.Model.LoadBalancerDescription lb in elbs)
                        {
                            this._statusModel.LoadBalancers.Insert(0, new LoadBalancerWrapper(lb));
                        }

                        this._statusModel.AutoScalingGroups.Clear();
                        foreach (Amazon.AutoScaling.Model.AutoScalingGroup asg in asgs)
                        {
                            Amazon.AutoScaling.Model.LaunchConfiguration lc;
                            if (lcs.TryGetValue(asg.LaunchConfigurationName, out lc))
                            {
                                this._statusModel.AutoScalingGroups.Insert(0, new AutoScalingGroupWrapper(asg, lc));
                            }
                        }

                        this._statusModel.Triggers.Clear();
                        foreach (Amazon.CloudWatch.Model.MetricAlarm ma in mas)
                        {
                            this._statusModel.Triggers.Insert(0, new MetricAlarmWrapper(ma));
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Error refreshing UI for resources", e);
                    }
                }));
            }
            catch (Exception e)
            {
                Logger.Error("Error refreshing resources", e);
            }
        }

        void refreshOptions()
        {
            if (this._statusModel.Status == BeanstalkConstants.STATUS_TERMINATED)
                return;

            DescribeConfigurationOptionsResponse optionsResponse =
                this._beanstalkClient.DescribeConfigurationOptions(new DescribeConfigurationOptionsRequest
                {
                    ApplicationName = _beanstalkEnvironment.ApplicationName,
                    EnvironmentName = _beanstalkEnvironment.Name,
                });
            List<ConfigurationOptionDescription> options = optionsResponse.Options;

            _toolkitContext.ToolkitHost.BeginExecuteOnUIThread((Action)(() =>
            {
                this._statusModel.ConfigModel.LoadConfigDescriptions(options);
            }));
        }

        void refreshConfigSettings(bool changingEnvironmentType)
        {
            if (this.Model.Status == BeanstalkConstants.STATUS_TERMINATED)
            {
                return;
            }

            var request = new DescribeConfigurationSettingsRequest()
            {
                ApplicationName = _beanstalkEnvironment.ApplicationName,
                EnvironmentName = _beanstalkEnvironment.Name
            };

            var response = this._beanstalkClient.DescribeConfigurationSettings(request);
            if (response.ConfigurationSettings.Count == 0)
                return;

            _toolkitContext.ToolkitHost.BeginExecuteOnUIThread((Action)(() =>
            {
                this._statusModel.ConfigModel.LoadConfigOptions(response.ConfigurationSettings[0].OptionSettings, changingEnvironmentType);
            }));
        }

        void setTabsForEnvironmentType(bool changingEnvironmentType)
        {
            _toolkitContext.ToolkitHost.BeginExecuteOnUIThread((Action)(() => this._control.CustomizeTabsForEnvironmentType(changingEnvironmentType)));
        }

        public void ReapplyFilter()
        {
            _toolkitContext.ToolkitHost.ExecuteOnUIThread((Action)(() =>
            {
                lock (UPDATE_EVENT_LOCK_OBJECT)
                {
                    this._statusModel.Events.Clear();

                    foreach (var evnt in this._statusModel.UnfilteredEvents)
                    {
                        if (evnt.PassClientFilter(this._statusModel.TextFilter, false))
                            this._statusModel.Events.Add(evnt);
                    }
                }
            }));
        }

        internal EnvironmentResourceDescription GetEnvironmentResourceDescription()
        {
            var response = this._beanstalkClient.DescribeEnvironmentResources(
                new DescribeEnvironmentResourcesRequest() { EnvironmentId = this._statusModel.EnvironmentId });

            return response.EnvironmentResources;
        }

        internal void LoadCloudWatchData(MonitorGraphViewModel viewModel, string metricNamespace, string metricName, CloudWatchMetrics.Aggregate statsAggregate, string units, List<Dimension> dimensions, int hoursToView)
        {
            Task.Run(async () => { await LoadCloudWatchDataAsync(viewModel, metricNamespace, metricName, statsAggregate, units, dimensions, hoursToView); }).LogExceptionAndForget();
        }

        private async Task LoadCloudWatchDataAsync(MonitorGraphViewModel viewModel, string metricNamespace, string metricName,
            CloudWatchMetrics.Aggregate statsAggregate, string units, List<Dimension> dimensions, int hoursToView)
        {
            try
            {
                _toolkitContext.ToolkitHost.ExecuteOnUIThread(() => viewModel.Loading = true);

                var values = await _cloudWatchMetrics.LoadMetricsAsync(
                    metricName, metricNamespace, dimensions, statsAggregate, units, hoursToView);

                _toolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
                {
                    viewModel.ApplyMetrics(values);
                });
            }
            catch (Exception e)
            {
                _toolkitContext.ToolkitHost.ExecuteOnUIThread(() => viewModel.ErrorMessage = e.Message);
                throw;
            }
            finally
            {
                _toolkitContext.ToolkitHost.ExecuteOnUIThread(() => viewModel.Loading = false);
            }
        }

        public void RequestEnvironmentLogs()
        {
            var request = new RequestEnvironmentInfoRequest()
            {
                EnvironmentName = _beanstalkEnvironment.Name,
                InfoType = "tail"
            };

            this._beanstalkClient.RequestEnvironmentInfo(request);
        }

        public RetrieveEnvironmentInfoResponse RetrieveEnvironmentLogs()
        {
            var request = new RetrieveEnvironmentInfoRequest()
            {
                EnvironmentName = _beanstalkEnvironment.Name,
                InfoType = "tail"
            };

            var response = this._beanstalkClient.RetrieveEnvironmentInfo(request);
            return response;
        }

        internal void RecordRebuildEnvironment(ActionResults result)
        {
            _toolkitContext.RecordBeanstalkRebuildEnvironment(result, ConnectionSettings);
        }

        internal void RecordDeleteEnvironment(ActionResults result)
        {
            _toolkitContext.RecordBeanstalkDeleteEnvironment(result, ConnectionSettings);
        }

        internal void RecordRestartApplication(ActionResults result)
        {
            _toolkitContext.RecordBeanstalkRestartApplication(result, ConnectionSettings);
        }

        internal void RecordEditEnvironment(ActionResults result)
        {
            _toolkitContext.RecordBeanstalkEditEnvironment(result, ConnectionSettings);
        }
    }
}
