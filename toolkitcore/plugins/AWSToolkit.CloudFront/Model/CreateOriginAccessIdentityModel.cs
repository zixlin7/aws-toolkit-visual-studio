using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;

using Amazon.CloudFront;
using Amazon.CloudFront.Model;

namespace Amazon.AWSToolkit.CloudFront.Model
{
    public class CreateOriginAccessIdentityModel : BaseModel
    {
        string _comment;
        public string Comment
        {
            get{return this._comment;}
            set
            {
                this._comment = value;
                base.NotifyPropertyChanged("Comment");
            }
        }

        CloudFrontOriginAccessIdentity _originAccessIdentity;
        public CloudFrontOriginAccessIdentity OriginAccessIdentity
        {
            get { return this._originAccessIdentity; }
            set
            {
                this._originAccessIdentity = value;
                base.NotifyPropertyChanged("OriginAccessIdentity");
            }
        }
    }
}
