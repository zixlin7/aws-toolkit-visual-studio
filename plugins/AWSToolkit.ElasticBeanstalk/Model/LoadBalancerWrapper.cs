using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

using Amazon.ElasticBeanstalk.Model;
using Amazon.ElasticLoadBalancing.Model;

using Amazon.AWSToolkit.Util;
using LoadBalancerDescription = Amazon.ElasticLoadBalancing.Model.LoadBalancerDescription;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Model
{
    public class LoadBalancerWrapper
    {
        LoadBalancerDescription _originalLoadBalancer;

        public LoadBalancerWrapper(LoadBalancerDescription originalLoadBalancer)
        {
            this._originalLoadBalancer = originalLoadBalancer;
        }

        public string Name
        {
            get { return this._originalLoadBalancer.LoadBalancerName; }
        }
        public string Instances
        {
            get
            {
                List<string> instances = new List<string>();
                foreach (Amazon.ElasticLoadBalancing.Model.Instance instance in this._originalLoadBalancer.Instances)
                {
                    instances.Add(instance.InstanceId);
                }
                return String.Join(", ", instances.ToArray());
            }
        }

        public string FormattedHealthCheck
        {
            get { return String.Format("{0}{1}{2}{3}{4}",
                this._originalLoadBalancer.HealthCheck.Target,
                EnvironmentStatusModel.StringIfSet(this._originalLoadBalancer.HealthCheck.Interval, " with Interval ", ""),
                EnvironmentStatusModel.StringIfSet(this._originalLoadBalancer.HealthCheck.Timeout, " with TimeOut ", ""),
                EnvironmentStatusModel.StringIfSet(this._originalLoadBalancer.HealthCheck.HealthyThreshold, " with Healthy Threshold ", ""),
                EnvironmentStatusModel.StringIfSet(this._originalLoadBalancer.HealthCheck.UnhealthyThreshold, " with Unhealthy Threshold ", ""));
            }
        }

        public string FormattedInstances
        {
            get
            {
                List<string> values = new List<string>();

                if (this._originalLoadBalancer.Instances != null)
                {
                    this._originalLoadBalancer.Instances.ForEach(x => values.Add(x.InstanceId));
                }

                return StringUtils.CreateCommaDelimitedList(values);
            }
        }

        //Properties below here currently not displayed
        public string HostedZoneNameId
        {
            get { return this._originalLoadBalancer.CanonicalHostedZoneNameID; }
        }
        public string Listeners
        {
            get
            {
                List<string> listeners = new List<string>();
                foreach (ListenerDescription ld in this._originalLoadBalancer.ListenerDescriptions)
                {
                    listeners.Add(String.Format("{0} with Policies: {1}",
                        String.Format("Mapping {0}{1} from port {2} to port {3}",
                            ld.Listener.Protocol,
                            EnvironmentStatusModel.StringIfSet(ld.Listener.SSLCertificateId, "with Certificate ", ""),
                            ld.Listener.LoadBalancerPort.ToString(),
                            ld.Listener.InstancePort.ToString()),
                        String.Join(", ",ld.PolicyNames.ToArray())));
                }
                return String.Join("\n", listeners.ToArray());
            }
        }

        public HealthCheck HealthCheck
        {
            get
            {
                return this._originalLoadBalancer.HealthCheck;
            }
        }

        public string Policies
        {
            get
            {
                List<string> policies = new List<string>();
                foreach (AppCookieStickinessPolicy policy in this._originalLoadBalancer.Policies.AppCookieStickinessPolicies )
                {
                    policies.Add(String.Format("Policy {0} with Cookie {1}", policy.PolicyName, policy.CookieName));
                }
                foreach (LBCookieStickinessPolicy policy in this._originalLoadBalancer.Policies.LBCookieStickinessPolicies)
                {
                    policies.Add(String.Format("Policy {0}{1}", policy.PolicyName,
                        EnvironmentStatusModel.StringIfSet(policy.CookieExpirationPeriod, " with Cookie Epriration Period ", "")));
                }
                return String.Join("\n", policies.ToArray());
            }
        }
        public string DNSName
        {
            get { return this._originalLoadBalancer.DNSName; }
        }
        public string HostedZoneName
        {
            get { return this._originalLoadBalancer.CanonicalHostedZoneName; }
        }
        public string AvailabilityZones
        {
            get { return String.Join(", ", this._originalLoadBalancer.AvailabilityZones.ToArray()); }
        }
        public string CreatedTime
        {
            get { return this._originalLoadBalancer.CreatedTime.ToString(); }
        }
    }
}
