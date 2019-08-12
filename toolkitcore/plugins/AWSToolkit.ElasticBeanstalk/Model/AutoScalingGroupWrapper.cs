using System;
using System.Collections.Generic;
using Amazon.AutoScaling.Model;

using Amazon.AWSToolkit.Util;

using AutoScalingGroup = Amazon.AutoScaling.Model.AutoScalingGroup;
using LaunchConfiguration = Amazon.AutoScaling.Model.LaunchConfiguration;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Model
{
    public class AutoScalingGroupWrapper
    {
        AutoScalingGroup _originalAutoScalingGroup;
        LaunchConfiguration _originalLaunchConfiguration;

        public AutoScalingGroupWrapper(AutoScalingGroup originalAutoScalingGroup, LaunchConfiguration originalLaunchConfiguration)
        {
            this._originalAutoScalingGroup = originalAutoScalingGroup;
            this._originalLaunchConfiguration = originalLaunchConfiguration;
        }

        public AutoScalingGroup NativeAutoScalingGroup => this._originalAutoScalingGroup;

        public LaunchConfiguration NativeLaunchConfiguration => this._originalLaunchConfiguration;

        public string FormattedSecurityGroups => String.Join(", ", this._originalLaunchConfiguration.SecurityGroups.ToArray());

        public string Name => this._originalAutoScalingGroup.AutoScalingGroupName;

        public string MinSize => EnvironmentStatusModel.StringIfSet(this._originalAutoScalingGroup.MinSize, "", "");

        public string MaxSize => EnvironmentStatusModel.StringIfSet(this._originalAutoScalingGroup.MaxSize, "", "");

        public string DesiredCapacity => EnvironmentStatusModel.StringIfSet(this._originalAutoScalingGroup.DesiredCapacity, "", "");

        public string DefaultCooldown => EnvironmentStatusModel.StringIfSet(this._originalAutoScalingGroup.DefaultCooldown, "", "");

        public string Instances
        {
            get
            {
                List<string> instances = new List<string>();
                foreach (Amazon.AutoScaling.Model.Instance instance in this._originalAutoScalingGroup.Instances)
                {
                    instances.Add(String.Format("Id:{0} AZ:{1} State:{2} Status:{3} LaunchConfiguration:{4}",
                        instance.InstanceId,
                        instance.AvailabilityZone,
                        instance.LifecycleState,
                        instance.HealthStatus,
                        instance.LaunchConfigurationName));
                }
                return String.Join("\n", instances.ToArray());
            }
        }
        public string AvailabilityZones => String.Join(", ", this._originalAutoScalingGroup.AvailabilityZones.ToArray());

        public string LoadBalancers => String.Join(", ", this._originalAutoScalingGroup.LoadBalancerNames.ToArray());

        public string LaunchConfig => this._originalAutoScalingGroup.LaunchConfigurationName;


        //Properties below here are not shown.
        public string CreatedTime => EnvironmentStatusModel.StringIfSet(this._originalAutoScalingGroup.CreatedTime, "", "");

        public string ARN => this._originalAutoScalingGroup.AutoScalingGroupARN;

        public string HealthCheckType => this._originalAutoScalingGroup.HealthCheckType;

        public string HealthCheckGracePeriod => EnvironmentStatusModel.StringIfSet(this._originalAutoScalingGroup.HealthCheckGracePeriod, "", "");

        public string SuspendedProcesses
        {
            get
            {
                List<string> suspendedprocesses = new List<String>();
                foreach (SuspendedProcess process in this._originalAutoScalingGroup.SuspendedProcesses)
                {
                    suspendedprocesses.Add(String.Format("{0}:{1}", process.ProcessName, process.SuspensionReason));
                }
                return String.Join(", ", suspendedprocesses.ToArray());
            }
        }
        public string PlacementGroup => this._originalAutoScalingGroup.PlacementGroup;

        public string VPCZoneIdentifier => this._originalAutoScalingGroup.VPCZoneIdentifier;

        public string Metrics
        {
            get
            {
                List<string> metrics = new List<String>();
                foreach (EnabledMetric metric in this._originalAutoScalingGroup.EnabledMetrics)
                {
                    metrics.Add(String.Format("{0}:{1}", metric.Metric, metric.Granularity));
                }
                return String.Join(", ", metrics.ToArray());
            }
        }

        public string FormattedAvailabilityZones => StringUtils.CreateCommaDelimitedList(this._originalAutoScalingGroup.AvailabilityZones);

        public string FormattedLoadBalancerNames => StringUtils.CreateCommaDelimitedList(this._originalAutoScalingGroup.LoadBalancerNames);

        public string FormattedEnabledMetrics
        {
            get
            {
                List<string> values = new List<string>();

                if (this._originalAutoScalingGroup.EnabledMetrics != null)
                {
                    this._originalAutoScalingGroup.EnabledMetrics.ForEach(x => values.Add(x.Metric));
                }

                return StringUtils.CreateCommaDelimitedList(values);
            }
        }

        public string FormattedInstances
        {
            get
            {
                List<string> values = new List<string>();

                if (this._originalAutoScalingGroup.Instances != null)
                {
                    this._originalAutoScalingGroup.Instances.ForEach(x => values.Add(x.InstanceId));
                }

                return StringUtils.CreateCommaDelimitedList(values);
            }
        }

    }
}
