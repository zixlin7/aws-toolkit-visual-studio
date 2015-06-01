using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Util;

using Amazon.AutoScaling.Model;

namespace Amazon.AWSToolkit.CloudFormation.Model
{
    public class AutoScalingGroupWrapper
    {
        AutoScalingGroup _group;

        public AutoScalingGroupWrapper(AutoScalingGroup group)
        {
            this._group = group;
        }

        public AutoScalingGroup NativeAutoScalingGroup
        {
            get { return this._group; }
        }

        public string FormattedAvailabilityZones
        {
            get { return StringUtils.CreateCommaDelimitedList(this._group.AvailabilityZones); }
        }

        public string FormattedLoadBalancerNames
        {
            get { return StringUtils.CreateCommaDelimitedList(this._group.LoadBalancerNames); }
        }

        public string FormattedEnabledMetrics
        {
            get
            {
                List<string> values = new List<string>();

                if (this._group.EnabledMetrics != null)
                {
                    this._group.EnabledMetrics.ForEach(x => values.Add(x.Metric));
                }

                return StringUtils.CreateCommaDelimitedList(values);
            }
        }

        public string FormattedInstances
        {
            get
            {
                List<string> values = new List<string>();

                if (this._group.Instances != null)
                {
                    this._group.Instances.ForEach(x => values.Add(x.InstanceId));
                }

                return StringUtils.CreateCommaDelimitedList(values);
            }
        }
    }
}
