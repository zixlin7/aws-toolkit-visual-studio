using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.S3.Model
{
    public class CreateBucketModel : BaseModel
    {
        string _bucketName;

        public CreateBucketModel()
        {
        }

        public string BucketName
        {
            get => this._bucketName;
            set
            {
                this._bucketName = value;
                this.NotifyPropertyChanged("BucketName");
            }
        }
    }
}
