using System.Collections.Generic;
using System.Linq;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Model
{
    public class AvailabilityZone
    {
        public string ZoneId { get; set; }
    }

    public class LoadBalancer
    {
        public string Name { get; set; }
        public string HostedZoneNameId { get; set; }
        public string DNSName { get; set; }
        public List<AvailabilityZone> AvailabilityZones { get; } = new List<AvailabilityZone>();
        public string FormattedAvailabilityZones => string.Join(", ",
            AvailabilityZones
                .Select(az => az.ZoneId)
                .OrderBy(s => s));
    }
}
