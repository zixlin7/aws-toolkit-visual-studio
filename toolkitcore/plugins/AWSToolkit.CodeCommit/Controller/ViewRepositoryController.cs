using System.Threading;
using System.Windows.Threading;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CodeCommit.Model;
using Amazon.AWSToolkit.CodeCommit.Nodes;
using Amazon.AWSToolkit.CodeCommit.View;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.CodeCommit;
using Amazon.CodeCommit.Model;

namespace Amazon.AWSToolkit.CodeCommit.Controller
{
    public class ViewRepositoryController : BaseContextCommand
    {
        private IAmazonCodeCommit _codecommitClient;
        CodeCommitRepositoryViewModel _repositoryViewModel;

        private Dispatcher _uiDispatcher;

        public override ActionResults Execute(IViewModel model)
        {/*
            _projectViewModel = model as DevHubProjectViewModel;
            if (_projectViewModel == null)
                return new ActionResults().WithSuccess(false);

            _devhubClient = _projectViewModel.DevHubClient;

            var region = _projectViewModel.DevHubRootViewModel.CurrentEndPoint.RegionSystemName;

            _model = new ViewProjectModel(region, _projectViewModel.Project);
            _control = new ViewProjectControl(this);

            _uiDispatcher = _control.Dispatcher;

            ToolkitFactory.Instance.ShellProvider.OpenInEditor(_control);
           */
            return new ActionResults().WithSuccess(false);
        }

        public string RepositoryName
        {
            get { return Model?.RepositoryName; }
        }

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
            var rootViewModel = _repositoryViewModel?.Parent;
        }
    }
}
