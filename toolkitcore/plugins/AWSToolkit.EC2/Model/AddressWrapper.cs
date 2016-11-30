using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

using Amazon.AWSToolkit.CommonUI;
using Amazon.EC2.Model;
using Amazon.AWSToolkit.EC2.View.DataGrid;

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
        public Address NativeAddress
        {
            get { return this._address; }
        }

        [Browsable(false)]
        public string DisplayName
        {
            get { return this._address.PublicIp; }
        }

        [Browsable(false)]
        public string TypeName
        {
            get { return "Elastic IP"; }
        }

        [DisplayName("Address")]
        [AssociatedIcon(false, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.elastic-ip.png")]
        public string PublicIp
        {
            get { return this._address.PublicIp; }
        }

        [DisplayName("Private IP")]
        public string PrivateIpAddress
        {
            get { return this._address.PrivateIpAddress; }
        }

        [DisplayName("Allocation ID")]
        public string AllocationId
        {
            get { return this._address.AllocationId; }
        }

        [DisplayName("Association ID")]
        public string AssociationId
        {
            get { return this._address.AssociationId; }
        }

        [DisplayName("Scope")]
        public string Domain
        {
            get { return this._address.Domain; }
        }

        [DisplayName("Instance ID")]
        public string InstanceId
        {
            get { return this._address.InstanceId; }
        }

        [DisplayName("Network Interface")]
        public string NetworkInterfaceId
        {
            get { return this._address.NetworkInterfaceId; }
        }

        [DisplayName("Network Interface Owner")]
        public string NetworkInterfaceOwnerId
        {
            get { return this._address.NetworkInterfaceOwnerId; }
        }

        [Browsable(false)]
        public bool IsAddressInUse
        {
            get
            {
                return !string.IsNullOrEmpty(this.NativeAddress.InstanceId) || !string.IsNullOrEmpty(this.NativeAddress.NetworkInterfaceId);
            }
        }
    }
}
