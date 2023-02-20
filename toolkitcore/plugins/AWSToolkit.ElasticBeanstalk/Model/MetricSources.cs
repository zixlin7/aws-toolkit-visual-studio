using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Telemetry.Model;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Model
{
    public class MetricSources
    {
        public class BeanstalkMetricSource : BaseMetricSource
        {
            public static readonly BaseMetricSource Project = new BeanstalkMetricSource(null, "project");
            public static readonly BaseMetricSource ApplicationView = new BeanstalkMetricSource("applicationView");

            private BeanstalkMetricSource(string location) : this(ServiceNames.Beanstalk, location)
            {
            }

            private BeanstalkMetricSource(string service, string location) : base(service, location)
            {
            }
        }
    }
}
