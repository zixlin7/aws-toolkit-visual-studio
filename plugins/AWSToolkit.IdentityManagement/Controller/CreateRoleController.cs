using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.IdentityManagement.View;
using Amazon.AWSToolkit.IdentityManagement.Model;
using Amazon.AWSToolkit.IdentityManagement.Nodes;

using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;

namespace Amazon.AWSToolkit.IdentityManagement.Controller
{
    public class CreateRoleController : BaseContextCommand
    {
        CreateRoleControl _control;
        CreateRoleModel _model;
        IAMRoleRootViewModel _rootModel;
        ActionResults _results;

        public override ActionResults Execute(IViewModel model)
        {
            this._rootModel = model as IAMRoleRootViewModel;
            if (this._rootModel == null)
                return new ActionResults().WithSuccess(false);

            this._model = new CreateRoleModel();
            this._control = new CreateRoleControl(this);
            ToolkitFactory.Instance.ShellProvider.ShowModal(this._control);

            if (this._results == null)
                return new ActionResults().WithSuccess(false);
            return this._results;
        }

        public CreateRoleModel Model
        {
            get { return this._model; }
        }

        public void Persist()
        {
            var roleRequest = new CreateRoleRequest()
            {
                RoleName = this.Model.RoleName.Trim(),
                AssumeRolePolicyDocument = Amazon.AWSToolkit.Constants.IAM_ROLE_EC2_ASSUME_ROLE_POLICY_DOCUMENT
            };
            var createResponse = this._rootModel.IAMClient.CreateRole(roleRequest);

            InstanceProfile instanceProfile = null;
            try
            {
                instanceProfile = this._rootModel.IAMClient.GetInstanceProfile(new GetInstanceProfileRequest() { InstanceProfileName = roleRequest.RoleName }).InstanceProfile;
            }
            catch (NoSuchEntityException) { }

            // Check to see if an instance profile exists for this role and if not create it.
            if (instanceProfile == null)
            {
                var profileRequest = new CreateInstanceProfileRequest() { InstanceProfileName = roleRequest.RoleName };
                this._rootModel.IAMClient.CreateInstanceProfile(profileRequest);

                var addRoleRequest = new AddRoleToInstanceProfileRequest() { RoleName = roleRequest.RoleName, InstanceProfileName = roleRequest.RoleName };
                this._rootModel.IAMClient.AddRoleToInstanceProfile(addRoleRequest);
            }
            // If it already exists see if this role is already assigned and if not assign it.
            else
            {
                if (instanceProfile.Roles.FirstOrDefault(x => x.RoleName == roleRequest.RoleName) == null)
                {
                    var addRoleRequest = new AddRoleToInstanceProfileRequest()
                    {
                        RoleName = roleRequest.RoleName,
                        InstanceProfileName = roleRequest.RoleName
                    };
                    this._rootModel.IAMClient.AddRoleToInstanceProfile(addRoleRequest);
                }
            }

            this._results = new ActionResults()
                .WithSuccess(true)
                .WithParameter(IAMActionResultsConstants.PARAM_IAM_ROLE, createResponse.Role);
        }
    }
}
