using System.ComponentModel.Composition;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.VisualStudio.TeamExplorer.CodeCommit.Controls;
using Amazon.AWSToolkit.VisualStudio.TeamExplorer.CodeCommit.Model;
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
            base.Initialize(sender, e);

            IsVisible = ConnectionsManager.Instance.TeamExplorerAccount != null;
            ConnectionsManager.Instance.OnTeamExplorerBindingChanged += OnTeamExplorerBindingChanged;
        }

        protected override object CreateView(SectionInitializeEventArgs e)
        {
            return _view ?? (_view = new ConnectionSectionControl());
        }

        protected override void InitializeView(SectionInitializeEventArgs e)
        {
            _view.DataContext = _viewModel;
        }

        // triggers a change of visibility of the section, and a refresh of the repos collection
        // in the panel
        private void OnTeamExplorerBindingChanged(AccountViewModel boundAccount)
        {
            IsVisible = boundAccount != null;
            _viewModel?.RefreshRepositoriesList();
        }
    }
}
