using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;

using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class AttachElasticIPToInstanceController
    {
        ActionResults _results;
        AttachElasticIPToInstanceModel _model;
        IAmazonEC2 _ec2Client;

        public ActionResults Execute(IAmazonEC2 ec2Client, RunningInstanceWrapper instance)
        {
            this._ec2Client = ec2Client;
            this._model = new AttachElasticIPToInstanceModel(instance);
            this._model.AvailableAddresses = this.GetElasticIPAddress();


            if (this.Model.AvailableAddresses.Count > 0)
            {
                this._model.SelectedAddress = this._model.AvailableAddresses[0];
            }

            var control = new AttachElasticIPToInstanceControl(this);
            ToolkitFactory.Instance.ShellProvider.ShowModal(control);

            if (this._results == null)
                return new ActionResults().WithSuccess(false);

            return this._results;
        }

        public AttachElasticIPToInstanceModel Model
        {
            get { return this._model; }
        }

        List<AddressWrapper> GetElasticIPAddress()
        {
            List<AddressWrapper> ips = new List<AddressWrapper>();

            var response = this._ec2Client.DescribeAddresses(new DescribeAddressesRequest());

            foreach (var address in response.Addresses)
            {
                if (!string.IsNullOrEmpty(address.InstanceId))
                    continue;

                if (string.IsNullOrEmpty(this._model.Instance.VpcId))
                {
                    if (address.Domain == AddressWrapper.DOMAIN_EC2)
                        ips.Add(new AddressWrapper(address));
                }
                else
                {
                    if (address.Domain == AddressWrapper.DOMAIN_VPC)
                        ips.Add(new AddressWrapper(address));
                }
            }

            return ips;
        }

        public void Attach()
        {
            var associateRequest = new AssociateAddressRequest(){InstanceId = this.Model.Instance.InstanceId};
            if (this.Model.ActionSelectedAddress)
            {
                if(string.IsNullOrEmpty(this.Model.Instance.VpcId))
                    associateRequest.PublicIp = this.Model.SelectedAddress.PublicIp;
                else
                    associateRequest.AllocationId = this.Model.SelectedAddress.AllocationId;
            }
            else
            {
                var allocateRequest = new AllocateAddressRequest() { Domain = string.IsNullOrEmpty(this.Model.Instance.VpcId) ? AddressWrapper.DOMAIN_EC2 : AddressWrapper.DOMAIN_VPC };
                var allocateResponse = this._ec2Client.AllocateAddress(allocateRequest);
                if (string.IsNullOrEmpty(this.Model.Instance.VpcId))
                    associateRequest.PublicIp = allocateResponse.PublicIp;
                else
                    associateRequest.AllocationId = allocateResponse.AllocationId;
            }

            this._ec2Client.AssociateAddress(associateRequest);
            this._results = new ActionResults() { Success = true };
        }
    }
}
