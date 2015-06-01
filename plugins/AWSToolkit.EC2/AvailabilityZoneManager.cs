using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2
{
    public class AvailabilityZoneManager
    {
        static AvailabilityZoneManager _instance = new AvailabilityZoneManager();

        public static AvailabilityZoneManager Instance
        {
            get { return _instance; }
        }

        Dictionary<string, List<string>> _zonesByRegion
            = new Dictionary<string, List<string>>();

        private AvailabilityZoneManager() { }

        public IList<string> AvailabilityZonesForRegion(string region, IAmazonEC2 ec2Client)
        {
            if (!_zonesByRegion.ContainsKey(region))
            {
                loadAvailabilityZones(region, ec2Client);
            }

            return _zonesByRegion[region];
        }

        void loadAvailabilityZones(string region, IAmazonEC2 ec2Client)
        {
            var response = ec2Client.DescribeAvailabilityZones(new DescribeAvailabilityZonesRequest());

            foreach (var zone in response.AvailabilityZones)
            {
                if (!_zonesByRegion.ContainsKey(zone.RegionName))
                    _zonesByRegion.Add(zone.RegionName, new List<string>());

                _zonesByRegion[zone.RegionName].Add(zone.ZoneName);
            }
        }

    }
}
