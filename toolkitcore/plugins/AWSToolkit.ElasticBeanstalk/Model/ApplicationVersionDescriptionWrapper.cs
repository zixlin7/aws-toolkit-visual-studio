using System;
using Amazon.ElasticBeanstalk.Model;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Model
{
    public class ApplicationVersionDescriptionWrapper
    {
        ApplicationVersionDescription _originalVersion;

        public ApplicationVersionDescriptionWrapper(ApplicationVersionDescription originalVersion)
        {
            this._originalVersion = originalVersion;
        }

        public string VersionLabel => this._originalVersion.VersionLabel;

        public string S3Key
        {
            get 
            {
                if (this._originalVersion.SourceBundle == null)
                    return string.Empty;

                return this._originalVersion.SourceBundle.S3Key; 
            }
        }

        public string S3Bucket
        {
            get
            {
                if (this._originalVersion.SourceBundle == null)
                    return string.Empty;

                return this._originalVersion.SourceBundle.S3Bucket;
            }
        }

        public string Description
        {
            get 
            {
                if (!string.IsNullOrEmpty(this._originalVersion.Description))
                    return this._originalVersion.Description.Trim();
                else
                    return string.Empty;
            }
        }

        public DateTime DateCreated => this._originalVersion.DateCreated;
    }
}
