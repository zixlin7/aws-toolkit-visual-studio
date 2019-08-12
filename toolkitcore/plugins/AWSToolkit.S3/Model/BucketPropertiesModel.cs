using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Amazon.S3.Model;
using Amazon.AWSToolkit.PolicyEditor.Model;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.S3.Model
{
    public class BucketPropertiesModel : BaseModel
    {
        string _bucketName;

        ObservableCollection<Permission> _permissions = new ObservableCollection<Permission>();
        List<Permission> _originalPermissions = new List<Permission>();

        ObservableCollection<LifecycleRuleModel> _lifecycleRules = new ObservableCollection<LifecycleRuleModel>();
        List<LifecycleRuleModel> _originalLifecycleRules = new List<LifecycleRuleModel>();

        ObservableCollection<EventConfigurationModel> _eventConfigurations = new ObservableCollection<EventConfigurationModel>();
        List<EventConfigurationModel> _originalEventConfigurations = new List<EventConfigurationModel>();

        bool _hasLoggingChanged;
        bool _hasNotificationsChanged;
        bool _hasWebSiteChanged;

        bool _isLoggingEnabled;
        string _loggingTargetBucket;
        string _loggintTargetPrefix;

        bool _isWebSiteEnabled;
        string _webSiteIndexDocument;
        string _webSiteErrorDocument;
        string _webSiteEndPoint;

        Owner _bucketOwner;
        DateTime? _creationDate;
        string _regionDisplayName;
        string _regionSystemName;

        public BucketPropertiesModel()
        {
            this.PropertyChanged += onPropertyChanged;
            this.PermissionEntries.CollectionChanged += onCollectionChanged;
            this.LifecycleRules.CollectionChanged += onCollectionChanged;
        }

        void onCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.IsDirty = true;
        }

        void onPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!string.Equals("IsDirty", e.PropertyName))
            {
                this.IsDirty = true;
            }
        }

        bool _isDirty;
        public bool IsDirty
        {
            get => this._isDirty;
            set
            {
                if (!value && this._policyModel != null)
                {
                    this._policyModel.IsDirty = false;
                }

                this._isDirty = value;
                base.NotifyPropertyChanged("IsDirty");
            }
        }

        public BucketPropertiesModel(string bucketName)
            : this()
        {
            this._bucketName = bucketName;
        }

        public bool HasLoggingChanged => this._hasLoggingChanged;

        public bool HasWebSiteChanged => this._hasWebSiteChanged;

        public bool HasNotificationsChanged => this._hasNotificationsChanged;

        public bool HasPermissionsChanged => !Permission.IsDifferent(this._originalPermissions, this._permissions);

        public bool HasLifecycleRulesChanged => !LifecycleRuleModel.IsDifferent(this._originalLifecycleRules, this._lifecycleRules);

        public void CommitState()
        {
            this._hasLoggingChanged = false;
            this._hasNotificationsChanged = false;
            this._hasWebSiteChanged = false;

            this._originalLifecycleRules = new List<LifecycleRuleModel>();
            foreach (var entry in this.LifecycleRules)
            {
                this._originalLifecycleRules.Add(entry.Clone() as LifecycleRuleModel);
            }

            this._originalPermissions = new List<Permission>();
            foreach (var entry in this.PermissionEntries)
            {
                this._originalPermissions.Add(entry.Clone() as Permission);
            }

            this._originalEventConfigurations = new List<EventConfigurationModel>();
            foreach (var entry in this.EventConfigurations)
            {
                this._originalEventConfigurations.Add(entry.Clone() as EventConfigurationModel);
            }

            this.PolicyModel.IsDirty = false;
            this.IsDirty = false;            
        }

        public string RegionDisplayName
        {
            get => this._regionDisplayName;
            set
            {
                this._regionDisplayName = value;
                base.NotifyPropertyChanged("RegionDisplayName");
            }
        }

        public string RegionSystemName
        {
            get => this._regionSystemName;
            set
            {
                this._regionSystemName = value;
                base.NotifyPropertyChanged("RegionSystemName");
            }
        }

        public string BucketName
        {
            get => this._bucketName;
            set 
            { 
                this._bucketName = value;
                base.NotifyPropertyChanged("BucketName");
            }
        }

        public Owner BucketOwner
        {
            get => this._bucketOwner;
            set
            {
                this._bucketOwner = value;
                base.NotifyPropertyChanged("BucketOwner");
            }
        }

        public List<string> AllBucketNames
        {
            get;
            set;
        }

        public List<string> PossibleTopics
        {
            get;
            set;
        }

        public DateTime? CreationDate
        {
            get => this._creationDate;
            set
            {
                this._creationDate = value;
                base.NotifyPropertyChanged("CreationDate");
            }
        }

        public ObservableCollection<Permission> PermissionEntries
        {
            get => this._permissions;
            set => this._permissions = value;
        }

        public ObservableCollection<LifecycleRuleModel> LifecycleRules
        {
            get => this._lifecycleRules;
            set => this._lifecycleRules = value;
        }

        public ObservableCollection<EventConfigurationModel> EventConfigurations
        {
            get => this._eventConfigurations;
            set => this._eventConfigurations = value;
        }

        public bool IsLoggingEnabled
        {
            get => this._isLoggingEnabled;
            set
            {
                this._isLoggingEnabled = value;
                this._hasLoggingChanged = true;
                base.NotifyPropertyChanged("IsLoggingEnabled");
            }
        }

        public string LoggingTargetBucket
        {
            get => this._loggingTargetBucket;
            set
            {
                this._loggingTargetBucket = value;
                this._hasLoggingChanged = true;
                base.NotifyPropertyChanged("LoggingTargetBucket");
            }
        }

        public string LoggingTargetPrefix
        {
            get => this._loggintTargetPrefix;
            set
            {
                this._loggintTargetPrefix = value;
                this._hasLoggingChanged = true;
                base.NotifyPropertyChanged("LoggingTargetPrefix");
            }
        }

        public bool IsWebSiteEnabled
        {
            get => this._isWebSiteEnabled;
            set
            {
                this._isWebSiteEnabled = value;
                this._hasLoggingChanged = true;
                base.NotifyPropertyChanged("IsWebSiteEnabled");
                base.NotifyPropertyChanged("WebSiteEndPoint");
            }
        }

        public string WebSiteIndexDocument
        {
            get => this._webSiteIndexDocument;
            set
            {
                this._webSiteIndexDocument = value;
                this._hasWebSiteChanged = true;
                base.NotifyPropertyChanged("WebSiteIndexDocument");
            }
        }

        public string WebSiteErrorDocument
        {
            get => this._webSiteErrorDocument;
            set
            {
                this._webSiteErrorDocument = value;
                this._hasWebSiteChanged = true;
                base.NotifyPropertyChanged("WebSiteErrorDocument");
            }
        }

        public string WebSiteEndPoint
        {
            get 
            {
                if (!this.IsWebSiteEnabled)
                    return string.Empty;

                return this._webSiteEndPoint; 
            }
            set => this._webSiteEndPoint = value;
        }

        PolicyModel _policyModel;
        public PolicyModel PolicyModel
        {
            get => this._policyModel;
            set
            {
                this._policyModel = value;
                base.NotifyPropertyChanged("PolicyModel");
            }
        }
    }
}
