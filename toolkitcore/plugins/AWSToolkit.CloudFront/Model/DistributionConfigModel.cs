using System;
using Amazon.AWSToolkit.Util;
using Amazon.CloudFront;
using Amazon.CloudFront.Model;

namespace Amazon.AWSToolkit.CloudFront.Model
{
    public class DistributionConfigModel : BaseConfigModel
    {
        DistributionConfig _lastLoadedDistributionConfig;


        bool _requireHTTPS;
        public bool RequireHTTPS
        {
            get => this._requireHTTPS;
            set
            {
                this._requireHTTPS = value;
                base.NotifyPropertyChanged("RequireHTTPS");
            }
        }

        string _defaultRootObject;
        public string DefaultRootObject
        {
            get => this._defaultRootObject;
            set
            {
                this._defaultRootObject = value;
                base.NotifyPropertyChanged("DefaultRootObject");
            }
        }

        bool _s3OriginSelected = true;
        public bool S3OriginSelected
        {
            get => this._s3OriginSelected;
            set
            {
                this._s3OriginSelected = value;
                this._customOriginSelected = !value;
                base.NotifyPropertyChanged("S3OriginSelected");
                base.NotifyPropertyChanged("CustomOriginSelected");
            }
        }

        bool _customOriginSelected;
        public bool CustomOriginSelected
        {
            get => this._customOriginSelected;
            set
            {
                this._customOriginSelected = value;
                this._s3OriginSelected = !value;
                base.NotifyPropertyChanged("S3OriginSelected");
                base.NotifyPropertyChanged("CustomOriginSelected");
            }
        }


        string _customOriginDNSName;
        public string CustomOriginDNSName
        {
            get => this._customOriginDNSName;
            set
            {
                this._customOriginDNSName = value;
                base.NotifyPropertyChanged("CustomOriginDNSName");
            }
        }

        string _httpPort = "80";
        public string HttpPort
        {
            get => this._httpPort;
            set
            {
                this._httpPort = value;
                base.NotifyPropertyChanged("HttpPort");
            }
        }

        string _httpsPort = "443";
        public string HttpsPort
        {
            get => this._httpsPort;
            set
            {
                this._httpsPort = value;
                base.NotifyPropertyChanged("HttpsPort");
            }
        }

        bool _httpOnly;
        public bool HttpOnly
        {
            get => this._httpOnly;
            set
            {
                if (this._httpOnly == value)
                    return;

                this._httpOnly = value;
                this._matchViewer = !value;
                base.NotifyPropertyChanged("HttpOnly");
                base.NotifyPropertyChanged("MatchViewer");
            }
        }

        bool _matchViewer = true;
        public bool MatchViewer
        {
            get => this._matchViewer;
            set
            {
                if (this._matchViewer == value)
                    return;

                this._matchViewer = value;
                this._httpOnly = !value;
                base.NotifyPropertyChanged("HttpOnly");
                base.NotifyPropertyChanged("MatchViewer");
            }
        }



