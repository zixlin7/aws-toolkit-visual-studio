using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Lambda.Nodes;
using Amazon.AWSToolkit;

using Amazon.Lambda;
using Amazon.Lambda.Model;

namespace Amazon.AWSToolkit.Lambda.Controller
{
    public class DeleteFunctionController : BaseContextCommand
    {
        public override ActionResults Execute(IViewModel model)
        {
            LambdaFunctionViewModel functionModel = model as LambdaFunctionViewModel;
            if (functionModel == null)
                return new ActionResults().WithSuccess(false);

            var msg = string.Format("Are you sure you want to delete the {0} cloud function?", model.Name);

            if (ToolkitFactory.Instance.ShellProvider.Confirm("Delete Function", msg))
            {
                try
                {
                    functionModel.LambdaClient.DeleteFunction(functionModel.Name);
                }
                catch (Exception e)
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Error deleting functionn: " + e.Message);
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
