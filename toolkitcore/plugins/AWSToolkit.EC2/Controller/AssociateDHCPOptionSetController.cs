using System.Collections.ObjectModel;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;
using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class AssociateDHCPOptionSetController
    {
        ActionResults _results;
        AssociateDHCPOptionSetModel _model;
        IAmazonEC2 _ec2Client;

        public ActionResults Execute(IAmazonEC2 ec2Client, VPCWrapper vpc)
        {
            this._ec2Client = ec2Client;
            this._model = new AssociateDHCPOptionSetModel(vpc) { AvailableDHCPOptions = new ObservableCollection<DHCPOptionsWrapper>() };

            var response = this._ec2Client.DescribeDhcpOptions(new DescribeDhcpOptionsRequest());
            foreach (var item in response.DhcpOptions)
                this._model.AvailableDHCPOptions.Add(new DHCPOptionsWrapper(item));


            var control = new AssociateDHCPOptionSetControl(this);
            ToolkitFactory.Instance.ShellProvider.ShowModal(control);

            if (this._results == null)
                return new ActionResults().WithSuccess(false);

            return this._results;
        }

        public AssociateDHCPOptionSetModel Model => this._model;

        public void Commit()
        {
            string dhcpOptionId = null;
            if (this._model.IsNew)
            {
                var createRequest = new CreateDhcpOptionsRequest();
                createRequest.DhcpConfigurations = this._model.NewDHCPOptions.NativeDHCPOptions.DhcpConfigurations;
                var response = this._ec2Client.CreateDhcpOptions(createRequest);
                dhcpOptionId = response.DhcpOptions.DhcpOptionsId;
            }
            else
            {
                dhcpOptionId = this._model.SelectedDHCPOptions.NativeDHCPOptions.DhcpOptionsId;
            }

            this._ec2Client.AssociateDhcpOptions(new AssociateDhcpOptionsRequest() { VpcId = this._model.VPC.VpcId, DhcpOptionsId = dhcpOptionId });
            this._results = new ActionResults() { Success = true, FocalName = dhcpOptionId };
        }

        public void DeleteDHCPOptionSet(DHCPOptionsWrapper dhcpOptions)
        {
            var request = new DeleteDhcpOptionsRequest() { DhcpOptionsId = dhcpOptions.DhcpOptionsId };
            this._ec2Client.DeleteDhcpOptions(request);
            this._model.AvailableDHCPOptions.Remove(dhcpOptions);
        }
    }
}
