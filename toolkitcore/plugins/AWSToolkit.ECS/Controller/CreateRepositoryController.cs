using System;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.ECS.Model;
using Amazon.AWSToolkit.ECS.Nodes;
using Amazon.AWSToolkit.ECS.Util;
using Amazon.AWSToolkit.ECS.View;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.ECR.Model;

namespace Amazon.AWSToolkit.ECS.Controller
{
    public class CreateRepositoryController : BaseContextCommand
    {
        private readonly ToolkitContext _toolkitContext;
        CreateRepositoryControl _control;
        CreateRepositoryModel _model;
        RepositoriesRootViewModel _rootModel;
        ActionResults _results;

        public CreateRepositoryController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public override ActionResults Execute(IViewModel model)
        {
            var result = CreateRepository(model);
            RecordMetric(result);
            return result;
        }

        public ActionResults CreateRepository(IViewModel model)
        {
            _rootModel = model as RepositoriesRootViewModel;
            if (_rootModel == null)
            {
                return ActionResults.CreateFailed(new ToolkitException("Unable to find ECR repository data",
                       ToolkitException.CommonErrorCode.InternalMissingServiceState));
            }

            _model = new CreateRepositoryModel();
            _control = new CreateRepositoryControl(this);
            if (!_toolkitContext.ToolkitHost.ShowModal(_control))
            {
                return ActionResults.CreateCancelled();
            }

            return _results ?? ActionResults.CreateFailed();
        }

        public CreateRepositoryModel Model => this._model;

        public bool Persist()
        {
            try
            {
                var response = _rootModel.ECRClient.CreateRepository(new CreateRepositoryRequest
                {
                    RepositoryName = Model.RepositoryName
                });

                _results = new ActionResults().WithSuccess(true);
                _rootModel.AddRepository(new RepositoryWrapper(response.Repository));
                return true;
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating repository: " + e.Message);
                _results = ActionResults.CreateFailed(e);

                // Record failures immediately -- the top level call records success/cancel once the dialog is closed
                RecordMetric(_results);
                return false;
            }
        }

        private void RecordMetric(ActionResults result)
        {
            var awsConnectionSettings = _rootModel?.EcsRootViewModel?.AwsConnectionSettings;
            _toolkitContext.RecordEcrCreateRepository(result, awsConnectionSettings);
        }
    }
}
