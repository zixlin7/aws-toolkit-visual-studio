using System.Collections.ObjectModel;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.CloudFront.Model
{
    public class ViewOriginAccessIdentiesModel : BaseModel
    {
        ObservableCollection<OriginAccessIdentity> _identites = new ObservableCollection<OriginAccessIdentity>();
        public ObservableCollection<OriginAccessIdentity> Identities
        {
            get => this._identites;
            set
            {
                this._identites = value;
                base.NotifyPropertyChanged("Identities");
            }
        }

        public class OriginAccessIdentity : BaseModel
        {

            string _id;
            public string Id
            {
                get => this._id;
                set
                {
                    this._id = value;
                    base.NotifyPropertyChanged("Id");
                }
            }

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

            string _canonicalUserId;
            public string CanonicalUserId
            {
                get => this._canonicalUserId;
                set
                {
                    this._canonicalUserId = value;
                    base.NotifyPropertyChanged("CanonicalUserId");
                }
            }
        }
    }
}
