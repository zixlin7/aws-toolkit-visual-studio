using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amazon.AWSToolkit.CodeCommitTeamExplorer.CodeCommit
{
    internal class TeamExplorerConnectService : Amazon.AWSToolkit.CodeCommit.ConnectServiceManager.IConnectService
    {
        public void OpenConnection()
        {
            Controllers.ConnectController.OpenConnection();
        }
    }
}
