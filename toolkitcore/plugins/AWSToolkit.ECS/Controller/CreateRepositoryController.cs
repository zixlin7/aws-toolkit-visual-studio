using System;
using Amazon.AWSToolkit.ECS.Model;
using Amazon.AWSToolkit.ECS.Nodes;
using Amazon.AWSToolkit.ECS.View;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.ECR.Model;

namespace Amazon.AWSToolkit.ECS.Controller
{
    public class CreateRepositoryController : BaseContextCommand
    {
        CreateRepositoryControl _control;
        CreateRepositoryModel _model;
        RepositoriesRootViewModel _rootModel;
        ActionResults _results;

        public override ActionResults Execute(IViewModel model)
        {
            this._rootModel = model as RepositoriesRootViewModel;
            if (this._rootModel == null)
                return new ActionResults().WithSuccess(false);

            this._model = new CreateRepositoryModel();
            this._control = new CreateRepositoryControl(this);
            ToolkitFactory.Instance.ShellProvider.ShowModal(this._control);

            if (this._results == null)
                return new ActionResults().WithSuccess(false);
            return this._results;
        }

        public CreateRepositoryModel Model => this._model;

        public bool Persist()
        {
            try
            {
                var response = this._rootModel.ECRClient.CreateRepository(new CreateRepositoryRequest
                {
                    RepositoryName = this.Model.RepositoryName
                });
                
                this._results = new ActionResults().WithSuccess(true);
                this._rootModel.AddRepository(new RepositoryWrapper(response.Repository));
                return true;
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating repository: " + e.Message);
                this._results = new ActionResults().WithSuccess(false);
                return false;
            }
        }
    }
}
