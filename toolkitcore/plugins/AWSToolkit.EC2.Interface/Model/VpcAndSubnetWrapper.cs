using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Model
{
    /// <summary>
    /// Flattened wrapper for a vpc with subnet, for the purposes of presenting a grouped
    /// or hierarchical UI based on the vpc id.
    /// </summary>
    public class VpcAndSubnetWrapper
    {
        public VpcAndSubnetWrapper(Vpc vpc, Subnet subnet)
        {
            Vpc = new VPCWrapper(vpc);
            Subnet = new SubnetWrapper(subnet, null, null);
        }

        public VpcAndSubnetWrapper(Vpc vpc, string pseudoSubnetId)
        {
            Vpc = new VPCWrapper(vpc);
            Subnet = new SubnetWrapper(pseudoSubnetId);
        }

        public VpcAndSubnetWrapper(string pseudoVpcId, string pseudoSubnetId)
        {
            Vpc = new VPCWrapper(pseudoVpcId);
            Subnet = new SubnetWrapper(pseudoSubnetId);
        }

        public string FormattedSubnet
        {
            get
            {
                if (Subnet == null)
                    return string.Empty;

                return string.Format("{0} (vpc {1})", Subnet.ShortDisplayDetails, Vpc.VpcId);
            }
        }

        public string VpcGroupingHeader
        {
            get
            {
                if (Vpc == null)
                    return string.Empty;

                return Vpc.FormattedLabel;
            }
        }

        public string FormattedMapPublicIpOnLaunch
        {
            get
            {
                if (Subnet == null)
                    return string.Empty;

                return Subnet.NativeSubnet.MapPublicIpOnLaunch.ToString();
            }
        }

        public VPCWrapper Vpc { get; set; }

        public SubnetWrapper Subnet { get; set; }
    }
}
