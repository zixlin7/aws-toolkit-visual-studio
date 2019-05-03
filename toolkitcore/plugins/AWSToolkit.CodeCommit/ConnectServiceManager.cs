using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
