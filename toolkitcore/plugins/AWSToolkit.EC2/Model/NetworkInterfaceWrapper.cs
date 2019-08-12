using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Amazon.AWSToolkit.CommonUI;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class NetworkInterfaceWrapper : PropertiesModel, IWrapper, ITagSupport
    {
        NetworkInterface _networkInterface;

        public NetworkInterfaceWrapper(NetworkInterface networkInterface)
        {
            this._networkInterface = networkInterface;
        }

        public override void GetPropertyNames(out string className, out string componentName)
        {
            className = "Network Interface";
            componentName = this._networkInterface.NetworkInterfaceId;
        }

        [Browsable(false)]
        public NetworkInterface NativeNetworkInterface => this._networkInterface;

        [Browsable(false)]
        public string DisplayName => this.NativeNetworkInterface.NetworkInterfaceId;

        [Browsable(false)]
        public string TypeName => "Network Interface";

        [DisplayName("Network Interface ID")]
        [AssociatedIcon(false, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.route-table.png")]
        public string NetworkInterfaceId => this._networkInterface.NetworkInterfaceId;

        [Browsable(false)]
        public bool CanDelete => this._networkInterface.Association == null;

        [DisplayName("VPC")]
        public string VpcId => this._networkInterface.VpcId;

        [Browsable(false)]
        public string FormattedLabel
        {
            get
            {
                var tag = FindTag("Name");
                if (tag == null)
                    return string.Format("{0}", this._networkInterface.NetworkInterfaceId);
                else
                    return string.Format("{0} - {1}", tag.Value, this._networkInterface.NetworkInterfaceId);
            }
        }



        public Tag FindTag(string name)
        {
            if (this._networkInterface.TagSet == null)
                return null;

            return this._networkInterface.TagSet.FirstOrDefault(x => string.Equals(x.Key, name));
        }

        public void SetTag(string name, string value)
        {
            var tag = FindTag(name);
            if (tag == null)
            {
                tag = new Tag();
                tag.Key = name;
                tag.Value = value;
                this._networkInterface.TagSet.Add(tag);
            }
            else
            {
                tag.Value = value;
            }
        }

        [Browsable(false)]
        public List<Tag> Tags => this._networkInterface.TagSet;
    }
}
