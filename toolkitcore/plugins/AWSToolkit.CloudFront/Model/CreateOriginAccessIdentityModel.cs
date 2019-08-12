using Amazon.AWSToolkit.CommonUI;
using Amazon.CloudFront.Model;

namespace Amazon.AWSToolkit.CloudFront.Model
{
    public class CreateOriginAccessIdentityModel : BaseModel
    {
        string _comment;
        public string Comment
        {
            get => this._comment;
            set
            {
                this._comment = value;
                base.NotifyPropertyChanged("Comment");
            }
        }

        CloudFrontOriginAccessIdentity _originAccessIdentity;
        public CloudFrontOriginAccessIdentity OriginAccessIdentity
        {
            get => this._originAccessIdentity;
            set
            {
                this._originAccessIdentity = value;
                base.NotifyPropertyChanged("OriginAccessIdentity");
            }
        }
    }
}
