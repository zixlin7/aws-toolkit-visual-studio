using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Amazon.AWSToolkit.ECS.Nodes;
using Amazon.AWSToolkit.ECS.View;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.ECR.Model;

namespace Amazon.AWSToolkit.ECS.Controller
{
    public class DeleteRepositoryController : BaseContextCommand
    {
        ActionResults _results;

        public override ActionResults Execute(IViewModel model)
        {
            var repositoryModel = model as RepositoryViewModel;
            if (repositoryModel == null)
                return new ActionResults().WithSuccess(false);

            var control = new DeleteRepositoryControl(repositoryModel.Name);
            if (ToolkitFactory.Instance.ShellProvider.ShowModal(control, MessageBoxButton.YesNo))
            {
                try
                {
                    repositoryModel.ECRClient.DeleteRepository(new DeleteRepositoryRequest
                    {
                        RepositoryName = repositoryModel.RepositoryName,
                        Force = control.ForceDelete
                    });

                    repositoryModel.RootViewModel.RemoveRepositoryInstance(repositoryModel.RepositoryName);
                    return new ActionResults().WithSuccess(true);
                }
                catch (Exception e)
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Error deleting repository: " + e.Message);
                }
            }

            return new ActionResults().WithSuccess(false);
        }
    }
}
