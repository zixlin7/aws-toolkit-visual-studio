using System;
using System.Collections.Generic;

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

        public string DBEngine { get; }
        
        public bool IsMySql => DBEngine.StartsWith("mysql", StringComparison.InvariantCultureIgnoreCase);

        public bool IsOracle => DBEngine.StartsWith("oracle", StringComparison.InvariantCultureIgnoreCase);

        public bool IsSqlServer => DBEngine.StartsWith("sqlserver", StringComparison.InvariantCultureIgnoreCase);

        public bool IsPostgres => DBEngine.StartsWith("postgres", StringComparison.InvariantCultureIgnoreCase);

        public int DefaultPort { get; }
        public int MinPort { get; }
        public int MaxPort { get; }

        public IEnumerable<string> SupportedLicenses { get; }
        public IEnumerable<string> SupportedInstanceClassIDs { get; }
        
        public IEnumerable<DBInstanceClass> SupportedInstanceClasses => RDSServiceMeta.Instance.MetaForDBInstancesClasses(SupportedInstanceClassIDs);

        public bool SupportsMultiAZ { get; }

        public int MinStorageAlloc { get; }
        public int MaxStorageAlloc { get; }

        public int MinDBInstanceIdentifierLength { get; }
        public int MaxDBInstanceIdentifierLength { get; }

        public int MinDBNameLength { get; }
        public int MaxDBNameLength { get; }

        public int MinDBParameterGroupNameLength { get; }
        public int MaxDBParameterGroupNameLength { get; }

        public int MinMasterUserNameLength { get; }
        public int MaxMasterUserNameLength { get; }

        public int MinMasterPwdNameLength { get; }
        public int MaxMasterPwdNameLength { get; }

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
