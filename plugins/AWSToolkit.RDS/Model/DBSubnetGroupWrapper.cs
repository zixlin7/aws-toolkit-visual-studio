using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.RDS.Model;

namespace Amazon.AWSToolkit.RDS.Model
{
    public class DBSubnetGroupWrapper : PropertiesModel
    {
        public const string DbSubnetGroupAvailable = "complete";
        private readonly DBSubnetGroup _nativeDbSubnetGroup;

        public DBSubnetGroupWrapper(DBSubnetGroup nativeDbSubnetGroup)
        {
            this._nativeDbSubnetGroup = nativeDbSubnetGroup;
        }

        public override void GetPropertyNames(out string className, out string componentName)
        {
            className = this.TypeName;
            componentName = this.Name;
        }

        [Browsable(false)]
        public string TypeName
        {
            get { return "DB Subnet Group"; }
        }

        [Browsable(false)]
        public string NameAndDescription
        {
            get
            {
                if (!string.IsNullOrEmpty(this._nativeDbSubnetGroup.DBSubnetGroupDescription))
                    return string.Format("{0} - {1}", 
                                         this._nativeDbSubnetGroup.DBSubnetGroupName,
                                         this._nativeDbSubnetGroup.DBSubnetGroupDescription);

                return this._nativeDbSubnetGroup.DBSubnetGroupName;
            }
        }

        public string DBSubnetGroupIdentifier
        {
            get { return this._nativeDbSubnetGroup.DBSubnetGroupName; }
        }

        [Browsable(false)]
        internal DBSubnetGroup NativeSubnetGroup
        {
            get { return this._nativeDbSubnetGroup; }
        }

        [DisplayName("Name")]
        [AssociatedIconAttribute(false, "Amazon.AWSToolkit.RDS.Resources.EmbeddedImages.DBSubnetGroups.png;AWSToolkit.RDS")]
        public string Name
        {
            get { return NativeSubnetGroup.DBSubnetGroupName; }    
        }

        [DisplayName("Description")]
        public string Description
        {
            get { return NativeSubnetGroup.DBSubnetGroupDescription; }
        }

        [DisplayName("Status")]
        [AssociatedIcon(true, "StatusIcon")]
        public string Status
        {
            get { return NativeSubnetGroup.SubnetGroupStatus; }
        }

        [DisplayName("VPC ID")]
        public string VpcId
        {
            get { return NativeSubnetGroup.VpcId; }
        }

        [Browsable(false)]
        public System.Windows.Media.ImageSource StatusIcon
        {
            get
            {
                string iconPath;
                switch (NativeSubnetGroup.SubnetGroupStatus.ToLower())
                {
                    case DbSubnetGroupAvailable:
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

                var icon = IconHelper.GetIcon(iconPath);

                return icon.Source;
            }
        }


    }
}
