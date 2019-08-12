using System.ComponentModel;
using Amazon.AWSToolkit.CommonUI;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class AddressWrapper : PropertiesModel, IWrapper
    {
        public const string DOMAIN_VPC = "vpc";
        public const string DOMAIN_EC2 = "standard";

        Address _address;

        public AddressWrapper(Address address)
        {
            this._address = address;
        }

        public override void GetPropertyNames(out string className, out string componentName)
        {
            className = "Elastic IP";
            componentName = this.DisplayName;
        }

        [Browsable(false)]
        public Address NativeAddress => this._address;

        [Browsable(false)]
        public string DisplayName => this._address.PublicIp;

        [Browsable(false)]
        public string TypeName => "Elastic IP";

        [DisplayName("Address")]
        [AssociatedIcon(false, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.elastic-ip.png")]
        public string PublicIp => this._address.PublicIp;

        [DisplayName("Private IP")]
        public string PrivateIpAddress => this._address.PrivateIpAddress;

        [DisplayName("Allocation ID")]
        public string AllocationId => this._address.AllocationId;

        [DisplayName("Association ID")]
        public string AssociationId => this._address.AssociationId;

        [DisplayName("Scope")]
        public string Domain => this._address.Domain;

        [DisplayName("Instance ID")]
        public string InstanceId => this._address.InstanceId;

        [DisplayName("Network Interface")]
        public string NetworkInterfaceId => this._address.NetworkInterfaceId;

        [DisplayName("Network Interface Owner")]
        public string NetworkInterfaceOwnerId => this._address.NetworkInterfaceOwnerId;

        [Browsable(false)]
        public bool IsAddressInUse => !string.IsNullOrEmpty(this.NativeAddress.InstanceId) || !string.IsNullOrEmpty(this.NativeAddress.NetworkInterfaceId);
    }
}
