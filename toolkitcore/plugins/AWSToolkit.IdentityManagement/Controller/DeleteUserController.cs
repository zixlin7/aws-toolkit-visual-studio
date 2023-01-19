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
    public class DeleteUserController : BaseContextCommand
    {
        private readonly ToolkitContext _toolkitContext;

        public DeleteUserController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public override ActionResults Execute(IViewModel model)
        {
            var result = DeleteUser(model);
            RecordMetric(result, model);
            return result;
        }

        public ActionResults DeleteUser(IViewModel model)
        {
            var userModel = model as IAMUserViewModel;
            if (userModel == null)
            {
                return ActionResults.CreateFailed(new ToolkitException("Unable to find IAM user data",
                            ToolkitException.CommonErrorCode.InternalMissingServiceState));
            }

            var msg = $"Are you sure you want to delete the {model.Name} user and all of its policies?";
            if (!_toolkitContext.ToolkitHost.Confirm("Delete User", msg))
            {
                return ActionResults.CreateCancelled();
            }

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
                foreach (var group in groups)
                {
                    userModel.IAMClient.RemoveUserFromGroup(new RemoveUserFromGroupRequest
                    {
                        GroupName = group.GroupName,
                        UserName = model.Name
                    });
                }

                var accessKeys = userModel.IAMClient.ListAccessKeys(new ListAccessKeysRequest { UserName = model.Name }).AccessKeyMetadata;
                foreach (var accessKey in accessKeys)
                {
                    userModel.IAMClient.DeleteAccessKey(new DeleteAccessKeyRequest
                    {
                        UserName = model.Name,
                        AccessKeyId = accessKey.AccessKeyId
                    });
                }

                var attachedPolicies = userModel.IAMClient.ListAttachedUserPolicies(new ListAttachedUserPoliciesRequest { UserName = model.Name }).AttachedPolicies;
                foreach (var policy in attachedPolicies)
                {
                    userModel.IAMClient.DetachUserPolicy(new DetachUserPolicyRequest
                    {
                        UserName = model.Name,
                        PolicyArn = policy.PolicyArn
                    });
                }

                var request = new DeleteUserRequest() { UserName = userModel.User.UserName };
                userModel.IAMClient.DeleteUser(request);

                return new ActionResults()
                    .WithSuccess(true)
                    .WithFocalname(model.Name)
                    .WithShouldRefresh(true);
            }
            catch (Exception e)
            {
                _toolkitContext.ToolkitHost.ShowError($"Error deleting user:{Environment.NewLine}{e.Message}");
                return ActionResults.CreateFailed(e);
            }
        }

        public void RecordMetric(ActionResults results, IViewModel model)
        {
            var groupModel = model as IAMUserViewModel;
            var awsConnectionSettings = groupModel?.IAMUserRootViewModel?.IAMRootViewModel?.AwsConnectionSettings;
            _toolkitContext.RecordIamDelete(IamResourceType.User, results,
                 awsConnectionSettings);
        }
    }
}
