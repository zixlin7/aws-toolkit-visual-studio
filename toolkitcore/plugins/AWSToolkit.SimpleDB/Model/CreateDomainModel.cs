using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.SimpleDB.Model
{
    public class CreateDomainModel : BaseModel
    {
        string _domainName;

        public string DomainName
        {
            get => this._domainName;
            set
            {
                this._domainName = value;
                base.NotifyPropertyChanged("DomainName");
            }
        }
    }
}
