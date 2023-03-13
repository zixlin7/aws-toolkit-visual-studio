using System;
using System.Collections.Generic;
using System.Linq;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.RDS.Model;
using Amazon.AWSToolkit.RDS.Nodes;
using Amazon.AWSToolkit.RDS.Util;
using Amazon.AWSToolkit.RDS.View;
using Amazon.EC2;
using Amazon.RDS;
using Amazon.RDS.Model;
using log4net;

namespace Amazon.AWSToolkit.RDS.Controller
{
    public class CreateDBSubnetGroupController : BaseContextCommand
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(CreateDBSubnetGroupController));
        private readonly ToolkitContext _toolkitContext;

        ActionResults _results;
        IAmazonRDS _rdsClient;
        IAmazonEC2 _ec2Client;
        CreateDBSubnetGroupModel _model;
        RDSSubnetGroupsRootViewModel _subnetGroupsRootViewModel;

        public CreateDBSubnetGroupController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public ToolkitContext ToolkitContext => _toolkitContext;

        public CreateDBSubnetGroupModel Model => _model;

        public override ActionResults Execute(IViewModel model)
        {
            var result = CreateDBSubnetGroup(model);
            RecordMetric(result);
            return result;
        }

        private ActionResults CreateDBSubnetGroup(IViewModel model)
        {
            var subnetGroupsRootViewModel = model as RDSSubnetGroupsRootViewModel;
            if (subnetGroupsRootViewModel == null)
            {
                return ActionResults.CreateFailed(new ToolkitException("Unable to find RDS Subnet group data",
                    ToolkitException.CommonErrorCode.InternalMissingServiceState));
            }

            return Execute(subnetGroupsRootViewModel);
        }

        public ActionResults Execute(RDSSubnetGroupsRootViewModel subnetGroupsRootViewModel)
        {
            try
            {
                _subnetGroupsRootViewModel = subnetGroupsRootViewModel;
                _rdsClient = _subnetGroupsRootViewModel.RDSClient;

                var account = _subnetGroupsRootViewModel.AccountViewModel;
                _ec2Client = account.CreateServiceClient<AmazonEC2Client>(_subnetGroupsRootViewModel.Region);

                _model = new CreateDBSubnetGroupModel();

                _model.LoadVPCList(_ec2Client.DescribeVpcs().Vpcs);
                _model.LoadAvailabilityZones(_ec2Client.DescribeAvailabilityZones().AvailabilityZones);
                _model.LoadAllSubnets(_ec2Client.DescribeSubnets().Subnets);

                var control = new CreateDBSubnetGroupControl(this);
                if (!_toolkitContext.ToolkitHost.ShowModal(control))
                {
                    return ActionResults.CreateCancelled();
                }

                return _results ?? ActionResults.CreateFailed();
            }
            catch (Exception e)
            {
                _logger.ErrorFormat("Caught exception launching create dbsubnet dialog: {0}", e.Message);
                return ActionResults.CreateFailed(e);
            }
        }

        internal void LoadSubnetsForSelectedVPCAndZone()
        {
            // possibly triggered by us clearing members as other selections are made
            if (_model.SelectedVPC == null || _model.SelectedZone == null)
                return;

            LoadSubnetsForSelectedVPCAndZone(_model.SelectedVPC.VpcId, _model.SelectedZone.ZoneName);    
        }

        // Filter all available subnets down to those belonging to the specified vpc
        // and in the specified zone that have not been already selected into the pending group
        internal void LoadSubnetsForSelectedVPCAndZone(string vpcId, string availabilityZone)
        {
            var allSubnets = _model.AllSubnets;
            var usedSubnets = new HashSet<string>();
            foreach (var usedSubnet in _model.AssignedSubnets)
            {
                usedSubnets.Add(usedSubnet.SubnetId);
            }

            var availableSubnets 
                = allSubnets.Where(subnet => !usedSubnets.Contains(subnet.SubnetId))
                            .Where(subnet => subnet.VpcId.Equals(vpcId, StringComparison.OrdinalIgnoreCase) 
                                    && subnet.AvailabilityZone.Equals(availabilityZone, StringComparison.OrdinalIgnoreCase))
                            .ToList();

            _model.SubnetsForVPCZone = availableSubnets;
        }

        internal void AddSelectedZoneSubnet()
        {
            var assignedSubnet = new AssignedSubnet
            {
                AvailabilityZone = _model.SelectedZone.ZoneName,
                SubnetId = _model.SelectedSubnet.SubnetId,
                CidrBlock = _model.SelectedSubnet.CidrBlock
            };

            _model.AddAssignedSubnet(assignedSubnet);
        }

        internal void RemoveAssignedSubnet(string subnetId)
        {
            var assignedSubnets = _model.AssignedSubnets;
            foreach (var subnet in assignedSubnets)
            {
                if (subnet.SubnetId.Equals(subnetId, StringComparison.OrdinalIgnoreCase))
                {
                    _model.RemoveAssignedSubnet(subnet);
                    // reload the available subnets so what we just released gets to be re-used
                    // if needed
                    LoadSubnetsForSelectedVPCAndZone();
                    break;
                }
            }
        }

        internal void AddAllAvailableZonesAndSubnets()
        {
            var assignedSubnets = new List<AssignedSubnet>();

            var allSubnets = _model.AllSubnets;
            foreach (var subnet in allSubnets)
            {
                if (subnet.VpcId.Equals(_model.SelectedVPC.VpcId, StringComparison.OrdinalIgnoreCase))
                {
                    var assignedSubnet = new AssignedSubnet
                    {
                        AvailabilityZone = subnet.AvailabilityZone,
                        SubnetId = subnet.SubnetId,
                        CidrBlock = subnet.CidrBlock
                    };

                    assignedSubnets.Add(assignedSubnet);
                }
            }

            _model.SetAssignedSubnets(assignedSubnets);

            _model.SubnetsForVPCZone = null;
        }

        public string CreateDBSubnetGroup()
        {
            var request = new CreateDBSubnetGroupRequest
            {
                DBSubnetGroupName = _model.Name,
                DBSubnetGroupDescription = _model.Description,
                SubnetIds = _model.AssignedSubnets.Select(subnet => subnet.SubnetId).ToList()
            };

            var response = _rdsClient.CreateDBSubnetGroup(request);

            if (this._subnetGroupsRootViewModel != null)
            {
                this._subnetGroupsRootViewModel.AddDBSubnetGroup(response.DBSubnetGroup);
            }

            this._results = new ActionResults().WithFocalname(this._model.Name).WithSuccess(true);
            return _model.Name;
        }

        public void RecordMetric(ActionResults results)
        {
            var awsConnectionSettings = _subnetGroupsRootViewModel?.RDSRootViewModel?.AwsConnectionSettings;
            _toolkitContext.RecordRdsCreateSubnetGroup(results, awsConnectionSettings);
        }
    }
}
