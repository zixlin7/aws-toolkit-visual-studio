using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Util;

using Amazon.ElasticLoadBalancing.Model;

namespace Amazon.AWSToolkit.CloudFormation.Model
{
    public class LoadBalancerDescriptionWrapper
    {
        LoadBalancerDescription _loadBalancer;

        public LoadBalancerDescriptionWrapper(LoadBalancerDescription loadBalancer)
        {
            this._loadBalancer = loadBalancer;
        }

        public LoadBalancerDescription NativeLoadBalancerDescription
        {
            get { return this._loadBalancer; }
        }

        public string FormattedAvailabilityZones
        {
            get { return StringUtils.CreateCommaDelimitedList(this._loadBalancer.AvailabilityZones); }
        }

        public string FormattedInstances
        {
            get
            {
                List<string> values = new List<string>();

                if (this._loadBalancer.Instances != null)
                {
                    this._loadBalancer.Instances.ForEach(x => values.Add(x.InstanceId));
                }

                return StringUtils.CreateCommaDelimitedList(values);
            }
        }

        public string FormattedHealthCheck
        {
            get { return StringUtils.CreateCommaDelimitedList(this._loadBalancer.AvailabilityZones); }
        }

        List<ListenerDescriptionWrapper> _listenerDescriptions;
        public List<ListenerDescriptionWrapper> ListenerDescriptions
        {
            get
            {
                if (this._listenerDescriptions == null)
                {
                    this._listenerDescriptions = new List<ListenerDescriptionWrapper>();
                    this.NativeLoadBalancerDescription.ListenerDescriptions.ForEach(x => this._listenerDescriptions.Add(new ListenerDescriptionWrapper(x)));
                }
                return this._listenerDescriptions;
            }
        }

        public class ListenerDescriptionWrapper
        {
            ListenerDescription _listenerDescription;
            public ListenerDescriptionWrapper(ListenerDescription listenerDescription)
            {
                this._listenerDescription = listenerDescription;
            }

            public ListenerDescription NativeListenerDescription
            {
                get { return this._listenerDescription; }
            }

            public string FormattedPolicyNames
            {
                get { return StringUtils.CreateCommaDelimitedList(this._listenerDescription.PolicyNames); }
            }
        }
    }
}
