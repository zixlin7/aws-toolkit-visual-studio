using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Amazon.AWSToolkit.CommonUI;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class VPCWrapper : PropertiesModel, IWrapper, ITagSupport
    {
        // some special 'id' strings that are used in wizards
        public const string NotInVpcPseudoId = "Not in VPC";
        public const string CreateNewVpcPseudoId = "Create new VPC";

        readonly Vpc _vpc;

        public VPCWrapper(Vpc vpc)
        {
            this._vpc = vpc;
        }

        public VPCWrapper(string pseudoId)
        {
            this._vpc = null;
            PseudoVpcId = pseudoId;
        }

        public override void GetPropertyNames(out string className, out string componentName)
        {
            className = "VPC";
            componentName = this.VpcId;
        }

        [Browsable(false)]
        public Vpc NativeVPC => this._vpc;

        [Browsable(false)]
        public string DisplayName => this.VpcId;

        [Browsable(false)]
        public string TypeName => "VPC";

        [DisplayName("VPC ID")]
        [AssociatedIcon(false, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.vpc.png")]
        public string VpcId
        {
            get
            {
                if (!string.IsNullOrEmpty(PseudoVpcId))
                    return PseudoVpcId;

                return this._vpc.VpcId;
            }
        }

        public string PseudoVpcId { get; }

        [DisplayName("State")]
        [AssociatedIcon(true, "VpcStateIcon")]
        public string VpcState => NativeVPC != null ? NativeVPC.State : null;

        [Browsable(false)]
        public System.Windows.Media.ImageSource VpcStateIcon
        {
            get
            {
                if (NativeVPC == null)
                    return null;

                string iconPath;
                switch (NativeVPC.State)
                {
                    case EC2Constants.VPC_STATE_AVAILABLE:
                        iconPath = "green-circle.png";
                        break;
                    case EC2Constants.VPC_STATE_PENDING:
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

        [DisplayName("CIDR")]
        public string CidrBlock => NativeVPC != null ? NativeVPC.CidrBlock : string.Empty;

        [DisplayName("DHCP Options Set")]
        public string DhcpOptionsId => NativeVPC != null ? NativeVPC.DhcpOptionsId : string.Empty;

        [DisplayName("Tenancy")]
        public string InstanceTenancy => NativeVPC != null ? NativeVPC.InstanceTenancy : null;

        [DisplayName("Default")]
        public string FormattedIsDefault => NativeVPC != null ? NativeVPC.IsDefault.ToString() : null;

        [Browsable(false)]
        public string FormattedLabel
        {
            get
            {
                if (this.VpcId.Equals(NotInVpcPseudoId, StringComparison.Ordinal) || this.VpcId.Equals(CreateNewVpcPseudoId, StringComparison.Ordinal))
                    return this.VpcId;

                if (_vpc.IsDefault)
                    return string.Format("Default VPC ({0})", this.VpcId);

                var sb = new StringBuilder();
                var tag = FindTag("Name");
                if (tag != null)
                    sb.AppendFormat("{0} - ", tag.Value);
                sb.Append(VpcId);
                if (NativeVPC != null)
                    sb.AppendFormat(" ({0})", CidrBlock);
                return sb.ToString();
            }
        }

        public Tag FindTag(string name)
        {
            if (this.NativeVPC == null || this.NativeVPC.Tags == null)
                return null;

            return this.NativeVPC.Tags.FirstOrDefault(x => string.Equals(x.Key, name));
        }

        public void SetTag(string name, string value)
        {
            var tag = FindTag(name);
            if (tag == null)
            {
                tag = new Tag {Key = name, Value = value};
                this.NativeVPC.Tags.Add(tag);
            }
            else
            {
                tag.Value = value;
            }
        }

        [Browsable(false)]
        public List<Tag> Tags => NativeVPC != null ? this.NativeVPC.Tags : null;
    }
}
