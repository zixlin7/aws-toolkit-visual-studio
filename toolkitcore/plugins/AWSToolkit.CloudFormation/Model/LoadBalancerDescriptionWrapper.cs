using System.Collections.Generic;
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

        public LoadBalancerDescription NativeLoadBalancerDescription => this._loadBalancer;

        public string FormattedAvailabilityZones => StringUtils.CreateCommaDelimitedList(this._loadBalancer.AvailabilityZones);

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

        public string FormattedHealthCheck => StringUtils.CreateCommaDelimitedList(this._loadBalancer.AvailabilityZones);

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

            public ListenerDescription NativeListenerDescription => this._listenerDescription;

            public string FormattedPolicyNames => StringUtils.CreateCommaDelimitedList(this._listenerDescription.PolicyNames);
        }
    }
}
