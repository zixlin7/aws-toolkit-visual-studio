using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class ViewSecurityGroupsController : FeatureController<ViewSecurityGroupsModel>, IIPPermissionController
    {
        ViewSecurityGroupsControl _control;
        IPPermissionController _ingressPermissionController;
        IPPermissionController _egressPermissionController;

        protected override void DisplayView()
        {
            this._control = new ViewSecurityGroupsControl(this);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);
        }

        public void LoadModel()
        {
            RefreshSecurityGroups();
        }

        public void RefreshSecurityGroups()
        {
            var response = this.EC2Client.DescribeSecurityGroups(new DescribeSecurityGroupsRequest());            

            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
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

        public void DeleteSecurityGroups(IList<SecurityGroupWrapper> groups)
        {
            var controller = new DeleteSecurityGroupController();
            controller.Execute(this.EC2Client, groups);
            this.RefreshSecurityGroups();
        }

        public void RefreshPermission(EC2Constants.PermissionType permisionType)
        {
            if (this._ingressPermissionController != null && permisionType == EC2Constants.PermissionType.Ingress)
                this._ingressPermissionController.RefreshPermission(permisionType);

            if (this._egressPermissionController != null && permisionType == EC2Constants.PermissionType.Egrees)
                this._egressPermissionController.RefreshPermission(permisionType);
        }

        public void AddPermission(EC2Constants.PermissionType permisionType)
        {
            if (this._ingressPermissionController != null && permisionType == EC2Constants.PermissionType.Ingress)
                this._ingressPermissionController.AddPermission(permisionType);

            if (this._egressPermissionController != null && permisionType == EC2Constants.PermissionType.Egrees)
                this._egressPermissionController.AddPermission(permisionType);
        }

        public void DeletePermission(IList<IPPermissionWrapper> toBeDeleted, EC2Constants.PermissionType permisionType)
        {
            if (this._ingressPermissionController != null && permisionType == EC2Constants.PermissionType.Ingress)
                this._ingressPermissionController.DeletePermission(toBeDeleted, permisionType);

            if (this._egressPermissionController != null && permisionType == EC2Constants.PermissionType.Egrees)
                this._egressPermissionController.DeletePermission(toBeDeleted, permisionType);
        }

        public SecurityGroupWrapper CreateSecurityGroup()
        {
            var controller = new CreateSecurityGroupController();
            var results = controller.Execute(this.EC2Client);
            if (results.Success)
            {
                this.RefreshSecurityGroups();

                foreach (var group in this.Model.SecurityGroups)
                {
                    if (group.NativeSecurityGroup.GroupId == results.FocalName)
                    {
                        return group;
                    }
                }
            }

            return null;
        }
    }
}
