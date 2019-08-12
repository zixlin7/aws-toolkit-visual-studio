using System;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.IdentityManagement.Nodes;
using Amazon.IdentityManagement.Model;

namespace Amazon.AWSToolkit.IdentityManagement.Controller
{
    public class DeleteRoleController : BaseContextCommand
    {
        public override ActionResults Execute(IViewModel model)
        {
            var roleModel = model as IAMRoleViewModel;
            if (roleModel == null)
                return new ActionResults().WithSuccess(false);

            string msg = string.Format("Are you sure you want to delete the {0} role and all of its policies?", model.Name);
            if (ToolkitFactory.Instance.ShellProvider.Confirm("Delete Role", msg))
            {
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
                }
                catch (Exception e)
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Error deleting role: " + e.Message);
                    return new ActionResults().WithSuccess(false);
                }
                return new ActionResults()
                    .WithSuccess(true)
                    .WithFocalname(model.Name)
                    .WithShouldRefresh(true);
            }

            return new ActionResults().WithSuccess(false);
        }
    }
}
