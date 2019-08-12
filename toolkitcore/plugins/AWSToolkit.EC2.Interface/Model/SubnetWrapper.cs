using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class SubnetWrapper : PropertiesModel, IWrapper, ITagSupport
    {
        readonly Subnet _subnet;
        readonly RouteTable _routeTable;
        readonly NetworkAcl _networkAcl;

        public const string NoVpcSubnetPseudoId = "Do not use a VPC subnet";
        public const string NoPreferenceSubnetPseudoId = "No Preference (default subnet in any Availability Zone)";

        public SubnetWrapper(Subnet subnet, RouteTable routeTable, NetworkAcl networkAcl)
        {
            this._subnet = subnet;
            this._routeTable = routeTable;
            this._networkAcl = networkAcl;
        }

        public SubnetWrapper(string pseudoId)
        {
            this._subnet = null;
            this._routeTable = null;
            this._networkAcl = null;

            this.PseudoId = pseudoId;
        }

        public override void GetPropertyNames(out string className, out string componentName)
        {
            className = "Subnet";
            componentName = this.SubnetId;
        }

        [Browsable(false)]
        public Subnet NativeSubnet => this._subnet;

        [Browsable(false)]
        public string DisplayName => this.SubnetId;

        [Browsable(false)]
        public string TypeName => "Subnet";

        [Browsable(false)]
        public string ShortDisplayDetails
        {
            get
            {
                var sb = new StringBuilder();
                var tag = FindTag("Name");
                if (tag != null)
                    sb.AppendFormat("{0} - ", tag.Value);

                sb.Append(SubnetId);
                if (NativeSubnet != null)
                    sb.AppendFormat("({0}) {1}", CidrBlock, AvailabilityZone);

                return sb.ToString();
            }
        }

        [Browsable(false)]
        public string IdAndCidrDetails
        {
            get
            {
                var sb = new StringBuilder(SubnetId);
                if (NativeSubnet != null)
                    sb.AppendFormat(" ({0})", CidrBlock);
                return sb.ToString();
            }
        }

        [DisplayName("Subnet Id")]
        [AssociatedIcon(false, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.subnet.png")]
        public string SubnetId
        {
            get
            {
                if (!string.IsNullOrEmpty(PseudoId))
                    return PseudoId;

                return this._subnet.SubnetId;
            }
        }

        public string PseudoId { get; }

        public bool IsNoPreferencePseudoId => this.SubnetId.Equals(NoPreferenceSubnetPseudoId, StringComparison.Ordinal);

        public bool IsNoSubnetPseudoId => this.SubnetId.Equals(NoVpcSubnetPseudoId, StringComparison.Ordinal);

        [DisplayName("Availability Zone")]
        public string AvailabilityZone => NativeSubnet != null ? this.NativeSubnet.AvailabilityZone : string.Empty;

        [DisplayName("Available IPs")]
        public string FormattedAvailableIpAddressCount => NativeSubnet != null ? this.NativeSubnet.AvailableIpAddressCount.ToString() : string.Empty;

        [DisplayName("CIDR")]
        public string CidrBlock => NativeSubnet != null ? this.NativeSubnet.CidrBlock : string.Empty;

        [DisplayName("Default For Zone")]
        public string FormattedDefaultForAz => NativeSubnet != null ? this.NativeSubnet.DefaultForAz.ToString() : string.Empty;

        [DisplayName("Map Public IP on Launch")]
        public string FormattedMapPublicIpOnLaunch => NativeSubnet != null ? this.NativeSubnet.MapPublicIpOnLaunch.ToString() : string.Empty;

        [DisplayName("State")]
        [AssociatedIcon(true, "SubnetStateIcon")]
        public string SubnetState => NativeSubnet != null ? this.NativeSubnet.State : null;

        [Browsable(false)]
        public System.Windows.Media.ImageSource SubnetStateIcon
        {
            get
            {
                if (NativeSubnet == null)
                    return null;

                string iconPath;
                switch (NativeSubnet.State)
                {
                    case EC2Constants.SUBNET_STATE_AVAILABLE:
                        iconPath = "green-circle.png";
                        break;
                    case EC2Constants.SUBNET_STATE_PENDING:
                        iconPath = "yellow-circle.png";
                        break;
                    default:
                        iconPath = "red-circle.png";
                        break;
                }

                var icon = IconHelper.GetIcon(iconPath);
                return icon.Source;
            }
        }

        [DisplayName("VPC")]
        public string VpcId => NativeSubnet != null ? this.NativeSubnet.VpcId : string.Empty;

        [DisplayName("Route Table")]
        public string RouteTableId
        {
            get
            {
                if (this._routeTable == null)
                    return string.Empty;

                return this._routeTable.RouteTableId;
            }
        }

        [DisplayName("Network ACL")]
        public string NetworkAclId
        {
            get
            {
                if (this._networkAcl == null)
                    return string.Empty;

                if (this._networkAcl.IsDefault)
                    return "Default";

                return this._networkAcl.NetworkAclId;
            }
        }

        public Tag FindTag(string name)
        {
            if (this.NativeSubnet == null || this.NativeSubnet.Tags == null)
                return null;

            return this.NativeSubnet.Tags.FirstOrDefault(x => string.Equals(x.Key, name));
        }

        public void SetTag(string name, string value)
        {
            var tag = FindTag(name);
            if (tag == null)
            {
                tag = new Tag {Key = name, Value = value};
                this.NativeSubnet.Tags.Add(tag);
            }
            else
            {
                tag.Value = value;
            }
        }

        [Browsable(false)]
        public List<Tag> Tags => this.NativeSubnet.Tags;
    }
}