        public void LoadDistributionConfig(DistributionConfig config)
        {
            this._lastLoadedDistributionConfig = config;

            this.Comment = config.Comment;
            this.Enabled = config.Enabled;
            this.DefaultRootObject = config.DefaultRootObject;
            if (config.PriceClass != null)
                this.SelectedPriceClass = PriceClass.FindPriceClass(config.PriceClass);

            this.RequireHTTPS = string.Equals(config.DefaultCacheBehavior.ViewerProtocolPolicy, "https-only", StringComparison.InvariantCultureIgnoreCase);
            this.CallerReference = config.CallerReference;

            if (config.Origins.Items.Count > 0)
            {
                var origin = config.Origins.Items[0];

                if (config.Origins.Items[0].S3OriginConfig != null)
                {
                    this.S3OriginSelected = true;
                    this.CustomOriginSelected = false;
                    this.S3BucketOrigin = origin.DomainName;
                    if (origin.S3OriginConfig.OriginAccessIdentity != null)
                    {
                        var oai = FindOriginAccessIdentityById(origin.S3OriginConfig.OriginAccessIdentity);
                        if (oai != null)
                        {
                            this.CanonicalUserId = oai.CanonicalUserId;
                            this.SelectedOriginAccessIdentityWrapper = oai;
                        }
                        this.IsPrivateDistributionEnabled = true;
                    }
                    else
                    {
                        this.IsPrivateDistributionEnabled = false;
                    }

                    this.HttpPort = string.Empty;
                    this.HttpsPort = string.Empty;
                    this.MatchViewer = false;
                    this.HttpOnly = false;
                }
                else
                {
                    this.S3OriginSelected = false;
                    this.CustomOriginSelected = true;

                    this.CustomOriginDNSName = origin.DomainName;
                    this.HttpPort = origin.CustomOriginConfig.HTTPPort.ToString();
                    this.HttpsPort = origin.CustomOriginConfig.HTTPSPort.ToString();
                    if (string.Equals(origin.CustomOriginConfig.OriginProtocolPolicy, "match-viewer", StringComparison.InvariantCultureIgnoreCase))
                        this.MatchViewer = true;
                    else
                        this.HttpOnly = true;
                }
            }

            if (config.Logging != null)
            {
                this.IsLoggingEnabled = config.Logging.Enabled;
                this.LoggingTargetBucket = config.Logging.Bucket;
                this.LoggingTargetPrefix = config.Logging.Prefix;
                this.IsLoggingCookiesEnabled = config.Logging.IncludeCookies;
            }

            this.TrustedSignerAWSAccountIds.Clear();
            this.IsPrivateDistributionEnabled = false;
            if (config.DefaultCacheBehavior.TrustedSigners != null)
            {
                this.IsPrivateDistributionEnabled = config.DefaultCacheBehavior.TrustedSigners.Items.Count > 0;
                foreach (var account in config.DefaultCacheBehavior.TrustedSigners.Items)
                {
                    if(string.Equals(account, "self", StringComparison.InvariantCultureIgnoreCase))
                        this.TrustedSignerSelf = true;
                    else
                        this.TrustedSignerAWSAccountIds.Add(new MutableString(account));
                }
            }

            this.CNAMEs.Clear();
            if (config.Aliases != null)
            {
                foreach (string cname in config.Aliases.Items)
                {
                    this.CNAMEs.Add(new MutableString(cname));
                }
            }
        }

