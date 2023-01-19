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
    public class DeleteGroupController : BaseContextCommand
    {
        private readonly ToolkitContext _toolkitContext;

        public DeleteGroupController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public override ActionResults Execute(IViewModel model)
        {
            var result = DeleteGroup(model);
            RecordMetric(result, model);
            return result;
        }

        public ActionResults DeleteGroup(IViewModel model)
        {
            var groupModel = model as IAMGroupViewModel;
            if (groupModel == null)
            {
                return ActionResults.CreateFailed(new ToolkitException("Unable to find IAM group data",
                            ToolkitException.CommonErrorCode.InternalMissingServiceState));
            }

            var msg = $"Are you sure you want to delete the {model.Name} group and all of its policies?";

            if (!_toolkitContext.ToolkitHost.Confirm("Delete Group", msg))
            {
                return ActionResults.CreateCancelled();
            }

            try
            {
                var policies = groupModel.IAMClient.ListGroupPolicies(new ListGroupPoliciesRequest { GroupName = groupModel.Group.GroupName }).PolicyNames;
                foreach (var policy in policies)
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

                return new ActionResults()
                    .WithSuccess(true)
                    .WithFocalname(model.Name)
                    .WithShouldRefresh(true);
            }
            catch (Exception e)
            {
                _toolkitContext.ToolkitHost.ShowError($"Error deleting group:{Environment.NewLine}{e.Message}");
                return ActionResults.CreateFailed(e);
            }
        }

        public void RecordMetric(ActionResults results, IViewModel model)
        {
            var groupModel = model as IAMGroupViewModel;
            var awsConnectionSettings = groupModel?.IAMGroupRootViewModel?.IAMRootViewModel?.AwsConnectionSettings;
            _toolkitContext.RecordIamDelete(IamResourceType.Group, results,
                 awsConnectionSettings);
        }
    }
}
