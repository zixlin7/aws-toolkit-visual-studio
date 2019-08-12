using System;
using System.Windows.Threading;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CodeCommit.Model;
using Amazon.AWSToolkit.CodeCommit.Nodes;
using Amazon.AWSToolkit.CodeCommit.View;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.CodeCommit;
using Amazon.CodeCommit.Model;
using log4net;

namespace Amazon.AWSToolkit.CodeCommit.Controller
{
    public class ViewRepositoryController : BaseContextCommand
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(ViewRepositoryController));

        private IAmazonCodeCommit _codecommitClient;
        private CodeCommitRepositoryViewModel _repositoryViewModel;
        private Dispatcher _uiDispatcher;

        public override ActionResults Execute(IViewModel model)
        {
            this._repositoryViewModel = model as CodeCommitRepositoryViewModel;
            if (this._repositoryViewModel == null)
                return new ActionResults().WithSuccess(false);

            _codecommitClient = _repositoryViewModel.CodeCommitClient;

            Model = new ViewRepositoryModel(QueryRepositoryMetadata());

            Control = new ViewRepositoryControl(this);

            _uiDispatcher = Control.Dispatcher;

            ToolkitFactory.Instance.ShellProvider.OpenInEditor(Control);

            return new ActionResults().WithSuccess(false);
        }

        public string RepositoryName => Model?.Name;

        public ViewRepositoryModel Model { get; private set; }

        public ViewRepositoryControl Control { get; private set; }

        public AccountViewModel Account => _repositoryViewModel?.AccountViewModel;

        public void LoadModel()
        {
            RefreshAll();
        }

        // note: this (and LoadModel) need to be invoked in the context of a worker
        // thread. executeBackGroundLoadDataLoad in the BaseAWSControl class handles this
        // for us on control instantiation.
        public void RefreshAll()
        {
        }

        private RepositoryMetadata QueryRepositoryMetadata()
        {
            try
            {
                var response = _codecommitClient.GetRepository(
                    new GetRepositoryRequest { RepositoryName = _repositoryViewModel.RepositoryNameAndID.RepositoryName });
                return response.RepositoryMetadata;
            }
            catch (Exception e)
            {
                LOGGER.Error("Failed to query repository metadata for " + _repositoryViewModel.RepositoryNameAndID.RepositoryName, e);
            }

            return null;
        }
    }
}
