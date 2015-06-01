using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

using Amazon.ElasticBeanstalk.Model;
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

        public AutoScalingGroup NativeAutoScalingGroup
        {
            get { return this._originalAutoScalingGroup; }
        }

        public LaunchConfiguration NativeLaunchConfiguration
        {
            get { return this._originalLaunchConfiguration; }
        }

        public string FormattedSecurityGroups
        {
            get { return String.Join(", ", this._originalLaunchConfiguration.SecurityGroups.ToArray()); }
        }

        public string Name
        {
            get { return this._originalAutoScalingGroup.AutoScalingGroupName; }
        }
        public string MinSize
        {
            get { return EnvironmentStatusModel.StringIfSet(this._originalAutoScalingGroup.MinSize, "", ""); }
        }
        public string MaxSize
        {
            get { return EnvironmentStatusModel.StringIfSet(this._originalAutoScalingGroup.MaxSize, "", ""); }
        }
        public string DesiredCapacity
        {
            get { return EnvironmentStatusModel.StringIfSet(this._originalAutoScalingGroup.DesiredCapacity, "", ""); }
        }
        public string DefaultCooldown
        {
            get { return EnvironmentStatusModel.StringIfSet(this._originalAutoScalingGroup.DefaultCooldown, "", ""); }
        }
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
        public string AvailabilityZones
        {
            get { return String.Join(", ", this._originalAutoScalingGroup.AvailabilityZones.ToArray()); }
        }
        public string LoadBalancers
        {
            get { return String.Join(", ", this._originalAutoScalingGroup.LoadBalancerNames.ToArray()); }
        }
        public string LaunchConfig
        {
            get { return this._originalAutoScalingGroup.LaunchConfigurationName; }
        }


        //Properties below here are not shown.
        public string CreatedTime
        {
            get { return EnvironmentStatusModel.StringIfSet(this._originalAutoScalingGroup.CreatedTime, "", ""); }
        }
        public string ARN
        {
            get { return this._originalAutoScalingGroup.AutoScalingGroupARN; }
        }
        public string HealthCheckType
        {
            get { return this._originalAutoScalingGroup.HealthCheckType; }
        }
        public string HealthCheckGracePeriod
        {
            get { return EnvironmentStatusModel.StringIfSet(this._originalAutoScalingGroup.HealthCheckGracePeriod, "", ""); }
        }
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
        public string PlacementGroup
        {
            get { return this._originalAutoScalingGroup.PlacementGroup; }
        }
        public string VPCZoneIdentifier
        {
            get { return this._originalAutoScalingGroup.VPCZoneIdentifier; }
        }
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

        public string FormattedAvailabilityZones
        {
            get { return StringUtils.CreateCommaDelimitedList(this._originalAutoScalingGroup.AvailabilityZones); }
        }

        public string FormattedLoadBalancerNames
        {
            get { return StringUtils.CreateCommaDelimitedList(this._originalAutoScalingGroup.LoadBalancerNames); }
        }

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
