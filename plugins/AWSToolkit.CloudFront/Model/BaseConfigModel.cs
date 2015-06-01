using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.Util;
using Amazon.AWSToolkit.CommonUI;

using Amazon.CloudFront.Model;

namespace Amazon.AWSToolkit.CloudFront.Model
{
    public class BaseConfigModel : BaseModel
    {
        public Visibility IsNewVisiblity
        {
            get 
            {
                if (IsNew)
                    return Visibility.Visible;
                else
                    return Visibility.Hidden;
            }
        }

        public bool IsNew
        {
            get { return this.CallerReference == null; }
        }

        public Visibility IsExistingVisiblity
        {
            get
            {
                if (IsExisting)
                    return Visibility.Visible;
                else
                    return Visibility.Hidden;
            }
        }

        public bool IsExisting
        {
            get { return this.CallerReference != null; }
        }

        string _callerReference;
        public string CallerReference
        {
            get { return this._callerReference; }
            set
            {
                this._callerReference = value;
                base.NotifyPropertyChanged("CallerRefernce");
                base.NotifyPropertyChanged("IsNew");
                base.NotifyPropertyChanged("IsExisting");
            }
        }

        string _comment;
        public string Comment
        {
            get { return this._comment; }
            set
            {
                this._comment = value;
                base.NotifyPropertyChanged("Comment");
            }
        }

        bool _enabled = true;
        public bool Enabled
        {
            get { return this._enabled; }
            set
            {
                this._enabled = value;
                base.NotifyPropertyChanged("Enabled");
            }
        }

        PriceClass _priceClass = PriceClass.PriceClassAll;
        public PriceClass SelectedPriceClass
        {
            get { return this._priceClass; }
            set
            {
                this._priceClass = value;
                base.NotifyPropertyChanged("SelectedPriceClass");
            }
        }

        public IEnumerable<PriceClass> AllAvailablePriceClasses
        {
            get { return PriceClass.AllPriceClass; }
        }

        string _s3BucketOrigin;
        public string S3BucketOrigin
        {
            get { return this._s3BucketOrigin; }
            set
            {
                this._s3BucketOrigin = value;
                base.NotifyPropertyChanged("S3BucketOrigin");
            }
        }

        string _canonicalUserId;
        public string CanonicalUserId
        {
            get { return this._canonicalUserId; }
            set
            {
                this._canonicalUserId = value;
                base.NotifyPropertyChanged("CanonicalUserId");
            }
        }

        bool _isPrivateDistributionEnabled;
        public bool IsPrivateDistributionEnabled
        {
            get { return this._isPrivateDistributionEnabled; }
            set
            {
                this._isPrivateDistributionEnabled = value;
                base.NotifyPropertyChanged("IsPrivateDistributionEnabled");
            }
        }

        OriginAccessIdentitiesWrapper _originAccessIdentityWrapper;
        public OriginAccessIdentitiesWrapper SelectedOriginAccessIdentityWrapper
        {
            get { return this._originAccessIdentityWrapper; }
            set
            {
                if (value == null)
                {
                    this._canonicalUserId = null;
                    this._originAccessIdentityWrapper = null;
                }
                else
                {
                    this._canonicalUserId = value.CanonicalUserId;
                    this._originAccessIdentityWrapper = value;
                }

                base.NotifyPropertyChanged("SelectedOriginAccessIdentityWrapper");
                base.NotifyPropertyChanged("CanonicalUserId");
            }
        }

        public OriginAccessIdentitiesWrapper GetOriginAccessIdentity()
        {
            if (string.IsNullOrEmpty(this.CanonicalUserId))
                return null;

            foreach (OriginAccessIdentitiesWrapper oai in this.AllOriginAccessIdentities)
            {
                if (oai.CanonicalUserId.Equals(this.CanonicalUserId))
                    return oai;
            }
            return null;
        }

        public OriginAccessIdentitiesWrapper FindOriginAccessIdentityById(string id)
        {
            foreach (OriginAccessIdentitiesWrapper oai in this.AllOriginAccessIdentities)
            {
                if (id.EndsWith("/" + oai.Id))
                    return oai;
            }
            return null;

        }

        bool _trustedSignerSelf;
        public bool TrustedSignerSelf
        {
            get { return this._trustedSignerSelf; }
            set
            {
                this._trustedSignerSelf = value;
                base.NotifyPropertyChanged("TrustedSignerSelf");
            }
        }

