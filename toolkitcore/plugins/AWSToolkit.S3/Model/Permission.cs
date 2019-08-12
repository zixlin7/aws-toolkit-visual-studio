using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;
using Amazon.S3;
using Amazon.S3.Model;

namespace Amazon.AWSToolkit.S3.Model
{
    public class Permission : BaseModel, ICloneable
    {
        public enum PermissionMode { Bucket, Object }

        public Permission()
        {
            this.Grantee = new S3Grantee();
        }

        public Permission(S3Grantee grantee)
        {
            this.Grantee = grantee;
        }

        public static void LoadPermissions(IList<Permission> permissionEntries, S3AccessControlList acl)
        {
            Dictionary<string, Permission> permissions = new Dictionary<string, Permission>();
            foreach (var grant in acl.Grants)
            {
                string key = GetUniqueName(grant.Grantee);
                Permission permission;
                if (!permissions.TryGetValue(key, out permission))
                {
                    permission = new Permission(grant.Grantee);
                    permissions[key] = permission;
                }

                if (grant.Permission == S3Permission.READ)
                {
                    permission.OpenDownload = true;
                    permission.List = true;
                }
                else if (grant.Permission == S3Permission.WRITE)
                {
                    permission.UploadAndDelete = true;
                }
                else if (grant.Permission == S3Permission.READ_ACP)
                {
                    permission.ViewPermissions = true;
                }
                else if (grant.Permission == S3Permission.WRITE_ACP)
                {
                    permission.EditPermissions = true;
                }
                else if (grant.Permission == S3Permission.FULL_CONTROL)
                {
                    permission.OpenDownload = true;
                    permission.ViewPermissions = true;
                    permission.EditPermissions = true;
                    permission.List = true;
                    permission.UploadAndDelete = true;
                }

            }

            foreach (var entry in permissions.Values.OrderBy(item => item.GranteeFormatted))
            {
                permissionEntries.Add(entry);
            }
        }

        private static string GetUniqueName(S3Grantee grantee)
        {
            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrEmpty(grantee.EmailAddress))
            {
                sb.Append(System.String.Concat("EmailAddress:", grantee.EmailAddress));
            }
            if (!string.IsNullOrEmpty(grantee.URI))
            {
                sb.Append(System.String.Concat("URI:", grantee.URI));
            }
            if (!string.IsNullOrEmpty(grantee.CanonicalUser))
            {
                sb.Append(System.String.Concat("ID:", grantee.CanonicalUser, " DisplayName:", grantee.DisplayName));
            }

            return sb.ToString();
        }

        public static S3AccessControlList ConvertToAccessControlList(IList<Permission> permissionEntries, PermissionMode mode)
        {
            return ConvertToAccessControlList(permissionEntries, mode, false);
        }

        public static S3AccessControlList ConvertToAccessControlList(IList<Permission> permissionEntries, PermissionMode mode, bool makePublicRead)
        {
            S3AccessControlList list = new S3AccessControlList();

            bool foundPublicReadPermission = false;
            foreach (var entry in permissionEntries)
            {
                if (entry.OpenDownload)
                {
                    if (CommonURIGrantee.ALL_USERS_URI.Equals(entry.Grantee.URI))
                        foundPublicReadPermission = true;

                    list.AddGrant(entry.Grantee, S3Permission.READ);
                }
                if (entry.ViewPermissions)
                {
                    list.AddGrant(entry.Grantee, S3Permission.READ_ACP);
                }
                if (entry.EditPermissions)
                {
                    list.AddGrant(entry.Grantee, S3Permission.WRITE_ACP);
                }
                if (mode == PermissionMode.Bucket && entry.List)
                {
                    list.AddGrant(entry.Grantee, S3Permission.READ);
                }
                if (mode == PermissionMode.Bucket && entry.UploadAndDelete)
                {
                    list.AddGrant(entry.Grantee, S3Permission.WRITE);
                }
            }

            if (makePublicRead && !foundPublicReadPermission)
            {
                list.AddGrant(CommonURIGrantee.ALL_USERS_URI.Grantee, S3Permission.READ);
            }


            return list;
        }

        public static bool IsDifferent(IList<Permission> orignal, IList<Permission> permissions)
        {
            if (orignal.Count != permissions.Count)
                return false;

            for (int i = 0; i < orignal.Count; i++)
            {
                if (orignal[i].List != permissions[i].List)
                    return false;
                if (orignal[i].UploadAndDelete != permissions[i].UploadAndDelete)
                    return false;
                if (orignal[i].OpenDownload != permissions[i].OpenDownload)
                    return false;
                if (orignal[i].ViewPermissions != permissions[i].ViewPermissions)
                    return false;
                if (orignal[i].EditPermissions != permissions[i].EditPermissions)
                    return false;
                if (!orignal[i].Grantee.Equals(permissions[i].Grantee))
                    return false;
            }

            return true;
        }

        S3Grantee _grantee;
        public S3Grantee Grantee
        {
            get => this._grantee;
            set
            {
                this._grantee = value;
                base.NotifyPropertyChanged("Grantee");
            }
        }

