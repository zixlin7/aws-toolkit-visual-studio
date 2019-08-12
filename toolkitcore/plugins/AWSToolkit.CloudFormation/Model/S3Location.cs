namespace Amazon.AWSToolkit.CloudFormation.Model
{
    /// <summary>
    /// A specification of a location in Amazon S3. This is a copy of the
    /// published ElasticBeanstalk S3Location SDK class
    /// </summary>
    public class S3Location
    {
        public S3Location()
        {
        }

        // Summary:
        //     The Amazon S3 bucket where the data is located.
        //     Constraints: Length 0 - 255
        public string S3Bucket { get; set; }

        //
        // Summary:
        //     The Amazon S3 key where the data is located.
        //     Constraints: Length 0 - 1024
        public string S3Key { get; set; }

        // Summary:
        //     Sets the S3Bucket property
        //
        // Parameters:
        //   s3Bucket:
        //     The value to set for the S3Bucket property
        //
        // Returns:
        //     this instance
        public S3Location WithS3Bucket(string s3Bucket)
        {
            this.S3Bucket = s3Bucket;
            return this;
        }

        //
        // Summary:
        //     Sets the S3Key property
        //
        // Parameters:
        //   s3Key:
        //     The value to set for the S3Key property
        //
        // Returns:
        //     this instance
        public S3Location WithS3Key(string s3Key)
        {
            this.S3Key = s3Key;
            return this;
        }
    }
}
