using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;
using Amazon.AWSToolkit.Util;

using Amazon.EC2;
using Amazon.EC2.Model;


namespace Amazon.AWSToolkit.EC2.Controller
{
    public class AssociateAddressController
    {
        ActionResults _results;
        AssociateAddressModel _model;
        IAmazonEC2 _ec2Client;

        public ActionResults Execute(IAmazonEC2 ec2Client, AddressWrapper address)
        {
            this._ec2Client = ec2Client;
            this._model = new AssociateAddressModel(address);
            this._model.AvailableInstances = this.GetAvailableInstances();

            if (this.Model.AvailableInstances.Count == 0)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("There are no running instances to associate the address to.");
                return new ActionResults().WithSuccess(false);
            }
            this._model.Instance = this._model.AvailableInstances[0];

            var control = new AssociateAddressControl(this);
            ToolkitFactory.Instance.ShellProvider.ShowModal(control);

            if (this._results == null)
                return new ActionResults().WithSuccess(false);

            return this._results;
        }

        List<AssociateAddressModel.InstanceItem> GetAvailableInstances()
        {
            List<AssociateAddressModel.InstanceItem> items = new List<AssociateAddressModel.InstanceItem>();
            var addressResponse = this._ec2Client.DescribeAddresses(new DescribeAddressesRequest());
            var instanceResponse = this._ec2Client.DescribeInstances(new DescribeInstancesRequest());

            foreach (var reservation in instanceResponse.Reservations)
            {
                foreach (var instance in reservation.Instances)
                {
                    if (instance.State.Name == EC2Constants.INSTANCE_STATE_SHUTTING_DOWN ||
                        instance.State.Name == EC2Constants.INSTANCE_STATE_TERMINATED)
                        continue;

                    if (addressResponse.Addresses.FirstOrDefault(x => string.Equals(x.PublicIp, instance.PublicIpAddress)) != null)
                        continue;

                    if (this._model.Address.Domain == AddressWrapper.DOMAIN_EC2 && string.IsNullOrEmpty(instance.SubnetId))
                        items.Add(new AssociateAddressModel.InstanceItem(instance));
                    else if(this._model.Address.Domain == AddressWrapper.DOMAIN_VPC && !string.IsNullOrEmpty(instance.SubnetId))
                        items.Add(new AssociateAddressModel.InstanceItem(instance));
                }
            }

            return items;
        }        

        public AssociateAddressModel Model
        {
            get { return this._model; }
        }

        public void AssociateAddress()
        {
            AssociateAddressRequest request = null;
            if (this._model.Address.Domain == AddressWrapper.DOMAIN_EC2)
            {
                request = new AssociateAddressRequest
                {
                    PublicIp = this._model.Address.NativeAddress.PublicIp,
                    InstanceId = this._model.Instance.InstanceId
                };
            }
            else
            {
                request = new AssociateAddressRequest
                {
                    AllocationId = this._model.Address.NativeAddress.AllocationId,
                    InstanceId = this._model.Instance.InstanceId
                };
            }

            var response = this._ec2Client.AssociateAddress(request);

            this._results = new ActionResults().WithSuccess(true);
        }
    }
}
