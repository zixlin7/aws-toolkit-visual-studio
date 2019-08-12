using System;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.IdentityManagement.Nodes;
using Amazon.IdentityManagement.Model;

namespace Amazon.AWSToolkit.IdentityManagement.Controller
{
    public class DeleteGroupController : BaseContextCommand
    {
        public override ActionResults Execute(IViewModel model)
        {
            IAMGroupViewModel groupModel = model as IAMGroupViewModel;
            if (groupModel == null)
                return new ActionResults().WithSuccess(false);

            string msg = string.Format("Are you sure you want to delete the {0} group and all of its policies?", model.Name);
            if (ToolkitFactory.Instance.ShellProvider.Confirm("Delete Group", msg))
            {
                try
                {
                    var policies = groupModel.IAMClient.ListGroupPolicies(new ListGroupPoliciesRequest { GroupName = groupModel.Group.GroupName }).PolicyNames;
                    foreach(var policy in policies)
                    {
                        groupModel.IAMClient.DeleteGroupPolicy(new DeleteGroupPolicyRequest
                            {
                                GroupName = groupModel.Group.GroupName,
                                PolicyName = policy
                            });
                    }

                    var attachedPolicies = groupModel.IAMClient.ListAttachedGroupPolicies(new ListAttachedGroupPoliciesRequest { GroupName = model.Name }).AttachedPolicies;
                    foreach (var policy in attachedPolicies)
                    {
                        groupModel.IAMClient.DetachGroupPolicy(new DetachGroupPolicyRequest
                        {
                            GroupName = model.Name,
                            PolicyArn = policy.PolicyArn
                        });
                    }

                    var request = new DeleteGroupRequest() { GroupName = groupModel.Group.GroupName };
                    groupModel.IAMClient.DeleteGroup(request);
                }
                catch (Exception e)
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Error deleting group: " + e.Message);
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
