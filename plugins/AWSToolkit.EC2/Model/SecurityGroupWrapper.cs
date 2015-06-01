using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class SecurityGroupWrapper : PropertiesModel, IWrapper, ITagSupport
    {
        SecurityGroup _securityGroup;
        ObservableCollection<IPPermissionWrapper> _ipIngressPermissions = new ObservableCollection<IPPermissionWrapper>();
        ObservableCollection<IPPermissionWrapper> _ipEgressPermissions = new ObservableCollection<IPPermissionWrapper>();

        public SecurityGroupWrapper(SecurityGroup securityGroup)
        {
            this._securityGroup = securityGroup;
            ReloadIpPermissions(this._securityGroup.IpPermissions, EC2Constants.PermissionType.Ingress);
            ReloadIpPermissions(this._securityGroup.IpPermissionsEgress, EC2Constants.PermissionType.Egrees);
        }

        public override void GetPropertyNames(out string className, out string componentName)
        {
            className = "Security Group";
            componentName = this._securityGroup.GroupName;
        }

        public void ReloadIpPermissions(IList<IpPermission> permissions, EC2Constants.PermissionType permissionType)
        {
            ObservableCollection<IPPermissionWrapper> groupsPermissions;
            if (permissionType == EC2Constants.PermissionType.Ingress)
                groupsPermissions = this._ipIngressPermissions;
            else
                groupsPermissions = this._ipEgressPermissions;

            groupsPermissions.Clear();

            List<IPPermissionWrapper> preSortedCollection = new List<IPPermissionWrapper>();
            foreach (var permission in permissions)
            {
                if (permission.UserIdGroupPairs.Count > 0)
                {
                    foreach (var userGroup in permission.UserIdGroupPairs)
                    {
                        preSortedCollection.Add(new IPPermissionWrapper(permission.IpProtocol, (int)permission.FromPort, (int)permission.ToPort, userGroup.UserId, string.IsNullOrEmpty(userGroup.GroupName) ? userGroup.GroupId : userGroup.GroupName, null));
                    }
                }
                else
                {
                    foreach (var source in permission.IpRanges)
                    {
                        preSortedCollection.Add(new IPPermissionWrapper(permission.IpProtocol, (int)permission.FromPort, (int)permission.ToPort, null, null, source));
                    }
                }
            }

            foreach (var permission in preSortedCollection.OrderBy(x => x.FormattedIPProtocol))
            {
                groupsPermissions.Add(permission);
            }
        }

        [Browsable(false)]
        public SecurityGroup NativeSecurityGroup
        {
            get { return this._securityGroup; }
        }

        [DisplayName("Name")]
        public string DisplayName
        {
            get { return _securityGroup.GroupName; }
        }

        [DisplayName("Group ID")]
        [AssociatedIcon(false, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.security-groups.png")]
        public string GroupId
        {
            get { return _securityGroup.GroupId; }
        }

        [DisplayName("Description")]
        public string GroupDescription
        {
            get { return _securityGroup.Description; }
        }

        [DisplayName("VPC")]
        public string VpcId
        {
            get { return _securityGroup.VpcId; }
        }


        [Browsable(false)]
        public string TypeName
        {
            get { return "Security Group"; }
        }

        [Browsable(false)]
        public ObservableCollection<IPPermissionWrapper> IpIngressPermissions
        {
            get { return this._ipIngressPermissions; }
        }

        [Browsable(false)]
        public ObservableCollection<IPPermissionWrapper> IpEgressPermissions
        {
            get { return this._ipEgressPermissions; }
        }

        [Browsable(false)]
        public System.Windows.Media.ImageSource SecurityGroupIcon
        {
            get
            {
                string iconPath = "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.security-groups.png";

                var icon = IconHelper.GetIcon(this.GetType().Assembly, iconPath);
                return icon.Source;
            }
        }

        public Tag FindTag(string name)
        {
            if (this.NativeSecurityGroup.Tags == null)
                return null;

            return this.NativeSecurityGroup.Tags.FirstOrDefault(x => string.Equals(x.Key, name));
        }

        public void SetTag(string name, string value)
        {
            var tag = FindTag(name);
            if (tag == null)
            {
                tag = new Tag();
                tag.Key = name;
                tag.Value = value;
                this.NativeSecurityGroup.Tags.Add(tag);
            }
            else
            {
                tag.Value = value;
            }
        }

        [Browsable(false)]
        public List<Tag> Tags
        {
            get { return this.NativeSecurityGroup.Tags; }
        }

    }
}
