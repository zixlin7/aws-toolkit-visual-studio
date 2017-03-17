using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.VisualStudio.TeamExplorer.CodeCommit.Controls;

namespace Amazon.AWSToolkit.VisualStudio.TeamExplorer.CodeCommit.Controllers
{
    /// <summary>
    /// Manages the connection flow to CodeCommit. This involves selecting, or creating,
    /// an AWS credential profile. The selected profile is then registered as the active
    /// TeamExplorer connection with the connection manager.
    /// </summary>
    public class ConnectController
    {
        private ConnectControl _selectionControl;

        public ActionResults Execute()
        {
            this._selectionControl = new ConnectControl(this);
            if (ToolkitFactory.Instance.ShellProvider.ShowModal(_selectionControl))
            {
                return new ActionResults().WithSuccess(true);
            }

            return new ActionResults().WithSuccess(false);
        }

        public AccountViewModel SelectedAccount => _selectionControl?.SelectedAccount;
    }
}