        ObservableCollection<MutableString> _trustedSignerAWSAccountIds = new ObservableCollection<MutableString>();
        public ObservableCollection<MutableString> TrustedSignerAWSAccountIds
        {
            get { return this._trustedSignerAWSAccountIds; }
            set
            {
                this._trustedSignerAWSAccountIds = value;
                base.NotifyPropertyChanged("TrustedSignerAWSAccountIds");
            }
        }

        ObservableCollection<MutableString> _cnames = new ObservableCollection<MutableString>();
        public ObservableCollection<MutableString> CNAMEs
        {
            get { return this._cnames; }
            set
            {
                this._cnames = value;
                base.NotifyPropertyChanged("CNAMEs");
            }
        }

        bool _isLoggingEnabled;
        public bool IsLoggingEnabled
        {
            get { return this._isLoggingEnabled; }
            set
            {
                this._isLoggingEnabled = value;
                base.NotifyPropertyChanged("IsLoggingEnabled");
            }
        }

        string _loggingTargetBucket;
        public string LoggingTargetBucket
        {
            get { return this._loggingTargetBucket; }
            set
            {
                this._loggingTargetBucket = value;
                base.NotifyPropertyChanged("LoggingTargetBucket");
            }
        }

        public string GetLoggingTargetBucketWithPostFix()
        {
            if (LoggingTargetBucket == null)
                return string.Empty;

            string bucket = LoggingTargetBucket.Trim();
            if (bucket.EndsWith("amazonaws.com"))
                return bucket;

            return bucket + ".s3.amazonaws.com";
        }

        string _loggingTargetPrefix;
        public string LoggingTargetPrefix
        {
            get { return this._loggingTargetPrefix; }
            set
            {
                this._loggingTargetPrefix = value;
                base.NotifyPropertyChanged("LoggingTargetPrefix");
            }
        }

        bool _isLoggingCookiesEnabled;
        public bool IsLoggingCookiesEnabled
        {
            get { return this._isLoggingCookiesEnabled; }
            set
            {
                this._isLoggingCookiesEnabled = value;
                base.NotifyPropertyChanged("IsLoggingCookiesEnabled");
            }
        }

        ObservableCollection<string> _allBucketNames = new ObservableCollection<string>();
        public ObservableCollection<string> AllBucketNames
        {
            get { return this._allBucketNames; }
            set
            {
                this._allBucketNames = value;
                base.NotifyPropertyChanged("AllBucketNames");
            }
        }

        ObservableCollection<string> _allCanonicalUserIds = new ObservableCollection<string>();
        public ObservableCollection<string> AllCanonicalUserIds
        {
            get { return this._allCanonicalUserIds; }
            private set
            {
                this._allCanonicalUserIds = value;
                base.NotifyPropertyChanged("AllCanonicalUserIds");
            }
        }

        ObservableCollection<OriginAccessIdentitiesWrapper> _allOriginAccessIdentities = new ObservableCollection<OriginAccessIdentitiesWrapper>();
        public ObservableCollection<OriginAccessIdentitiesWrapper> AllOriginAccessIdentities
        {
            get { return this._allOriginAccessIdentities; }
            private set
            {
                this._allOriginAccessIdentities = value;
                base.NotifyPropertyChanged("AllOriginAccessIdentities");
            }
        }

        public void AddCloudFrontOriginAccessIdentity(CloudFrontOriginAccessIdentitySummary identity)
        {
            var wrapper = new OriginAccessIdentitiesWrapper(identity.Id, identity.Comment, identity.S3CanonicalUserId);
            this.AllOriginAccessIdentities.Add(wrapper);
            this.AllCanonicalUserIds.Add(identity.S3CanonicalUserId);
        }

        public class PriceClass
        {
            public static readonly PriceClass PriceClassAll = new PriceClass("PriceClass_All", "Use All Edge Locations (Best Performance)");
            public static readonly PriceClass PriceClass100 = new PriceClass("PriceClass_100", "Use Only US and Europe");
            public static readonly PriceClass PriceClass200 = new PriceClass("PriceClass_200", "Use Only US, Europe and Asia");

            public static IEnumerable<PriceClass> AllPriceClass
            {
                get { return new PriceClass[] { PriceClassAll, PriceClass200, PriceClass100 }; }
            }


            private PriceClass(string value, string displayName)
            {
                this.DisplayName = displayName;
                this.Value = value;
            }

            public string DisplayName
            {
                get;
                private set;
            }

            public string Value
            {
                get;
                private set;
            }

            public static PriceClass FindPriceClass(string value)
            {
                foreach (var cls in AllPriceClass)
                {
                    if (string.Equals(cls.Value, value, StringComparison.InvariantCultureIgnoreCase))
                        return cls;
                }

                return new PriceClass(value, value);
            }
        }
    }
}
