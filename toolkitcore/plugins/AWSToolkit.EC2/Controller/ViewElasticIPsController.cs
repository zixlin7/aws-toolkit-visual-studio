using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.EC2.Nodes;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;

using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class ViewElasticIPsController : FeatureController<ViewElasticIPsModel>
    {
        ViewElasticIPsControl _control;

        protected override void DisplayView()
        {
            this._control = new ViewElasticIPsControl(this);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);
        }

        public void LoadModel()
        {
            RefreshElasticIPs();
        }


        public void RefreshElasticIPs()
        {
            var response = this.EC2Client.DescribeAddresses(new DescribeAddressesRequest());

            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
            {
                this.Model.Addresses.Clear();
                foreach (var item in response.Addresses)
                {
                    this.Model.Addresses.Add(new AddressWrapper(item));
                }
            }));
        }

        public AddressWrapper Allocate()
        {
            var controller = new AllocateAddressController();
            var results = controller.Execute(this.EC2Client);

            if (results.Success)
            {
                this.RefreshElasticIPs();
                foreach (var item in this.Model.Addresses)
                {
                    if (string.Equals(item.PublicIp, results.FocalName))
                        return item;
                }
            }

            return null;
        }

        public void Associate(AddressWrapper address)
        {
            var controller = new AssociateAddressController();
            var results = controller.Execute(this.EC2Client, address);

            if(results.Success)
                this.RefreshElasticIPs();
        }

        public void Release(AddressWrapper address)
        {
            ReleaseAddressRequest request = null;
            if (address.NativeAddress.Domain == AddressWrapper.DOMAIN_EC2)
                request = new ReleaseAddressRequest() { PublicIp = address.PublicIp };
            else
                request = new ReleaseAddressRequest() { AllocationId = address.AllocationId };

            this.EC2Client.ReleaseAddress(request);
            this.RefreshElasticIPs();
        }

        public void Disassociate(AddressWrapper address)
        {
            DisassociateAddressRequest request = null;
            if (address.NativeAddress.Domain == AddressWrapper.DOMAIN_EC2)
                request = new DisassociateAddressRequest() { PublicIp = address.PublicIp };
            else
                request = new DisassociateAddressRequest() { AssociationId = address.AssociationId };

            this.EC2Client.DisassociateAddress(request);
            this.RefreshElasticIPs();
        }
    }
}
