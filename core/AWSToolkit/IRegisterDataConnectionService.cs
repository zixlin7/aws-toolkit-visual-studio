using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit
{
    public enum DatabaseTypes { Unknown, SQLServer, MySQL, Oracle };

    public interface IRegisterDataConnectionService
    {
        void AddDataConnection(DatabaseTypes type, string connectionName, string connectionString);
        void RegisterDataConnection(DatabaseTypes type, string connectionPrefixName, string host, int port, string masterUsername, string dbName);
    }

    public class RegisterDataConnectionException : Exception
    {

        public static RegisterDataConnectionException CreateMySQLMissingProvider(Exception innerException)
        {
            return new RegisterDataConnectionException("Data provider for connecting to MySQL was not found.  The data provider can be downloaded from the following link.", innerException, 
                "http://dev.mysql.com/tech-resources/articles/mysql-installer-for-windows.html");
        }

        RegisterDataConnectionException(string message, Exception innerException, string url)
            : base(message, innerException)
        {
            this.URL = url;
        }



        public string URL
        {
            get;
            private set;
        }
    }
}
