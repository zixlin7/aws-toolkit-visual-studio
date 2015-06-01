using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

using Amazon.RDS.Model;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Util;
using Amazon.AWSToolkit.EC2.Model;

namespace Amazon.AWSToolkit.RDS.Model
{
    public class DBInstanceWrapper : PropertiesModel, IWrapper
    {
        public const string DbStatusAvailable = "available";
        readonly DBInstance _nativeDbInstance;
        readonly List<OptionGroup> _optionGroups;

        public DBInstanceWrapper(DBInstance dbInstance)
            : this(dbInstance, new List<OptionGroup>())
        {
        }

        public DBInstanceWrapper(DBInstance dbInstance, List<OptionGroup> optionGroups)
        {
            this._nativeDbInstance = dbInstance;
            this._optionGroups = optionGroups;
        }

        public override void GetPropertyNames(out string className, out string componentName)
        {
            className = this.TypeName;
            componentName = this.DisplayName;
        }

        [Browsable(false)]
        public string TypeName
        {
            get { return "DB Instance"; }
        }

        [Browsable(false)]
        public string DisplayName
        {
            get { return this._nativeDbInstance.DBInstanceIdentifier; }
        }

        [DisplayName("DB Instance")]
        [AssociatedIconAttribute(false, "Amazon.AWSToolkit.RDS.Resources.EmbeddedImages.DBInstances.png;AWSToolkit.RDS")]
        public string DBInstanceIdentifier
        {
            get { return this._nativeDbInstance.DBInstanceIdentifier; }
        }

        [DisplayName("Endpoint")]
        public string Endpoint
        {
            get 
            {
                if (this._nativeDbInstance.Endpoint == null)
                    return "";

                return this._nativeDbInstance.Endpoint.Address; 
            }
        }

        [DisplayName("MasterUsername")]
        public string MasterUsername
        {
            get { return this._nativeDbInstance.MasterUsername; }
        }

        [DisplayName("Port")]
        public int? Port
        {
            get 
            {
                if (this._nativeDbInstance.Endpoint == null)
                    return null;

                return this._nativeDbInstance.Endpoint.Port; 
            }
        }

        [Browsable(false)]
        public bool MultiAZ
        {
            get 
            {
                return this._nativeDbInstance.MultiAZ; 
            }
        }

        [DisplayName("Multi AZ")]
        public string MultiAZFormatted
        {
            get
            {
                if (this._optionGroups != null  && this._optionGroups.FirstOrDefault(
                                                        x => x.Options.FirstOrDefault(y => y.OptionName == RDSConstants.MIRRORING_OPTION_GROUP) != null)
                                                        != null)
                {
                    return "Yes (Mirroring)";
                }

                return this._nativeDbInstance.MultiAZ ? "Yes" : "No";
            }
        }

        [DisplayName("Class")]
        public string DBInstanceClass
        {
            get { return this._nativeDbInstance.DBInstanceClass; }
        }

        [DisplayName("Status")]
        [AssociatedIcon(true, "StatusIcon")]
        public string DBInstanceStatus
        {
            get { return this._nativeDbInstance.DBInstanceStatus; }
        }

        

        [DisplayName("Storage")]
        public string FormattedStorage
        {
            get { return string.Format("{0} GiB", this._nativeDbInstance.AllocatedStorage); }
        }

        [DisplayName("Security Groups")]
        public string FormattedSecurityGroups
        {
            get 
            {
                if (this._nativeDbInstance.VpcSecurityGroups != null && this._nativeDbInstance.VpcSecurityGroups.Any())
                    return StringUtils.CreateCommaDelimitedList<VpcSecurityGroupMembership>(this._nativeDbInstance.VpcSecurityGroups, x => x.VpcSecurityGroupId); 

                return StringUtils.CreateCommaDelimitedList<DBSecurityGroupMembership>(this._nativeDbInstance.DBSecurityGroups, x => x.DBSecurityGroupName); 
            }
        }

