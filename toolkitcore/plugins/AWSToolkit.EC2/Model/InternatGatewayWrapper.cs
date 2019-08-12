using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Amazon.AWSToolkit.CommonUI;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class InternetGatewayWrapper : PropertiesModel, IWrapper, ITagSupport
    {
        InternetGateway _gateway;

        public InternetGatewayWrapper(InternetGateway gateway)
        {
            this._gateway = gateway;
        }

        public override void GetPropertyNames(out string className, out string componentName)
        {
            className = "Internet Gateway";
            componentName = this._gateway.InternetGatewayId;
        }

        [Browsable(false)]
        public InternetGateway NativeInternetGateway => this._gateway;

        [Browsable(false)]
        public string DisplayName => this.NativeInternetGateway.InternetGatewayId;

        [Browsable(false)]
        public string TypeName => "Internet Gateway";

        [Browsable(false)]
        public string FormattedLabel
        {
            get
            {
                var tag = FindTag("Name");
                if (tag == null)
                    return string.Format("{0}", this._gateway.InternetGatewayId);
                else
                    return string.Format("{0} - {1}", tag.Value, this._gateway.InternetGatewayId);
            }
        }

        [DisplayName("ID")]
        [AssociatedIcon(false, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.internet-gateway.png")]
        public string InternetGatewayId => this.NativeInternetGateway.InternetGatewayId;

        [DisplayName("State")]
        [AssociatedIcon(true, "StateIcon")]
        public string State
        {
            get 
            {
                string state = "available";
                foreach(var attachment in this.NativeInternetGateway.Attachments)
                {
                    if(attachment.State.Value.EndsWith("ing"))
                    {
                        state = "pending";
                        break;
                    }
                }
                return state; 
            }
        }

        [Browsable(false)]
        public System.Windows.Media.ImageSource StateIcon
        {
            get
            {
                string iconPath;
                Assembly assembly = null;
                switch (State)
                {
                    case "available":
                        iconPath = "green-circle.png";
                        break;
                    default:
                        iconPath = "yellow-circle.png";
                        break;
                }

                System.Windows.Controls.Image icon;
                if (assembly == null)
                    icon = IconHelper.GetIcon(iconPath);
                else
                    icon = IconHelper.GetIcon(assembly, iconPath);

                return icon.Source;
            }
        }

        [DisplayName("VPC")]
        public string FormattedVPC
        {
            get
            {
                var vpcIds = new List<string>();
                this.NativeInternetGateway.Attachments.ForEach(x => vpcIds.Add(string.Format("{0} ({1})", x.VpcId, x.State)));

                return AWSToolkit.Util.StringUtils.CreateCommaDelimitedList(vpcIds);
            }
        }

        [Browsable(false)]
        public string VpcId
        {
            get
            {
                var vpcIds = new List<string>();
                this.NativeInternetGateway.Attachments.ForEach(x => vpcIds.Add(x.VpcId));

                return AWSToolkit.Util.StringUtils.CreateCommaDelimitedList(vpcIds);
            }
        }

        public Tag FindTag(string name)
        {
            if (this.NativeInternetGateway.Tags == null)
                return null;

            return this.NativeInternetGateway.Tags.FirstOrDefault(x => string.Equals(x.Key, name));
        }

        public void SetTag(string name, string value)
        {
            var tag = FindTag(name);
            if (tag == null)
            {
                tag = new Tag();
                tag.Key = name;
                tag.Value = value;
                this.NativeInternetGateway.Tags.Add(tag);
            }
            else
            {
                tag.Value = value;
            }
        }

        [Browsable(false)]
        public List<Tag> Tags => this.NativeInternetGateway.Tags;

        internal void ClearAttachments()
        {
            this._gateway.Attachments.Clear();
            base.NotifyPropertyChanged("VPC");
        }
    }
}
