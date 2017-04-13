using System.Windows.Input;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.VisualStudio.TeamExplorer.CodeCommit.Controllers;
using Amazon.AWSToolkit.VisualStudio.TeamExplorer.CredentialManagement;

namespace Amazon.AWSToolkit.VisualStudio.TeamExplorer.CodeCommit.Model
{
    public class ConnectSectionViewModel : TeamExplorerSectionViewModelBase
    {
        public ConnectSectionViewModel()
        {
            TeamExplorerConnection.OnTeamExplorerBindingChanged += OnTeamExplorerBindingChanged;

            _cloneCommand = new CommandHandler(OnClone, true);
            _createCommand = new CommandHandler(OnCreate, true);
            _signoutCommand = new CommandHandler(OnSignout, true);
        }

        // used to trigger notification that the Sign-out button label should update
        // to include the active profile name
        private void OnTeamExplorerBindingChanged(TeamExplorerConnection connection)
        {
            RaisePropertyChanged("SignoutLabel");
        }

        public void RefreshRepositoriesList()
        {
        }

        public string SignoutLabel=> TeamExplorerConnection.ActiveConnection == null 
            ? "Sign out" 
            : string.Concat("Sign out ", TeamExplorerConnection.ActiveConnection.Account.DisplayName);

        private readonly CommandHandler _signoutCommand;
        public ICommand SignoutCommand => _signoutCommand;

        private readonly CommandHandler _cloneCommand;
        public ICommand CloneCommand => _cloneCommand;

        private readonly CommandHandler _createCommand;
        public ICommand CreateCommand => _createCommand;

        private void OnClone()
        {
            new CloneRepositoryController().Execute();
        }

        private void OnCreate()
        {
            new CreateRepositoryController().Execute();
        }

        private void OnSignout()
        {
            TeamExplorerConnection.ActiveConnection.Signout();
        }

    }
}
