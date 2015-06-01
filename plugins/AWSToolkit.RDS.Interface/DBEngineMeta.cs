using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.RDS
{
    /// <summary>
    /// Additional non-queryable metatdata for DB engines (supported classes, field length limits etc)
    /// </summary>
    public class DBEngineMeta
    {
        // engine type names
        public static readonly string MYSQL = "mysql";
        public static readonly string ORACLE_EE = "oracle-ee";
        public static readonly string SQLSERVER_SE = "sqlserver-se";
        public static readonly string SQLSERVER_EX = "sqlserver-ex";
        public static readonly string SQLSERVER_WEB = "sqlserver-web";

        public string DBEngine { get; private set; }
        
        public bool IsMySql
        {
            get { return DBEngine.StartsWith("mysql", StringComparison.InvariantCultureIgnoreCase); }
        }
        public bool IsOracle
        {
            get { return DBEngine.StartsWith("oracle", StringComparison.InvariantCultureIgnoreCase); }
        }
        public bool IsSqlServer
        {
            get { return DBEngine.StartsWith("sqlserver", StringComparison.InvariantCultureIgnoreCase); }
        }
        public bool IsPostgres
        {
            get { return DBEngine.StartsWith("postgres", StringComparison.InvariantCultureIgnoreCase); }
        }

        public int DefaultPort { get; private set; }
        public int MinPort { get; private set; }
        public int MaxPort { get; private set; }

        public IEnumerable<string> SupportedLicenses { get; private set; }
        public IEnumerable<string> SupportedInstanceClassIDs { get; private set; }
        
        public IEnumerable<DBInstanceClass> SupportedInstanceClasses
        {
            get
            {
                return RDSServiceMeta.Instance.MetaForDBInstancesClasses(SupportedInstanceClassIDs);
            }
        }

        public bool SupportsMultiAZ { get; private set; }

        public int MinStorageAlloc { get; private set; }
        public int MaxStorageAlloc { get; private set; }

        public int MinDBInstanceIdentifierLength { get; private set; }
        public int MaxDBInstanceIdentifierLength { get; private set; }

        public int MinDBNameLength { get; private set; }
        public int MaxDBNameLength { get; private set; }

        public int MinDBParameterGroupNameLength { get; private set; }
        public int MaxDBParameterGroupNameLength { get; private set; }

        public int MinMasterUserNameLength { get; private set; }
        public int MaxMasterUserNameLength { get; private set; }

        public int MinMasterPwdNameLength { get; private set; }
        public int MaxMasterPwdNameLength { get; private set; }

        public DBEngineMeta(string dbEngine, 
                            int port, int minPort, int maxPort, 
                            IEnumerable<string> licenses, 
                            IEnumerable<string> instanceClasses, 
                            bool supportsMultiAZ, 
                            int minStorage, int maxStorage, 
                            int minIdLength, int maxIdLength, 
                            int minNameLength, int maxNameLength, 
                            int minParamGroupNameLength, int maxParamGroupNameLength, 
                            int minMasterUserNameLength, int maxMasterUserNameLength, 
                            int minMasterUserPwdLength, int maxMasterUserPwdLength)
        {
            this.DBEngine = dbEngine;
            this.DefaultPort = port;
            this.MinPort = minPort;
            this.MaxPort = maxPort;
            this.SupportedLicenses = licenses;
            this.SupportedInstanceClassIDs = instanceClasses;
            this.SupportsMultiAZ = supportsMultiAZ;
            this.MinStorageAlloc = minStorage;
            this.MaxStorageAlloc = maxStorage;
            this.MinDBInstanceIdentifierLength = minIdLength;
            this.MaxDBInstanceIdentifierLength = maxIdLength;
            this.MinDBNameLength = minNameLength;
            this.MaxDBNameLength = maxNameLength;
            this.MinDBParameterGroupNameLength = minParamGroupNameLength;
            this.MaxDBParameterGroupNameLength = maxParamGroupNameLength;
            this.MinMasterUserNameLength = minMasterUserNameLength;
            this.MaxMasterUserNameLength = maxMasterUserNameLength;
            this.MinMasterPwdNameLength = minMasterUserPwdLength;
            this.MaxMasterPwdNameLength = maxMasterUserPwdLength;
        }
    }
}
