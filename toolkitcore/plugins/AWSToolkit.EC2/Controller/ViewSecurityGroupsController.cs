using System;
using System.Collections.Generic;
using System.Linq;

using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;
using Amazon.AWSToolkit.EC2.View.DataGrid;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Navigator;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class ViewSecurityGroupsController : FeatureController<ViewSecurityGroupsModel>, IIPPermissionController
    {
        private readonly ToolkitContext _toolkitContext;

        ViewSecurityGroupsControl _control;
        IPPermissionController _ingressPermissionController;
        IPPermissionController _egressPermissionController;

        public ViewSecurityGroupsController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        protected override void DisplayView()
        {
            this._control = new ViewSecurityGroupsControl(this);
            _toolkitContext.ToolkitHost.OpenInEditor(this._control);
        }

        public void LoadModel()
        {
            RefreshSecurityGroups();
        }

        public void RefreshSecurityGroups()
        {
            var response = this.EC2Client.DescribeSecurityGroups(new DescribeSecurityGroupsRequest());

            _toolkitContext.ToolkitHost.ExecuteOnUIThread((Action)(() =>
            {
                this.Model.SecurityGroups.Clear();
                foreach (var group in response.SecurityGroups.OrderBy(x => x.GroupName.ToLower()))
                {
                    this.Model.SecurityGroups.Add(new SecurityGroupWrapper(group));
                }
            }));
        }

        public void ResetSelection(IList<SecurityGroupWrapper> groups)
        {
            this.Model.SelectedGroups.Clear();
            foreach (var group in groups)
            {
                this.Model.SelectedGroups.Add(group);
            }

            if (this.Model.SelectedGroups.Count == 0)
            {
                this._ingressPermissionController = null;
                this._egressPermissionController = null;
            }
            else
            {
                this._ingressPermissionController = new IPPermissionController(this.FeatureViewModel, this.Model.SelectedGroups[0]);
                this._egressPermissionController = new IPPermissionController(this.FeatureViewModel, this.Model.SelectedGroups[0]);
            }
        }

        public ActionResults DeleteSecurityGroups(IList<SecurityGroupWrapper> groups)
        {
            var controller = new DeleteSecurityGroupController();
            var result = controller.Execute(EC2Client, groups);
            RefreshSecurityGroups();

            return result;
        }

        public void RefreshPermission(EC2Constants.PermissionType permisionType)
        {
            if (this._ingressPermissionController != null && permisionType == EC2Constants.PermissionType.Ingress)
                this._ingressPermissionController.RefreshPermission(permisionType);

            if (this._egressPermissionController != null && permisionType == EC2Constants.PermissionType.Egrees)
                this._egressPermissionController.RefreshPermission(permisionType);
        }

        public ActionResults AddPermission(EC2Constants.PermissionType permisionType)
        {
            if (_ingressPermissionController != null && permisionType == EC2Constants.PermissionType.Ingress)
            {
                return _ingressPermissionController.AddPermission(permisionType);
            }

            if (_egressPermissionController != null && permisionType == EC2Constants.PermissionType.Egrees)
            {
                return _egressPermissionController.AddPermission(permisionType);
            }

            return ActionResults.CreateFailed(new ToolkitException("Add permission was called with an unhandled state", ToolkitException.CommonErrorCode.UnsupportedState));
        }

        public ActionResults DeletePermission(IList<IPPermissionWrapper> toBeDeleted, EC2Constants.PermissionType permisionType)
        {
            if (_ingressPermissionController != null && permisionType == EC2Constants.PermissionType.Ingress)
            {
                return _ingressPermissionController.DeletePermission(toBeDeleted, permisionType);
            }

            if (_egressPermissionController != null && permisionType == EC2Constants.PermissionType.Egrees)
            {
                return _egressPermissionController.DeletePermission(toBeDeleted, permisionType);
            }

            return ActionResults.CreateFailed(new ToolkitException("Delete permission was called with an unhandled state", ToolkitException.CommonErrorCode.UnsupportedState));
        }

        public ActionResults CreateSecurityGroup(ICustomizeColumnGrid grid)
        {
            var controller = new CreateSecurityGroupController();
            var results = controller.Execute(EC2Client);

            if (results.Success)
            {
                RefreshSecurityGroups();

                var createdGroup = Model.SecurityGroups.FirstOrDefault(sg =>
                    sg.NativeSecurityGroup.GroupId == results.FocalName);

                if (createdGroup != null)
                {
                    grid.SelectAndScrollIntoView(createdGroup);
                }
            }

            return results;
        }

        public void RecordCreateSecurityGroup(ActionResults result)
        {
            var data = CreateMetricData<Ec2CreateSecurityGroup>(result, _toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();
            _toolkitContext.TelemetryLogger.RecordEc2CreateSecurityGroup(data);
        }

        public void RecordDeleteSecurityGroup(int count, ActionResults result)
        {
            var data = CreateMetricData<Ec2DeleteSecurityGroup>(result, _toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();
            data.Value = count;
            _toolkitContext.TelemetryLogger.RecordEc2DeleteSecurityGroup(data);
        }

        public void RecordEditSecurityGroupPermission(ActionResults result)
        {
            var data = CreateMetricData<Ec2EditSecurityGroupPermission>(result, _toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();
            _toolkitContext.TelemetryLogger.RecordEc2EditSecurityGroupPermission(data);
        }
    }
}
