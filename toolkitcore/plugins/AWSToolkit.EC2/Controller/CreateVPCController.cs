using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;

using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.EC2.Util;


namespace Amazon.AWSToolkit.EC2.Controller
{
    public class CreateVPCController
    {
        ActionResults _results;
        CreateVPCModel _model;
        IAmazonEC2 _ec2Client;
        CreateVPCControl _control;

        public ActionResults Execute(IAmazonEC2 ec2Client)
        {
            this._ec2Client = ec2Client;
            this._model = new CreateVPCModel();
            this._model.AvailableZones = GetListOfAvailablityZones();
            this._model.KeyPairNames = GetListOfKeyPairNames();
            this._model.InstanceTypes = EC2ServiceMeta.Instance.ALL;

            this._control = new CreateVPCControl(this);
            ToolkitFactory.Instance.ShellProvider.ShowModal(this._control);

            if (this._results == null)
                return new ActionResults().WithSuccess(false);

            return this._results;
        }

        public CreateVPCModel Model
        {
            get { return this._model; }
        }

        IList<string> GetListOfAvailablityZones()
        {
            var zones = new List<string>();
            zones.Add(CreateVPCModel.NO_PREFERENCE_ZONE);

            var response = this._ec2Client.DescribeAvailabilityZones(new DescribeAvailabilityZonesRequest());
            foreach (var item in response.AvailabilityZones.OrderBy(x => x.ZoneName))
                zones.Add(item.ZoneName);

            return zones;
        }

        IList<string> GetListOfKeyPairNames()
        {
            var names = new List<string>();
                
            var response = this._ec2Client.DescribeKeyPairs(new DescribeKeyPairsRequest());
            foreach (var item in response.KeyPairs.OrderBy(x => x.KeyName))
                names.Add(item.KeyName);

            return names;
        }

        public void CreateVPC()
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(this.CreateVPCAsync));
        }

        void CreateVPCAsync(object state)
        {
            try
            {
                string vpcId;
                if (this._model.WithPrivateSubnet)
                {
                    var request = new LaunchVPCWithPublicAndPrivateSubnetsRequest()
                    {

                        VPCName = this._model.VPCName,
                        InstanceTenancy = this._model.InstanceTenancy,
                        VPCCidrBlock = this._model.CIDRBlock,
                        PublicSubnetCiderBlock = this._model.PublicSubnetCIDRBlock,
                        PublicSubnetAvailabilityZone = this._model.PublicSubnetAvailabilityZone == CreateVPCModel.NO_PREFERENCE_ZONE ? null : this._model.PublicSubnetAvailabilityZone,
                        PrivateSubnetCiderBlock = this._model.PrivateSubnetCIDRBlock,
                        PrivateSubnetAvailabilityZone = this._model.PrivateSubnetAvailabilityZone == CreateVPCModel.NO_PREFERENCE_ZONE ? null : this._model.PrivateSubnetAvailabilityZone,
                        InstanceType = this._model.NATInstanceType.Id,
                        KeyName = this._model.NATKeyPairName,
                        ConfigureDefaultVPCGroupForNAT = this._model.ConfigureDefaultVPCGroupForNAT,
                        ProgressCallback = this.progressCallback,
                        EnableDnsHostnames = this._model.EnableDNSHostnames,
                        EnableDnsSupport = this._model.EnableDNSSupport
                    };

                    ToolkitFactory.Instance.ShellProvider.OutputToHostConsole("Creating VPC with a public and private subnets", true);
                    vpcId = VPCUtilities.LaunchVPCWithPublicAndPrivateSubnets(this._ec2Client, request).VPC.VpcId;
                    ToolkitFactory.Instance.ShellProvider.OutputToHostConsole("Creation Complete!", true);
                }
                else if (this._model.WithPublicSubnet)
                {
                    var request = new LaunchVPCWithPublicSubnetRequest()
                    {
                        VPCName = this._model.VPCName,
                        InstanceTenancy = this._model.InstanceTenancy,
                        VPCCidrBlock = this._model.CIDRBlock,
                        PublicSubnetCiderBlock = this._model.PublicSubnetCIDRBlock,
                        PublicSubnetAvailabilityZone = this._model.PublicSubnetAvailabilityZone == CreateVPCModel.NO_PREFERENCE_ZONE ? null : this._model.PublicSubnetAvailabilityZone,
                        ProgressCallback = this.progressCallback,
                        EnableDnsHostnames = this._model.EnableDNSHostnames,
                        EnableDnsSupport = this._model.EnableDNSSupport
                    };

                    ToolkitFactory.Instance.ShellProvider.OutputToHostConsole("Creating VPC with a public subnet", true);
                    vpcId = VPCUtilities.LaunchVPCWithPublicSubnet(this._ec2Client, request).VPC.VpcId;
                    ToolkitFactory.Instance.ShellProvider.OutputToHostConsole("Creation Complete!", true);
                }
                else
                {
                    var request = new CreateVpcRequest()
                    {
                        CidrBlock = this._model.CIDRBlock,
                        InstanceTenancy = this._model.InstanceTenancy
                    };

                    vpcId = this._ec2Client.CreateVpc(request).Vpc.VpcId;

                    if (!string.IsNullOrEmpty(this._model.VPCName))
                    {
                        this._ec2Client.CreateTags(new CreateTagsRequest()
                        {
                            Resources = new List<string>() { vpcId },
                            Tags = new List<Tag>() { new Tag() { Key = "Name", Value = this._model.VPCName } }
                        });
                    }
                }

                this._results = new ActionResults()
                    .WithFocalname(vpcId)
                    .WithSuccess(true);

                if (this._control != null)
                    this._control.CreateAsyncComplete(true);
            }
            catch (Exception e)
            {
                // If there is no result then we failed at the initial VPC creation and the view is still active.
                if (this._results == null && this._control != null)
                {
                    this._control.CreateAsyncComplete(false);
                }
                else
                {
                    progressCallback("Error creating VPC: " + e.Message);
                }

                ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke((Action)(() =>
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Create Fail", "Error creating VPC: " + e.Message);
                }));
            }
        }

        void progressCallback(string message)
        {
            if (this._results == null && message.Contains("vpc-"))
            {
                int startPos = message.IndexOf("vpc-");
                var vpcId = message.Substring(startPos, 12);

                this._results = new ActionResults()
                    .WithFocalname(vpcId)
                    .WithSuccess(true);

                if (string.IsNullOrEmpty(this.Model.VPCName))
                {
                    this._control.CreateAsyncComplete(true);
                    this._control = null;
                }
            }
            // If there was a name for the VPC then wait till the second progress message so that name will be applied by then.
            // Otherwise the VPC view will refresh before the name applies.
            else if (this._results != null && this._control != null)
            {
                this._control.CreateAsyncComplete(true);
                this._control = null;
            }

            ToolkitFactory.Instance.ShellProvider.OutputToHostConsole("..." + message, true);
        }
    }
}
