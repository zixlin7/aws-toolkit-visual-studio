using System.ComponentModel.Composition;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.VisualStudio.TeamExplorer.CodeCommit.Controls;
using Amazon.AWSToolkit.VisualStudio.TeamExplorer.CodeCommit.Model;
using Amazon.AWSToolkit.VisualStudio.TeamExplorer.CredentialManagement;
using log4net;

namespace Amazon.AWSToolkit.VisualStudio.TeamExplorer.CodeCommit.Connect
{
    [TeamExplorerSection(TeamExplorerConnectionSectionId, TeamExplorerPageIds.Connect, 10)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class ConnectionSection : TeamExplorerSectionBase
    {
        public const string TeamExplorerConnectionSectionId = "FF7A257A-3AFB-44AC-B0F9-EA5F8789107E";

        private readonly ILog LOGGER = LogManager.GetLogger(typeof(ConnectionSection));
        private ConnectionSectionControl _view;
        private ConnectSectionViewModel _viewModel;

        [ImportingConstructor]
        public ConnectionSection()
        {
            Utility.ConfigureLog4Net();
            LOGGER.Info("Creating CodeCommit ConnectionSection");
        }

        protected override ITeamExplorerSection CreateViewModel(SectionInitializeEventArgs e)
        {
            if (_viewModel == null)
            {
                _viewModel = new ConnectSectionViewModel
                {
                    Title = "AWS CodeCommit"
                };
            }

            return _viewModel;
        }

        public override void Initialize(object sender, SectionInitializeEventArgs e)
        {
            LOGGER.Info("CodeCommit Connect Initialize");
            base.Initialize(sender, e);

            IsVisible = TeamExplorerConnection.ActiveConnection != null;
            TeamExplorerConnection.OnTeamExplorerBindingChanged += OnTeamExplorerBindingChanged;
        }

        protected override object CreateView(SectionInitializeEventArgs e)
        {
            LOGGER.Info("CodeCommit Connect CreateView");
            return _view ?? (_view = new ConnectionSectionControl());
        }

        protected override void InitializeView(SectionInitializeEventArgs e)
        {
            LOGGER.Info("CodeCommit Connect InitializeView");
            _view.DataContext = _viewModel;
        }

        // triggers a change of visibility of the section, and a refresh of the repos collection
        // in the panel
        private void OnTeamExplorerBindingChanged(TeamExplorerConnection connection)
        {
            LOGGER.Info("CodeCommit Connect OnTeamExplorerBindingChanged");
            IsVisible = connection != null;
            //_viewModel?.RefreshRepositoriesList();
        }
    }
}
