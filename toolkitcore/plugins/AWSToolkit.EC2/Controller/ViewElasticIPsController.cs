﻿using System;

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
            var ip = _toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(IElasticIpRepository)) as
                IElasticIpRepository;
            Model.AllocateElasticIp = new AllocateElasticIpCommand(Model, ip, AwsConnectionSettings, _toolkitContext);
            Model.ReleaseElasticIp = new ReleaseElasticIpCommand(Model, ip, AwsConnectionSettings, _toolkitContext);
            Model.AssociateElasticIp = new AssociateElasticIpCommand(Model, ip, AwsConnectionSettings, _toolkitContext);
            Model.DisassociateElasticIp = new DisassociateElasticIpCommand(Model, ip, AwsConnectionSettings, _toolkitContext);

            this._control = new ViewElasticIPsControl(this);
            _toolkitContext.ToolkitHost.OpenInEditor(this._control);
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
