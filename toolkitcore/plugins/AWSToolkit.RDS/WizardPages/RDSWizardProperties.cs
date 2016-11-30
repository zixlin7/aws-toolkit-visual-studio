namespace Amazon.AWSToolkit.RDS.WizardPages
{
    internal static class RDSWizardProperties
    {
        public static class SeedData
        {
            /// <summary>
            /// List<DBEngineWrapper>, available db engine versions for the selected
            /// engine class for the user to choose from.
            /// </summary>
            public static readonly string propkey_DBEngineVersions = "dbEngineVersions";

            /// <summary>
            /// DBInstanceWrapper, optional. Set to indicate to shared pages that they are running under
            /// the wizard to modify instance details rather than create a new instance.
            /// </summary>
            public static readonly string propkey_DBInstanceWrapper = "dbInstanceWrapper";

            /// <summary>
            /// AmazonEC2 client instance correctly scoped to region and account.
            /// </summary>
            public static readonly string propkey_EC2Client = "ec2Client";

            /// <summary>
            /// AmazonRDS client instance correctly scoped to region and account.
            /// </summary>
            public static readonly string propkey_RDSClient = "rdsClient";

            /// <summary>
            /// Boolean, true if the user is restricted to VPC environments only.
            /// </summary>
            public static readonly string propkey_VPCOnly = "vpcOnly";
        }

        public static class EngineProperties
        {
            /// <summary>
            /// String, the name of the selected db engine
            /// </summary>
            public static readonly string propkey_DBEngine = "dbEngine";

            /// <summary>
            /// DBEngineVersion instance for the selected db engine version
            /// </summary>
            public static readonly string propkey_EngineVersion = "engineVersion";
        }

        public static class InstanceProperties
        {

            /// <summary>
            /// String, optional. Selected licensing model.
            /// </summary>
            public static readonly string propkey_LicenseModel = "licenseModel";

            /// <summary>
            /// DBInstanceType; 'instance size' of the RDS server (db.m1.small etc)
            /// </summary>
            public static readonly string propkey_InstanceClass = "instanceClass";

            /// <summary>
            /// Bool, optional. False to not deploy to multi-az.
            /// </summary>
            public static readonly string propkey_MultiAZ = "multiAZ";

            /// <summary>
            /// Bool, optional. True to perform minor version upgrades automatically.
            /// </summary>
            public static readonly string propkey_AutoMinorVersionUpgrade = "autoMinorVersionUpgrade";

            /// <summary>
            /// Int; size of the storage pool to allocate.
            /// </summary>
            public static readonly string propkey_Storage = "storage";

            /// <summary>
            /// String; identifier name of the RDS instance.
            /// </summary>
            public static readonly string propkey_DBInstanceIdentifier = "dbInstanceIdentifier";

            /// <summary>
            /// String; name/id of the master DB user
            /// </summary>
            public static readonly string propkey_MasterUserName = "masterUserName";

            /// <summary>
            /// String; password for the master DB user
            /// </summary>
            public static readonly string propkey_MasterUserPassword = "masterUserPassword";

            /// <summary>
            /// String, optional, Oracle and MySql only
            /// </summary>
            public static readonly string propkey_DatabaseName = "databaseName";

            /// <summary>
            /// Int. Initially set to default port for the selected engine by the wizard.
            /// </summary>
            public static readonly string propkey_DatabasePort = "databasePort";

            /// <summary>
            /// String, optional. Id of the vpc.
            /// </summary>
            public static readonly string propkey_VpcId = "vpcId";

            /// <summary>
            /// String, optional. Name of the DB subnet group to associate with the VPC.
            /// </summary>
            public static readonly string propkey_DBSubnetGroup = "dbSubnetGroup";

            /// <summary>
            /// Bool, optional. Set true if the launch controller should create a new
            /// subnet group for the vpc being used.
            /// </summary>
            public static readonly string propkey_CreateDBSubnetGroup = "createDBSubnetGroup";

            /// <summary>
            /// String, optional. Set only if propkey_MultiAZ is false.
            /// </summary>
            public static readonly string propkey_AvailabilityZone = "availabilityZone";

            /// <summary>
            /// String, optional. Name of the DB parameter group to use with the instance.
            /// </summary>
            public static readonly string propkey_DBParameterGroup = "dbParameterGroup";

            /// <summary>
            /// List of SecurityGroupInfo instances, optional. Details of zero or more 
            /// RDS DB security groups or EC2-VPC security groups to use with the instance.
            /// </summary>
            public static readonly string propkey_SecurityGroups = "dbSecurityGroups";

            /// <summary>
            /// Bool, optional. If set, add the current best-estimate CIR to the selected security
            /// groups.
            /// </summary>
            public static readonly string propkey_AddCIDRToGroups = "addCIDRToSecurityGroups";

            /// <summary>
            /// Bool, optional. If set a new security group (RDS or EC2-VPC) will be created for the
            /// new database instance.
            /// </summary>
            public static readonly string propkey_CreateNewDBSecurityGroup = "createNewDBSecurityGroup";

            /// <summary>
            /// Bool, optional. Set if launching into a VPC.
            /// </summary>
            public static readonly string propkey_PubliclyAccessible = "publiclyAccessible";
        }

        public static class MaintenanceProperties
        {
            /// <summary>
            /// Int, optional. Period to retain backups.
            /// </summary>
            public static readonly string propkey_RetentionPeriod = "retentionPeriod";

            /// <summary>
            /// String, optional. If specified, user-selected backup window in
            /// hh24:mi-hh24:mi format.
            /// </summary>
            public static readonly string propkey_BackupWindow = "backupWindow";

            /// <summary>
            /// String, optional. If specified, user-selected maintenance window in
            /// ddd:hh24:mi-ddd:hh24:mi format.
            /// </summary>
            public static readonly string propkey_MaintenanceWindow = "maintenanceWindow";
        }

        public static class ReviewProperties
        {
            /// <summary>
            /// Bool, optional. Used in Modify DB Instance wizard.
            /// </summary>
            public static readonly string propkey_ApplyImmediately = "applyImmediately";

            /// <summary>
            /// Boolean, if true the host environment should open the Instances window
            /// when the wizard closes
            /// </summary>
            public static readonly string propkey_LaunchInstancesViewOnClose = "launchInstancesViewOnClose";
        }
    }
}
