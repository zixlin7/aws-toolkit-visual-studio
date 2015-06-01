using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

using System.Windows.Controls.DataVisualization.Charting;
using Amazon.AutoScaling;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.ElasticBeanstalk.Nodes;
using Amazon.AWSToolkit.ElasticBeanstalk.View;
using Amazon.AWSToolkit.ElasticBeanstalk.Model;
using Amazon.AWSToolkit.EC2.Nodes;
using Amazon.AWSToolkit;
using Amazon.AWSToolkit.SimpleWorkers;
using Amazon.EC2;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;

using Amazon;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.ElasticLoadBalancing;
using log4net;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Controller
{
    public class EnvironmentStatusController : BaseContextCommand
    {
        readonly object UPDATE_EVENT_LOCK_OBJECT = new object();

        static ILog LOGGER = LogManager.GetLogger(typeof(EnvironmentStatusController));

        Amazon.AutoScaling.IAmazonAutoScaling _asClient;
        Amazon.ElasticLoadBalancing.IAmazonElasticLoadBalancing _elbClient;
        Amazon.EC2.IAmazonEC2 _ec2Client;
        IAmazonCloudWatch _cwClient;
        IAmazonElasticBeanstalk _beanstalkClient;
        EnvironmentViewModel _environmentModel;

        EnvironmentStatusModel _statusModel;
        EnvironmentStatusControl _control;

        IEC2InstancesViewModel _ec2InstanceViewModel;

        public override ActionResults Execute(IViewModel model)
        {
            this._environmentModel = model as EnvironmentViewModel;
            if (this._environmentModel == null)
                return new ActionResults().WithSuccess(false);

            String region = this._environmentModel.ApplicationViewModel.ElasticBeanstalkRootViewModel.CurrentEndPoint.RegionSystemName;
            RegionEndPointsManager.RegionEndPoints endPoints = RegionEndPointsManager.Instance.GetRegion(region);
            
            this._beanstalkClient = this._environmentModel.BeanstalkClient;

            var cwConfig = new AmazonCloudWatchConfig {ServiceURL = endPoints.GetEndpoint(RegionEndPointsManager.CLOUDWATCH_SERVICE_NAME).Url};
            this._cwClient = new AmazonCloudWatchClient(this._environmentModel.AccountViewModel.AccessKey, this._environmentModel.AccountViewModel.SecretKey, cwConfig);

            var asConfig = new AmazonAutoScalingConfig {ServiceURL = endPoints.GetEndpoint(RegionEndPointsManager.AUTOSCALING_SERVICE_NAME).Url};
            this._asClient = new AmazonAutoScalingClient(this._environmentModel.AccountViewModel.AccessKey, this._environmentModel.AccountViewModel.SecretKey, asConfig);

            var elbConfig = new AmazonElasticLoadBalancingConfig {ServiceURL = endPoints.GetEndpoint(RegionEndPointsManager.ELB_SERVICE_NAME).Url};
            this._elbClient = new AmazonElasticLoadBalancingClient(this._environmentModel.AccountViewModel.AccessKey, this._environmentModel.AccountViewModel.SecretKey, elbConfig);

            var ec2Config = new AmazonEC2Config {ServiceURL = endPoints.GetEndpoint(RegionEndPointsManager.EC2_SERVICE_NAME).Url};
            this._ec2Client = new AmazonEC2Client(this._environmentModel.AccountViewModel.AccessKey, this._environmentModel.AccountViewModel.SecretKey, ec2Config);

            var ec2Model = this._environmentModel.AccountViewModel.FindSingleChild<IEC2RootViewModel>(false);
            if (ec2Model != null)
            {
                this._ec2InstanceViewModel = ec2Model.FindSingleChild<IEC2InstancesViewModel>(false);
            }

            this._statusModel = new EnvironmentStatusModel(
                this._environmentModel.Environment.EnvironmentId, this._environmentModel.Environment.EnvironmentName);

            _control = new EnvironmentStatusControl(this) {DisplayNotificationOnReady = true};

            ToolkitFactory.Instance.ShellProvider.OpenInEditor(_control);

            return new ActionResults()
                    .WithSuccess(true);
        }

        public EnvironmentStatusModel Model
        {
            get { return this._statusModel; }
        }

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
                    ToolkitFactory.Instance.ShellProvider.ShowError("Error Refreshing",
                        string.Format("Error refreshing environment {0}: {1}", this.Model.EnvironmentName, e.Message));
                }
            }
        }

        public void RestartApp()
        {
            var command = new RestartAppController();
            command.Execute(this._environmentModel);
        }

        public void RebuildEnvironment()
        {
            var command = new RebuildEnvironmentController();
            command.Execute(this._environmentModel);
        }

        public void TerminateEnvironment()
        {
            var command = new TerminateEnvironmentController();
            command.Execute(this._environmentModel);
        }

        public void ConnectToInstance()
        {
            var instanceIds = getListOfInstances();
            if (this._ec2InstanceViewModel == null)
                return;

            this._ec2InstanceViewModel.ConnectToInstance(instanceIds);
        }

        public void ChangeEnvironmentType(string requestedType)
        {
            var command = new ChangeEnvironmentTypeController 
            {
                RequestedEnvironmentType = requestedType,
                VPCId = this._statusModel.ConfigModel.GetValue("aws:ec2:vpc", "VPCId")
            };

            if ((command.Execute(this._environmentModel)).Success)
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
            var response = this._environmentModel.BeanstalkClient.DescribeEnvironmentResources(
                new DescribeEnvironmentResourcesRequest(){EnvironmentId = this._environmentModel.Environment.EnvironmentId});


            IList<string> instances = new List<string>();
            foreach (var instance in response.EnvironmentResources.Instances)
            {
                instances.Add(instance.Id);
            }

            return instances;
        }


        public void ApplyConfigSettings()
        {
            var settings = this.Model.ConfigModel.GetSettings();
            try
            {
                var response = this._beanstalkClient.ValidateConfigurationSettings(new ValidateConfigurationSettingsRequest()
                {
                    ApplicationName = this.Model.ApplicationName,
                    EnvironmentName = this.Model.EnvironmentName,
                    OptionSettings = settings
                });

                int warnings, errors;
                messageCount(response.Messages, out warnings, out errors);
                if (errors > 0)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Can not apply new settings because of the following error(s).\r\n");
                    foreach (var message in response.Messages)
                    {
                        sb.AppendLine("* " + message.Message);
                    }

                    ToolkitFactory.Instance.ShellProvider.ShowError("Error Applying Changes", sb.ToString());
                    return;
                }

                this._beanstalkClient.UpdateEnvironment(new UpdateEnvironmentRequest()
                {
                    EnvironmentId = this.Model.EnvironmentId,
                    OptionSettings = settings
                });

                this.Model.ConfigModel.IsConfigDirty = false;
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error Applying Changes", 
                    string.Format("Error applying changes to the environment {0}: {1}" , this.Model.EnvironmentName, e.Message));
                LOGGER.Error("Error applying changes for environment.", e);
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

            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((Action)(() =>
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
                request.StartTime = this._statusModel.LastEventTimestamp.ToUniversalTime().AddMilliseconds(10);

            var response = this._beanstalkClient.DescribeEvents(request);

            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((Action)(() =>
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

                ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((Action)(() =>
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
                        LOGGER.Error("Error refreshing UI for resources", e);
                    }
                }));
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing resources", e);
            }
        }

        void refreshOptions()
        {
            if (this._statusModel.Status == BeanstalkConstants.STATUS_TERMINATED)
                return;

            DescribeConfigurationOptionsResponse optionsResponse =
                this._beanstalkClient.DescribeConfigurationOptions(new DescribeConfigurationOptionsRequest
                {
                    ApplicationName = _environmentModel.Environment.ApplicationName,
                    EnvironmentName = _environmentModel.Environment.EnvironmentName,
                });
            List<ConfigurationOptionDescription> options = optionsResponse.Options;

            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((Action)(() =>
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
                ApplicationName = this._environmentModel.Environment.ApplicationName,
                EnvironmentName = this._environmentModel.Environment.EnvironmentName
            };

            var response = this._beanstalkClient.DescribeConfigurationSettings(request);
            if (response.ConfigurationSettings.Count == 0)
                return;

            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((Action)(() =>
            {
                this._statusModel.ConfigModel.LoadConfigOptions(response.ConfigurationSettings[0].OptionSettings, changingEnvironmentType);
            }));
        }

        void setTabsForEnvironmentType(bool changingEnvironmentType)
        {
            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((Action)(() => this._control.CustomizeTabsForEnvironmentType(changingEnvironmentType)));
        }

        public void ReapplyFilter()
        {
            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke((Action)(() =>
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

        internal void LoadCloudWatchData(LineSeries series, string metricNamespace, string metricName, string stats, string units, List<Dimension> dimensions, int hoursToView)
        {
            var df = new CloudWatchDataFetcher(this._cwClient, series, metricNamespace, metricName, stats, units, dimensions, hoursToView);
            ThreadPool.QueueUserWorkItem(df.Execute);
        }

        public void RequestEnvironmentLogs()
        {
            var request = new RequestEnvironmentInfoRequest()
            {
                EnvironmentName = this._environmentModel.Environment.EnvironmentName,
                InfoType = "tail"
            };

            this._beanstalkClient.RequestEnvironmentInfo(request);
        }

        public RetrieveEnvironmentInfoResponse RetrieveEnvironmentLogs()
        {
            var request = new RetrieveEnvironmentInfoRequest()
            {
                EnvironmentName = this._environmentModel.Environment.EnvironmentName,
                InfoType = "tail"
            };

            var response = this._beanstalkClient.RetrieveEnvironmentInfo(request);
            return response;
        }
    }
}
