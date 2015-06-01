using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Util;

using Amazon.CloudFront.Model;

namespace Amazon.AWSToolkit.CloudFront.Model
{
    public class StreamingDistributionConfigModel : BaseConfigModel
    {

        public void LoadStreamingDistributionConfig(StreamingDistributionConfig config)
        {
            this.Comment = config.Comment;
            this.Enabled = config.Enabled;
            this.CallerReference = config.CallerReference;

            this.S3BucketOrigin = config.S3Origin.DomainName;
            if (config.S3Origin.OriginAccessIdentity != null)
            {
                var oai = FindOriginAccessIdentityById(config.S3Origin.OriginAccessIdentity);
                if (oai != null)
                {
                    this.CanonicalUserId = oai.CanonicalUserId;
                }
            }

            if (config.Logging != null)
            {
                this.IsLoggingEnabled = true;
                this.LoggingTargetBucket = config.Logging.Bucket;
                this.LoggingTargetPrefix = config.Logging.Prefix;
            }

            this.TrustedSignerAWSAccountIds.Clear();
            if (config.TrustedSigners != null)
            {
                foreach (string account in config.TrustedSigners.Items)
                {
                    if (string.Equals(account, "self", StringComparison.InvariantCultureIgnoreCase))
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

        public StreamingDistributionConfig ConvertToStreamingDistribtionConfig()
        {
            StreamingDistributionConfig config = new StreamingDistributionConfig()
            {
                Comment = this.Comment,
                Enabled = this.Enabled
            };

            if(config.PriceClass != null)
                this.SelectedPriceClass = PriceClass.FindPriceClass(config.PriceClass);

            if (config.Comment == null)
                config.Comment = "";

            if (this.CallerReference == null)
                this.CallerReference = DateTime.Now.Ticks.ToString();
            config.CallerReference = this.CallerReference;
            config.PriceClass = this.SelectedPriceClass.Value;
            config.S3Origin = new S3Origin()
            {
                DomainName = this.S3BucketOrigin
            };

            if (!config.S3Origin.DomainName.EndsWith(".s3.amazonaws.com"))
                config.S3Origin.DomainName += ".s3.amazonaws.com";


            var wrapper = this.GetOriginAccessIdentity();
            if (wrapper != null)
            {
                config.S3Origin.OriginAccessIdentity = "origin-access-identity/cloudfront/" + wrapper.Id;
            }
            if (config.S3Origin.OriginAccessIdentity == null)
                config.S3Origin.OriginAccessIdentity = "";

            config.Logging = new StreamingLoggingConfig();
            config.Logging.Enabled = this.IsLoggingEnabled;
            config.Logging.Bucket = this.GetLoggingTargetBucketWithPostFix();
            if (config.Logging.Bucket == null)
                config.Logging.Bucket = "";
            config.Logging.Prefix = this.LoggingTargetPrefix;
            if (config.Logging.Prefix == null)
                config.Logging.Prefix = "";



            config.TrustedSigners = new TrustedSigners();            
            if (this.IsPrivateDistributionEnabled && (this.TrustedSignerSelf || this.TrustedSignerAWSAccountIds.Count > 0))
            {
                if (this.TrustedSignerSelf)
                    config.TrustedSigners.Items.Add("self");

                foreach (MutableString awsAccountId in this.TrustedSignerAWSAccountIds)
                {
                    if (string.IsNullOrEmpty(awsAccountId.Value))
                        continue;

                    string cleanedAccountId = awsAccountId.Value.Replace("-", "");
                    config.TrustedSigners.Items.Add(cleanedAccountId);
                }
            }
            config.TrustedSigners.Quantity = config.TrustedSigners.Items.Count;
            config.TrustedSigners.Enabled = config.TrustedSigners.Quantity > 0;

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
