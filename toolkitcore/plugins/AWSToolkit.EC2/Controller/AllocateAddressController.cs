using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.EC2.View;
using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class AllocateAddressController
    {
        ActionResults _results;
        IAmazonEC2 _ec2Client;

        public ActionResults Execute(IAmazonEC2 ec2Client)
        {
            this._ec2Client = ec2Client;

            var control = new AllocateAddressControl(this);
            ToolkitFactory.Instance.ShellProvider.ShowModal(control);
            if (this._results != null)
                return this._results;

            return new ActionResults().WithSuccess(false);
        }

        public void AllocateAddress(string domain)
        {
            var request = new AllocateAddressRequest() { Domain = domain };
            var response = this._ec2Client.AllocateAddress(request);

            this._results = new ActionResults()
                .WithFocalname(response.PublicIp)
                .WithSuccess(true);
        }
    }
}
