using System;

using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.IdentityManagement.Nodes;
using Amazon.AWSToolkit.IdentityManagement.Util;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.IdentityManagement.Model;

namespace Amazon.AWSToolkit.IdentityManagement.Controller
{
    public class DeleteRoleController : BaseContextCommand
    {
        private readonly ToolkitContext _toolkitContext;

        public DeleteRoleController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public override ActionResults Execute(IViewModel model)
        {
            var result = DeleteRole(model);
            RecordMetric(result, model);
            return result;
        }

        public ActionResults DeleteRole(IViewModel model)
        {
            var roleModel = model as IAMRoleViewModel;
            if (roleModel == null)
            {
                return ActionResults.CreateFailed(new ToolkitException("Unable to find IAM role data",
                            ToolkitException.CommonErrorCode.InternalMissingServiceState));
            }

            var msg = $"Are you sure you want to delete the {model.Name} role and all of its policies?";

            if (!_toolkitContext.ToolkitHost.Confirm("Delete Role", msg))
            {
                return ActionResults.CreateCancelled();
            }

            try
            {
                var policies = roleModel.IAMClient.ListRolePolicies(new ListRolePoliciesRequest { RoleName = model.Name }).PolicyNames;
                foreach (var policy in policies)
                {
                    roleModel.IAMClient.DeleteRolePolicy(new DeleteRolePolicyRequest
                    {
                        RoleName = model.Name,
                        PolicyName = policy
                    });
                }

                InstanceProfile instanceProfile = null;
                try
                {
                    instanceProfile = roleModel.IAMClient.GetInstanceProfile(new GetInstanceProfileRequest() { InstanceProfileName = model.Name }).InstanceProfile;
                }
                catch (NoSuchEntityException) { }

                // See if there is a instance profile with a 1 to 1 mapping to the role and if so delete that as well.
                if (instanceProfile != null)
                {
                    if (instanceProfile.Roles.Count == 1 && instanceProfile.Roles[0].RoleName == model.Name)
                    {
                        roleModel.IAMClient.RemoveRoleFromInstanceProfile(new RemoveRoleFromInstanceProfileRequest()
                        {
                            InstanceProfileName = model.Name,
                            RoleName = model.Name
                        });

                        roleModel.IAMClient.DeleteInstanceProfile(new DeleteInstanceProfileRequest() { InstanceProfileName = model.Name });
                    }
                }

                var attachedPolicies = roleModel.IAMClient.ListAttachedRolePolicies(new ListAttachedRolePoliciesRequest { RoleName = model.Name }).AttachedPolicies;
                foreach (var policy in attachedPolicies)
                {
                    roleModel.IAMClient.DetachRolePolicy(new DetachRolePolicyRequest
                    {
                        RoleName = model.Name,
                        PolicyArn = policy.PolicyArn
                    });
                }

                var request = new DeleteRoleRequest() { RoleName = roleModel.Role.RoleName };
                roleModel.IAMClient.DeleteRole(request);

                return new ActionResults()
                    .WithSuccess(true)
                    .WithFocalname(model.Name)
                    .WithShouldRefresh(true);
            }
            catch (Exception e)
            {
                _toolkitContext.ToolkitHost.ShowError($"Error deleting role:{Environment.NewLine}{e.Message}");
                return ActionResults.CreateFailed(e);
            }
        }


        public void RecordMetric(ActionResults results, IViewModel model)
        {
            var groupModel = model as IAMRoleViewModel;
            var awsConnectionSettings = groupModel?.IAMRoleRootViewModel?.IAMRootViewModel?.AwsConnectionSettings;
            _toolkitContext.RecordIamDelete(IamResourceType.Role, results,
                 awsConnectionSettings);
        }
    }
}
