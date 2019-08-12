using System;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.CloudFormation.Nodes;
using Amazon.CloudFormation.Model;

namespace Amazon.AWSToolkit.CloudFormation.Controllers
{
    public class DeleteStackController : BaseContextCommand
    {
        public override ActionResults Execute(IViewModel model)
        {
            var stackModel = model as CloudFormationStackViewModel;
            if (stackModel == null)
                return new ActionResults().WithSuccess(false);

            string msg = string.Format("Are you sure you want to delete the {0} stack?", model.Name);
            if (ToolkitFactory.Instance.ShellProvider.Confirm("Delete Stack", msg))
            {
                try
                {
                    var request = new DeleteStackRequest() { StackName = stackModel.StackName };
                    stackModel.CloudFormationClient.DeleteStack(request);
                }
                catch (Exception e)
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Error deleting stack: " + e.Message);
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
