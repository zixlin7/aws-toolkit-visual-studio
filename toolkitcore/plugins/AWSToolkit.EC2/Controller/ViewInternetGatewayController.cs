using System;
using System.Linq;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class ViewInternetGatewayController : FeatureController<ViewInternetGatewayModel>
    {
        ViewInternetGatewaysControl _control;

        protected override void DisplayView()
        {
            this._control = new ViewInternetGatewaysControl(this);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);
        }

        public void LoadModel()
        {
            RefreshInternetGateways();
        }


        public void RefreshInternetGateways()
        {
            var response = this.EC2Client.DescribeInternetGateways(new DescribeInternetGatewaysRequest());
            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
            {
                this.Model.Gateways.Clear();
                foreach (var gateway in response.InternetGateways.OrderBy(x => x.InternetGatewayId))
                {
                    this.Model.Gateways.Add(new InternetGatewayWrapper(gateway));
                }
            }));
        }

        public InternetGatewayWrapper CreateInternetGateway()
        {
            var response = this.EC2Client.CreateInternetGateway(new CreateInternetGatewayRequest());
            var gateway = new InternetGatewayWrapper(response.InternetGateway);
            this.Model.Gateways.Add(gateway);
            return gateway;
        }

        public void DeleteInternetGateway(InternetGatewayWrapper gateway)
        {
            var request = new DeleteInternetGatewayRequest { InternetGatewayId = gateway.InternetGatewayId };
            this.EC2Client.DeleteInternetGateway(request);
            this.Model.Gateways.Remove(gateway);
        }

        public void AttachToVPC(InternetGatewayWrapper gateway)
        {
            var controller = new AttachToVPCController();
            var results = controller.Execute(this.EC2Client, gateway.InternetGatewayId);
            if (results.Success)
                this.RefreshInternetGateways();
        }

        public void DetachToVPC(InternetGatewayWrapper gateway)
        {
            var request = new DetachInternetGatewayRequest() { InternetGatewayId = gateway.InternetGatewayId, VpcId = gateway.VpcId };
            this.EC2Client.DetachInternetGateway(request);

            gateway.ClearAttachments();
            this.Model.Gateways.Remove(gateway);
            this.Model.Gateways.Add(gateway);
        }
    }
}
