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
