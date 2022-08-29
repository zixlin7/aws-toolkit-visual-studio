using System.ComponentModel.Composition;

using Amazon.AWSToolkit.CodeCommitTeamExplorer.CodeCommit.Controls;
using Amazon.AWSToolkit.CodeCommitTeamExplorer.CodeCommit.Model;
using Amazon.AWSToolkit.CodeCommitTeamExplorer.CredentialManagement;

using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;

namespace Amazon.AWSToolkit.CodeCommitTeamExplorer.CodeCommit.Connect
{
    /// <summary>
    /// MEF Activated
    /// This manages the panel that is shown when users are connected to CodeCommit
    /// with credentials. That panel allows users to Clone and Create CodeCommit repos,
    /// and shows repos that have been previously pulled locally.
    /// 
    /// If user signs out, this panel is hidden, and <see cref="InvitationSection"/> is shown.
    /// </summary>
    [TeamExplorerSection(TeamExplorerConnectionSectionId, TeamExplorerPageIds.Connect, 10)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class ConnectionSection : TeamExplorerSectionBase
    {
        public const string TeamExplorerConnectionSectionId = "FF7A257A-3AFB-44AC-B0F9-EA5F8789107E";

        private ConnectionSectionControl _view;
        private ConnectSectionViewModel _viewModel;

        [ImportingConstructor]
        public ConnectionSection()
        {
            Utility.ConfigureLog4Net();
        }

        protected override ITeamExplorerSection CreateViewModel(SectionInitializeEventArgs e)
        {
            return _viewModel ?? (_viewModel = new ConnectSectionViewModel());
        }

        public override void Initialize(object sender, SectionInitializeEventArgs e)
        {
            base.Initialize(sender, e);

            IsVisible = TeamExplorerConnection.ActiveConnection != null;
            TeamExplorerConnection.OnTeamExplorerBindingChanged += (oldConnection, newConnection) => IsVisible = newConnection != null;
        }

        protected override object CreateView(SectionInitializeEventArgs e)
        {
            return _view ?? (_view = new ConnectionSectionControl());
        }

        protected override void InitializeView(SectionInitializeEventArgs e)
        {
            _view.DataContext = _viewModel;
        }
    }
}
