using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.RDS;
using Amazon.RDS.Model;

using Amazon.EC2;
using Amazon.EC2.Model;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.RDS.Model;
using Amazon.AWSToolkit.RDS.View;
using Amazon.AWSToolkit.RDS.Nodes;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.RDS.Controller
{
    public class PromptAddCurrentCIDRController
    {
        IAmazonRDS _rdsClient;
        IAmazonEC2 _ec2Client;
        DBInstanceWrapper _dbInstance;
        ActionResults _results;

        public ActionResults Execute(RDSInstanceViewModel rdsInstanceViewModel)
        {
            this._dbInstance = rdsInstanceViewModel.DBInstance;
            if (this._dbInstance.NativeInstance.DBSecurityGroups.Count == 0 && this._dbInstance.NativeInstance.VpcSecurityGroups.Count == 0)
                return new ActionResults().WithSuccess(false);

            this._rdsClient = rdsInstanceViewModel.RDSClient;

            string region = rdsInstanceViewModel.InstanceRootViewModel.CurrentEndPoint.RegionSystemName;
            RegionEndPointsManager.RegionEndPoints endPoints = RegionEndPointsManager.GetInstance().GetRegion(region);

            var ec2Config = new AmazonEC2Config { ServiceURL = endPoints.GetEndpoint(RegionEndPointsManager.EC2_SERVICE_NAME).Url };
            this._ec2Client = new AmazonEC2Client(rdsInstanceViewModel.AccountViewModel.Credentials, ec2Config);


            var control = new PromptAddCurrentCIDRControl(this);
            if (!ToolkitFactory.Instance.ShellProvider.ShowModal(control, System.Windows.MessageBoxButton.OKCancel))
                return new ActionResults().WithSuccess(false);

            if(_results == null)
                return new ActionResults().WithSuccess(false);


            return _results;
        }

        public void AddPermission(string cidr)
        {

            if (this._dbInstance.NativeInstance.DBSecurityGroups.Count > 0)
            {
                var request = new AuthorizeDBSecurityGroupIngressRequest();
                request.CIDRIP = cidr;
                request.DBSecurityGroupName = this._dbInstance.NativeInstance.DBSecurityGroups[0].DBSecurityGroupName;

                try
                {
                    this._rdsClient.AuthorizeDBSecurityGroupIngress(request);
                    this._results = new ActionResults().WithSuccess(true);
                }
                catch (AuthorizationAlreadyExistsException)
                {
                    throw new ApplicationException("The CIDR already exists for security group " + request.DBSecurityGroupName + ".  Check that port used by this instance is not blocked by your local firewall.");
                }
            }
            else
            {
                try
                {
                    var request = new AuthorizeSecurityGroupIngressRequest
                    {
                        GroupId = this._dbInstance.NativeInstance.VpcSecurityGroups[0].VpcSecurityGroupId
                    };

                    var ipPermission = new IpPermission
                    {
                        FromPort = this._dbInstance.Port.Value,
                        ToPort = this._dbInstance.Port.Value,
                        IpProtocol = "tcp"
                    };
                    ipPermission.Ipv4Ranges.Add(new IpRange { CidrIp = cidr });

                    request.IpPermissions.Add(ipPermission);
                    this._ec2Client.AuthorizeSecurityGroupIngress(request);
                    this._results = new ActionResults().WithSuccess(true);
                }
                catch (AmazonEC2Exception e)
                {
                    throw new ApplicationException("Failed to add security permssion to EC2 security group: " + e.Message);
                }
            }
        }
    }
}
