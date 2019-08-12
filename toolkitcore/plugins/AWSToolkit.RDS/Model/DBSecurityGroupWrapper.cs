using System.Collections.ObjectModel;
using System.ComponentModel;
using Amazon.RDS.Model;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Model;

namespace Amazon.AWSToolkit.RDS.Model
{
    public class DBSecurityGroupWrapper : PropertiesModel, IWrapper
    {
        DBSecurityGroup _nativeSecurityGroup;

        public DBSecurityGroupWrapper(DBSecurityGroup securityGroup)
        {
            RefreshNative(securityGroup);
        }

        public void RefreshNative(DBSecurityGroup securityGroup)
        {
            this._nativeSecurityGroup = securityGroup;
            this.PermissionRules.Clear();

            foreach (var iprange in this._nativeSecurityGroup.IPRanges)
            {
                if (iprange.Status.StartsWith("auth"))
                    this.PermissionRules.Add(new PermissionRule(PermissionRule.ConnectionType.CIDR, iprange.CIDRIP));
            }

            foreach (var group in this._nativeSecurityGroup.EC2SecurityGroups)
            {
                if (group.Status.StartsWith("auth"))
                    this.PermissionRules.Add(new PermissionRule(PermissionRule.ConnectionType.EC2SecurityGroup, group.EC2SecurityGroupOwnerId,
                        this._nativeSecurityGroup.VpcId == null ? group.EC2SecurityGroupName : group.EC2SecurityGroupId));
            }

            base.NotifyPropertyChanged("DisplayName");
            base.NotifyPropertyChanged("Description");
            base.NotifyPropertyChanged("OwnerId");
            base.NotifyPropertyChanged("VpcId");
            base.NotifyPropertyChanged("PermissionRules");
        }

        public override void GetPropertyNames(out string className, out string componentName)
        {
            className = this.TypeName;
            componentName = this.DisplayName;
        }

        [Browsable(false)]
        public string TypeName => "DB Security Group";

        [DisplayName("Name")]
        [AssociatedIconAttribute(false, "Amazon.AWSToolkit.RDS.Resources.EmbeddedImages.SecurityGroup.png;AWSToolkit.RDS")]
        public string DisplayName => this._nativeSecurityGroup.DBSecurityGroupName;

        [DisplayName("Description")]
        public string Description => this._nativeSecurityGroup.DBSecurityGroupDescription;

        [DisplayName("Owner ID")]
        public string OwnerId => this._nativeSecurityGroup.OwnerId;

        [DisplayName("VPC ID")]
        public string VpcId => this._nativeSecurityGroup.VpcId;

        ObservableCollection<PermissionRule> _rules = new ObservableCollection<PermissionRule>();
        [Browsable(false)]
        public ObservableCollection<PermissionRule> PermissionRules => this._rules;
    }
}
