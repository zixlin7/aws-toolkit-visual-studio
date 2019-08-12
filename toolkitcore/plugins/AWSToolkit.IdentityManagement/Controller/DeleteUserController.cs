using System;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.IdentityManagement.Nodes;
using Amazon.IdentityManagement.Model;

namespace Amazon.AWSToolkit.IdentityManagement.Controller
{
    public class DeleteUserController : BaseContextCommand
    {
        public override ActionResults Execute(IViewModel model)
        {
            IAMUserViewModel userModel = model as IAMUserViewModel;
            if (userModel == null)
                return new ActionResults().WithSuccess(false);

            string msg = string.Format("Are you sure you want to delete the {0} user and all of its policies?", model.Name);
            if (ToolkitFactory.Instance.ShellProvider.Confirm("Delete User", msg))
            {
                try
                {
                    var policies = userModel.IAMClient.ListUserPolicies(new ListUserPoliciesRequest { UserName = model.Name }).PolicyNames;
                    foreach (var policy in policies)
                    {
                        userModel.IAMClient.DeleteUserPolicy(new DeleteUserPolicyRequest
                        {
                            UserName = model.Name,
                            PolicyName = policy
                        });
                    }

                    var groups = userModel.IAMClient.ListGroupsForUser(new ListGroupsForUserRequest { UserName = model.Name }).Groups;
                    foreach(var group in groups)
                    {
                        userModel.IAMClient.RemoveUserFromGroup(new RemoveUserFromGroupRequest
                            {
                                GroupName = group.GroupName,
                                UserName = model.Name
                            });
                    }

                    var accessKeys = userModel.IAMClient.ListAccessKeys(new ListAccessKeysRequest { UserName = model.Name }).AccessKeyMetadata;
                    foreach(var accessKey in accessKeys)
                    {
                        userModel.IAMClient.DeleteAccessKey(new DeleteAccessKeyRequest
                            {
                                UserName = model.Name,
                                AccessKeyId = accessKey.AccessKeyId
                            });
                    }

                    var attachedPolicies = userModel.IAMClient.ListAttachedUserPolicies(new ListAttachedUserPoliciesRequest { UserName = model.Name }).AttachedPolicies;
                    foreach(var policy in attachedPolicies)
                    {
                        userModel.IAMClient.DetachUserPolicy(new DetachUserPolicyRequest 
                            { 
                                UserName = model.Name, 
                                PolicyArn = policy.PolicyArn 
                            });
                    }

                    var request = new DeleteUserRequest() { UserName = userModel.User.UserName };
                    userModel.IAMClient.DeleteUser(request);
                }
                catch (Exception e)
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Error deleting user: " + e.Message);
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
