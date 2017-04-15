using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CodeCommit.Interface.Model;
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

        /// <summary>
        /// Monitors for changes in the active connection and wires up to receive
        /// repository list change events when a connection is established.
        /// </summary>
        /// <param name="connection"></param>
        private void OnTeamExplorerBindingChanged(TeamExplorerConnection connection)
        {
            if (connection != null)
            {
                connection.PropertyChanged += (sender, args) =>
                {
                    RaisePropertyChanged(args.PropertyName);
                };
            }

            RaisePropertyChanged("Repositories");
            RaisePropertyChanged("SignoutLabel");
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

        public ObservableCollection<ICodeCommitRepository> Repositories 
            => TeamExplorerConnection.ActiveConnection != null
                ? TeamExplorerConnection.ActiveConnection.Repositories
                : null;

        public ICodeCommitRepository SelectedRepository { get; set; }

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