        public DistributionConfig ConvertToDistribtionConfig()
        {
            DistributionConfig config = new DistributionConfig()
            {
                Comment = this.Comment,
                Enabled = this.Enabled,
                DefaultRootObject = this.DefaultRootObject
            };

            if (config.Comment == null)
                config.Comment = "";
            if (config.DefaultRootObject == null)
                config.DefaultRootObject = "";

            if (this.CallerReference == null)
                this.CallerReference = DateTime.Now.Ticks.ToString();
            config.CallerReference = this.CallerReference;

            // Since we don't currently support editing origins in the toolkit lets just take the origins stored in the last loaded config 
            if (this._lastLoadedDistributionConfig != null)
            {
                config.Origins = this._lastLoadedDistributionConfig.Origins;
                config.ViewerCertificate = this._lastLoadedDistributionConfig.ViewerCertificate;
                config.Restrictions = this._lastLoadedDistributionConfig.Restrictions;
            }
            else
            {
                config.Origins = new Origins();
                if (this.S3OriginSelected)
                {
                    var origin = new Origin() { DomainName = this.S3BucketOrigin, Id = "1" };
                    config.Origins.Items.Add(origin);
                    if (!origin.DomainName.EndsWith(".s3.amazonaws.com"))
                        origin.DomainName += ".s3.amazonaws.com";

                    origin.S3OriginConfig = new S3OriginConfig();
                    if (this.IsPrivateDistributionEnabled)
                    {
                        var wrapper = this.GetOriginAccessIdentity();
                        if (wrapper != null)
                        {
                            origin.S3OriginConfig.OriginAccessIdentity = "origin-access-identity/cloudfront/" + wrapper.Id;
                        }
                    }
                    if (origin.S3OriginConfig.OriginAccessIdentity == null)
                        origin.S3OriginConfig.OriginAccessIdentity = "";
                }
                else
                {

                    int httpPort = 80;
                    int.TryParse(this.HttpPort, out httpPort);
                    int httpsPort = 443;
                    int.TryParse(this.HttpsPort, out httpsPort);

                    var origin = new Origin() { DomainName = this.CustomOriginDNSName, Id = "1" };
                    config.Origins.Items.Add(origin);

                    origin.CustomOriginConfig = new CustomOriginConfig();
                    origin.CustomOriginConfig.HTTPPort = httpPort;
                    origin.CustomOriginConfig.HTTPSPort = httpsPort;
                    origin.CustomOriginConfig.OriginProtocolPolicy = this.MatchViewer ? OriginProtocolPolicy.MatchViewer : OriginProtocolPolicy.HttpOnly;
                }
            }
            
            config.Origins.Quantity = config.Origins.Items.Count;

            config.PriceClass = this.SelectedPriceClass.Value;
            config.Logging = new Amazon.CloudFront.Model.LoggingConfig();
            config.Logging.Enabled = this.IsLoggingEnabled;
            config.Logging.IncludeCookies = this.IsLoggingCookiesEnabled;
            config.Logging.Bucket = this.GetLoggingTargetBucketWithPostFix();
            if (config.Logging.Bucket == null)
                config.Logging.Bucket = "";
            config.Logging.Prefix = this.LoggingTargetPrefix;
            if (config.Logging.Prefix == null)
                config.Logging.Prefix = "";


            if (this._lastLoadedDistributionConfig != null)
            {
                config.CacheBehaviors = this._lastLoadedDistributionConfig.CacheBehaviors;
                config.DefaultCacheBehavior = this._lastLoadedDistributionConfig.DefaultCacheBehavior;
                config.CustomErrorResponses = this._lastLoadedDistributionConfig.CustomErrorResponses;
            }
            else
            {
                config.CacheBehaviors = new CacheBehaviors() { Quantity = 0 };
                config.DefaultCacheBehavior = new DefaultCacheBehavior() { MinTTL = 0, ForwardedValues = new ForwardedValues() { QueryString = false } };
                config.DefaultCacheBehavior.ForwardedValues.Cookies = new CookiePreference() { Forward = "none" };
                config.DefaultCacheBehavior.TargetOriginId = config.Origins.Items[0].Id;
                config.CustomErrorResponses = new CustomErrorResponses() { Quantity = 0 };
            }
            
            config.DefaultCacheBehavior.ViewerProtocolPolicy = this.RequireHTTPS ? ViewerProtocolPolicy.HttpsOnly : ViewerProtocolPolicy.AllowAll;

            config.DefaultCacheBehavior.TrustedSigners = new TrustedSigners();
            if (this.IsPrivateDistributionEnabled && (this.TrustedSignerSelf || this.TrustedSignerAWSAccountIds.Count > 0))
            {
                if (this.TrustedSignerSelf)
                    config.DefaultCacheBehavior.TrustedSigners.Items.Add("self");

                foreach (MutableString awsAccountId in this.TrustedSignerAWSAccountIds)
                {
                    if (string.IsNullOrEmpty(awsAccountId.Value))
                        continue;

                    string cleanedAccountId = awsAccountId.Value.Replace("-", "");
                    config.DefaultCacheBehavior.TrustedSigners.Items.Add(cleanedAccountId);                    
                }                
            }
            config.DefaultCacheBehavior.TrustedSigners.Quantity = config.DefaultCacheBehavior.TrustedSigners.Items.Count;
            config.DefaultCacheBehavior.TrustedSigners.Enabled = config.DefaultCacheBehavior.TrustedSigners.Items.Count > 0;

            config.Aliases = new Aliases();
            foreach (MutableString cname in this.CNAMEs)
            {
                if (string.IsNullOrEmpty(cname.Value))
                    continue;
                config.Aliases.Items.Add(cname.Value);
            }
            config.Aliases.Quantity = config.Aliases.Items.Count;

            return config;
        }
    }
}
