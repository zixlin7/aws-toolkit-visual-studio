using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Amazon.AWSToolkit.CommonUI;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class NetworkAclWrapper : PropertiesModel, ISubnetAssociationWrapper, ITagSupport
    {
        NetworkAcl _networkAcl;
        IList<Subnet> _allSubnets;

        public NetworkAclWrapper(NetworkAcl networkAcl, IList<Subnet> subnets)
        {
            this._networkAcl = networkAcl;
            this._allSubnets = subnets;
        }

        public override void GetPropertyNames(out string className, out string componentName)
        {
            className = "Network ACL";
            componentName = this._networkAcl.NetworkAclId;
        }

        [Browsable(false)]
        public NetworkAcl NativeNetworkAcl => this._networkAcl;

        [Browsable(false)]
        public string DisplayName => this.NativeNetworkAcl.NetworkAclId;

        [Browsable(false)]
        public string TypeName => "Network ACL";

        [Browsable(false)]
        public string FormattedLabel
        {
            get
            {
                var tag = FindTag("Name");
                if (tag == null)
                    return string.Format("{0}", this._networkAcl.NetworkAclId);
                else
                    return string.Format("{0} - {1}", tag.Value, this._networkAcl.NetworkAclId);
            }
        }

        [DisplayName("ID")]
        [AssociatedIcon(false, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.networkacl.png")]
        public string NetworkAclId => this.NativeNetworkAcl.NetworkAclId;


        [DisplayName("VPC")]
        public string VpcId => this.NativeNetworkAcl.VpcId;

        [DisplayName("Default")]
        public string FormattedDefault => this.NativeNetworkAcl.IsDefault.ToString();

        [Browsable(false)]
        public bool CanDisassociate => !this.NativeNetworkAcl.IsDefault;

        ObservableCollection<NetworkAclAssociationWrapper> _associations;
        [Browsable(false)]
        public ObservableCollection<NetworkAclAssociationWrapper> Associations
        {
            get
            {
                if (this._associations == null)
                {
                    this._associations = new ObservableCollection<NetworkAclAssociationWrapper>();
                    foreach (var association in this._networkAcl.Associations)
                    {
                        if (string.IsNullOrEmpty(association.SubnetId))
                            continue;

                        Subnet subnet = null;
                        if(this._allSubnets != null)
                            subnet = this._allSubnets.FirstOrDefault(s => s.SubnetId == association.SubnetId);
                        this._associations.Add(new NetworkAclAssociationWrapper(association, subnet));
                    }
                }

                return this._associations;
            }
        }

        ObservableCollection<NetworkAclEntryWrapper> _ingressEntries;
        [Browsable(false)]
        public ObservableCollection<NetworkAclEntryWrapper> IngressEntries
        {
            get
            {
                if (this._ingressEntries == null)
                {
                    this._ingressEntries = new ObservableCollection<NetworkAclEntryWrapper>();
                    foreach (var item in this._networkAcl.Entries)
                    {
                        if (item.Egress)
                            continue;

                        this._ingressEntries.Add(new NetworkAclEntryWrapper(item));
                    }
                }

                return this._ingressEntries;
            }
        }

        ObservableCollection<NetworkAclEntryWrapper> _egressEntries;
        [Browsable(false)]
        public ObservableCollection<NetworkAclEntryWrapper> EgressEntries
        {
            get
            {
                if (this._egressEntries == null)
                {
                    this._egressEntries = new ObservableCollection<NetworkAclEntryWrapper>();
                    foreach (var item in this._networkAcl.Entries)
                    {
                        if (!item.Egress)
                            continue;

                        this._egressEntries.Add(new NetworkAclEntryWrapper(item));
                    }
                }

                return this._egressEntries;
            }
        }

        public Tag FindTag(string name)
        {
            if (this.NativeNetworkAcl.Tags == null)
                return null;

            return this.NativeNetworkAcl.Tags.FirstOrDefault(x => string.Equals(x.Key, name));
        }

        public void SetTag(string name, string value)
        {
            var tag = FindTag(name);
            if (tag == null)
            {
                tag = new Tag();
                tag.Key = name;
                tag.Value = value;
                this.NativeNetworkAcl.Tags.Add(tag);
            }
            else
            {
                tag.Value = value;
            }
        }

        [Browsable(false)]
        public List<Tag> Tags => this.NativeNetworkAcl.Tags;
    }
}