        public string GranteeFormatted
        {
            get
            {
                string displayName = string.Empty;
                if (!string.IsNullOrEmpty(this.Grantee.URI))
                {
                    CommonURIGrantee grantee = CommonURIGrantee.FindByURI(this.Grantee.URI);
                    if (grantee != null)
                    {
                        displayName = grantee.Label;
                    }
                    else
                    {
                        displayName = this.Grantee.URI;
                    }
                }
                else if (!string.IsNullOrEmpty(this.Grantee.EmailAddress))
                {
                    displayName = this.Grantee.EmailAddress;
                }
                else if (this.Grantee.CanonicalUser != null)
                {
                    displayName = this.Grantee.DisplayName;
                }

                return displayName;
            }
            set
            {
                CommonURIGrantee grantee = CommonURIGrantee.FindByLabel(value);
                if (grantee != null)
                {
                    this.Grantee.URI = grantee.URI;
                    this.Grantee.EmailAddress = null;
                    this.Grantee.CanonicalUser = null;
                }
                else if (value.Contains('@'))
                {
                    this.Grantee.URI = null;
                    this.Grantee.EmailAddress = value;
                    this.Grantee.CanonicalUser = null;
                }
                else
                {
                    this.Grantee.URI = null;
                    this.Grantee.EmailAddress = null;
                    this.Grantee.CanonicalUser = value;                    
                }

                base.NotifyPropertyChanged("GranteeFormatted");
            }
        }

        bool _list;
        public bool List
        {
            get => this._list;
            set
            {
                this._list = value;
                base.NotifyPropertyChanged("List");
            }
        }

        bool _uploadAndDelete;
        public bool UploadAndDelete
        {
            get => this._uploadAndDelete;
            set
            {
                this._uploadAndDelete = value;
                base.NotifyPropertyChanged("UploadAndDelete");
            }
        }

        bool _openDownload;
        public bool OpenDownload
        {
            get => this._openDownload;
            set
            {
                this._openDownload = value;
                base.NotifyPropertyChanged("OpenDownload");
            }
        }

        bool _viewPermissions;
        public bool ViewPermissions
        {
            get => this._viewPermissions;
            set
            {
                this._viewPermissions = value;
                base.NotifyPropertyChanged("ViewPermissions");
            }
        }

        bool _editPermissions;
        public bool EditPermissions
        {
            get => this._editPermissions;
            set
            {
                this._editPermissions = value;
                base.NotifyPropertyChanged("EditPermissions");
            }
        }

        public object Clone()
        {
            var perm = new Permission()
            {                
                Grantee = new S3Grantee()
                {
                    CanonicalUser = this.Grantee.CanonicalUser,
                    EmailAddress = this.Grantee.EmailAddress,
                    URI = this.Grantee.URI
                },
                List = this.List,
                UploadAndDelete = this.UploadAndDelete,
                OpenDownload = this.OpenDownload,
                ViewPermissions = this.ViewPermissions,
                EditPermissions = this.EditPermissions
            };
            return perm;
        }

        public class CommonURIGrantee
        {
            public static CommonURIGrantee LOG_DELIVER_URI = new CommonURIGrantee("Log Delivery", "http://acs.amazonaws.com/groups/s3/LogDelivery");
            public static CommonURIGrantee ALL_USERS_URI = new CommonURIGrantee("Everyone", "http://acs.amazonaws.com/groups/global/AllUsers");
            public static CommonURIGrantee AUTHENTICATED_USERS_URI = new CommonURIGrantee("Authenticated Users", "http://acs.amazonaws.com/groups/global/AuthenticatedUsers");

            static Dictionary<string, CommonURIGrantee> _findByLabel;
            static Dictionary<string, CommonURIGrantee> _findByURI;

            static CommonURIGrantee()
            {
                _findByLabel = new Dictionary<string, CommonURIGrantee>();
                _findByLabel[LOG_DELIVER_URI.Label] = LOG_DELIVER_URI;
                _findByLabel[ALL_USERS_URI.Label] = ALL_USERS_URI;
                _findByLabel[AUTHENTICATED_USERS_URI.Label] = AUTHENTICATED_USERS_URI;

                _findByURI = new Dictionary<string, CommonURIGrantee>();
                _findByURI[LOG_DELIVER_URI.URI] = LOG_DELIVER_URI;
                _findByURI[ALL_USERS_URI.URI] = ALL_USERS_URI;
                _findByURI[AUTHENTICATED_USERS_URI.URI] = AUTHENTICATED_USERS_URI;
            }

            private CommonURIGrantee(string label, string uri)
            {
                this.Label = label;
                this.URI = uri;
            }

            public static CommonURIGrantee FindByLabel(string label)
            {
                CommonURIGrantee grantee = null;
                _findByLabel.TryGetValue(label, out grantee);
                return grantee;
            }

            public static CommonURIGrantee FindByURI(string label)
            {
                CommonURIGrantee grantee = null;
                _findByURI.TryGetValue(label, out grantee);
                return grantee;
            }

            public string Label
            {
                get;
                set;
            }

            public string URI
            {
                get;
                set;
            }

            public S3Grantee Grantee => new S3Grantee() { URI = this.URI };
        }
    }
}
