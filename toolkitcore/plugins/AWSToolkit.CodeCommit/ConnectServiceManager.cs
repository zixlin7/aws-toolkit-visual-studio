namespace Amazon.AWSToolkit.CodeCommit
{
    public static class ConnectServiceManager
    {
        public static IConnectService ConnectService { get; set; }



        public interface IConnectService
        {
            void OpenConnection();
        }
    }
}