        [Browsable(false)]
        public System.Windows.Media.ImageSource StatusIcon
        {
            get
            {
                string iconPath;
                switch (this._nativeDbInstance.DBInstanceStatus.ToLower())
                {
                    case DbStatusAvailable:
                        iconPath = "green-circle.png";
                        break;
                    case "rebooting":
                        iconPath = "blue-circle.png";
                        break;
                    case "deleting":
                        iconPath = "red-circle.png";
                        break;
                    default:
                        iconPath = "yellow-circle.png";
                        break;
                }

                System.Windows.Controls.Image icon;
                icon = IconHelper.GetIcon(iconPath);

                return icon.Source;
            }
        }

        [Browsable(false)]
        public bool IsAvailable
        {
            get 
            { 
                // right now return last-queried state, might want to refresh first
                return string.Compare(this._nativeDbInstance.DBInstanceStatus,
                                      DbStatusAvailable, 
                                      StringComparison.InvariantCultureIgnoreCase) == 0; 
            }
        }

        [DisplayName("Engine")]
        public string Engine
        {
            get { return this._nativeDbInstance.Engine; }
        }

        [DisplayName("Zone")]
        public string AvailabilityZone
        {
            get { return this._nativeDbInstance.AvailabilityZone; }
        }

        [DisplayName("Created Time")]
        public DateTime? InstanceCreateTime
        {
            get 
            {
                if (this._nativeDbInstance.InstanceCreateTime == DateTime.MinValue)
                    return null;

                return this._nativeDbInstance.InstanceCreateTime; 
            }
        }

        [DisplayName("DB Name")]
        public string DBName
        {
            get { return this._nativeDbInstance.DBName; }
        }

        [Browsable(false)]
        public DatabaseTypes DatabaseType
        {
            get
            {
                string engine = this._nativeDbInstance.Engine.ToLower();
                if (engine.StartsWith("sqlserver"))
                    return DatabaseTypes.SQLServer;
                if (engine.StartsWith("oracle"))
                    return DatabaseTypes.Oracle;
                if (engine.StartsWith("mysql"))
                    return DatabaseTypes.MySQL;

                return DatabaseTypes.Unknown;
            }
        }

        [DisplayName("Pending Values")]
        public string PendingValues
        {
            get
            {
                var modifiedValues = new List<string>();

                var pdv = this._nativeDbInstance.PendingModifiedValues;
                if (pdv == null)
                    return "";

                if(pdv.AllocatedStorage != 0)
                    modifiedValues.Add(string.Format("AllocatedStorage: {0}", pdv.AllocatedStorage));
                if(pdv.BackupRetentionPeriod != 0)
                    modifiedValues.Add(string.Format("BackupRetentionPeriod: {0}", pdv.BackupRetentionPeriod));
                if (pdv.DBInstanceClass != null)
                    modifiedValues.Add(string.Format("DBInstanceClass: {0}", pdv.DBInstanceClass));
                if (pdv.EngineVersion != null)
                    modifiedValues.Add(string.Format("EngineVersion: {0}", pdv.EngineVersion));
                if (pdv.MasterUserPassword != null)
                    modifiedValues.Add("MasterUserPassword");
                if (pdv.Port != 0)
                    modifiedValues.Add(string.Format("Port: {0}", pdv.Port));


                return StringUtils.CreateCommaDelimitedList(modifiedValues);
            }
        }

        [Browsable(false)]
        internal DBInstance NativeInstance
        {
            get { return this._nativeDbInstance; }
        }

        public string CreateConnectionString(string dbName, string password)
        {
            var conStr = string.Format("Initial Catalog={0};Data Source={1},{2};User Id={3};Password={4};",
                dbName, this._nativeDbInstance.Endpoint.Address, this._nativeDbInstance.Endpoint.Port, this.MasterUsername, password);

            return conStr;
        }
    }
}
