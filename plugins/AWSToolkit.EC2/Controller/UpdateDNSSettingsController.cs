using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.EC2.View;
using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class UpdateDNSSettingsController
    {
        ActionResults _results;
        string _vpcId;
        IAmazonEC2 _ec2Client;
        UpdateDNSSettingsControl _control;
        UpdateDNSSettingsModel _model;

        public ActionResults Execute(IAmazonEC2 ec2Client, VPCWrapper vpc)
        {
            try
            {
                this._ec2Client = ec2Client;
                this._vpcId = vpc.VpcId;
                this._model = new UpdateDNSSettingsModel
                {
                    VpcId = this._vpcId,
                    EnableDnsHostnames = QueryDNSHostnames(),
                    EnableDnsSupport = QueryDNSSupport()
                };

                this._control = new UpdateDNSSettingsControl(this);
                ToolkitFactory.Instance.ShellProvider.ShowModal(this._control);

                if (this._results == null)
                    return new ActionResults().WithSuccess(false);

            }
            catch (Exception e)
            {
            }

            return this._results;
        }

        public UpdateDNSSettingsModel Model
        {
            get { return this._model; }
        }

        public bool UpdateVPCAttributes()
        {
            // attributes must be set one at a time
            _ec2Client.ModifyVpcAttribute(new ModifyVpcAttributeRequest
            {
                VpcId = _vpcId,
                EnableDnsHostnames = _model.EnableDnsHostnames
            });

            _ec2Client.ModifyVpcAttribute(new ModifyVpcAttributeRequest
            {
                VpcId = _vpcId,
                EnableDnsSupport = _model.EnableDnsSupport
            });

            this._results = new ActionResults().WithSuccess(true);
            return true;
        }

        bool QueryDNSHostnames()
        {
            return this._ec2Client.DescribeVpcAttribute(new DescribeVpcAttributeRequest
            {
                Attribute = VpcAttributeName.EnableDnsHostnames,
                VpcId = this._vpcId
            }).EnableDnsHostnames;
        }

        bool QueryDNSSupport()
        {
            return this._ec2Client.DescribeVpcAttribute(new DescribeVpcAttributeRequest
            {
                Attribute = VpcAttributeName.EnableDnsSupport,
                VpcId = this._vpcId
            }).EnableDnsSupport;
        }
    }
}
