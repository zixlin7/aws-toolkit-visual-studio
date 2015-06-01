using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.RDS
{
    public static class RDSUtil
    {
        const int LOGIN_FAILED_ERRORCODE = 18456;

        public static bool CanAccessSQLServer(Amazon.RDS.Model.Endpoint endpoint)
        {
            return CanAccessSQLServer(string.Format("{0},{1}", endpoint.Address, endpoint.Port));
        }

        public static bool CanAccessSQLServer(string endpoint)
        {
            try
            {
                SqlConnectionStringBuilder sb = new SqlConnectionStringBuilder();
                sb.DataSource = endpoint;
                sb.UserID = "aws-tools-port-probe";
                sb.Password = "invalid-password";
                sb.ConnectTimeout = 4;

                using (var sqlConnection = new SqlConnection(sb.ToString()))
                {
                    sqlConnection.Open();
                }
            }
            catch (SqlException e)
            {
                foreach (SqlError error in e.Errors)
                {
                    if (error.Number == LOGIN_FAILED_ERRORCODE)
                    {
                        return true;
                    }
                }
            }
            catch
            {
            }
            return false;
        }
    }
}
