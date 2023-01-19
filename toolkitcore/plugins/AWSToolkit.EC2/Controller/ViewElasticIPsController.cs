using System;
using System.Diagnostics;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.EC2.Commands;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.Repositories;
using Amazon.AWSToolkit.EC2.View;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class ViewElasticIPsController : FeatureController<ViewElasticIPsModel>
    {
        private readonly ToolkitContext _toolkitContext;
        private ViewElasticIPsControl _control;

        public ViewElasticIPsController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        protected override void DisplayView()
        {
            if (!(_toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(IEc2RepositoryFactory)) is
                    IEc2RepositoryFactory factory))
            {
                Debug.Assert(!Debugger.IsAttached, $"Plugin factory {nameof(IEc2RepositoryFactory)} is missing. The Toolkit is unable to perform EC2 Elastic IP operations.");
                throw new NotSupportedException("AWS Toolkit was unable to get details about EC2 Elastic IPs");
            }

            var ip = factory.CreateElasticIpRepository(AwsConnectionSettings);

            var handlerState = new ElasticIpCommandState(Model, ip, AwsConnectionSettings, _toolkitContext);

            Model.AllocateElasticIp = CreatePromptAndExecuteCommand(new AllocateElasticIpCommand(handlerState));
            Model.ReleaseElasticIp = CreatePromptAndExecuteCommand(new ReleaseElasticIpCommand(handlerState));
            Model.AssociateElasticIp = CreatePromptAndExecuteCommand(new AssociateElasticIpCommand(handlerState));
            Model.DisassociateElasticIp = CreatePromptAndExecuteCommand(new DisassociateElasticIpCommand(handlerState));

            _control = new ViewElasticIPsControl(this);
            _toolkitContext.ToolkitHost.OpenInEditor(_control);
        }

        private PromptAndExecuteCommand<ElasticIpCommandArgs> CreatePromptAndExecuteCommand(PromptAndExecuteHandler<ElasticIpCommandArgs> handler)
        {
            return new PromptAndExecuteCommand<ElasticIpCommandArgs>(handler, _toolkitContext);
        }

        public void LoadModel()
        {
            RefreshElasticIPs();
        }


        public void RefreshElasticIPs()
        {
            var response = this.EC2Client.DescribeAddresses(new DescribeAddressesRequest());

            _toolkitContext.ToolkitHost.ExecuteOnUIThread((Action)(() =>
            {
                this.Model.Addresses.Clear();
                foreach (var item in response.Addresses)
                {
                    this.Model.Addresses.Add(new AddressWrapper(item));
                }
            }));
        }
    }
}
