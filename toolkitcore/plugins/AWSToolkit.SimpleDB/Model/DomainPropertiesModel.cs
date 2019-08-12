using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.SimpleDB.Model
{
    public class DomainPropertiesModel : BaseModel
    {
        string _domain;

        string _itemCount;
        string _attributeValueCount;
        string _attributeNameCount;
        string _itemNamesSizeBytes;
        string _attributeValuesSizeBytes;
        string _attributeNamesSizeBytes;

        public DomainPropertiesModel(string domain)
        {
            this._domain = domain;
        }

        public string Domain => this._domain;

        public string ItemCount
        {
            get => this._itemCount;
            set
            {
                this._itemCount = value;
                base.NotifyPropertyChanged("ItemCount");
            }
        }

        public string AttributeValueCount
        {
            get => this._attributeValueCount;
            set
            {
                this._attributeValueCount = value;
                base.NotifyPropertyChanged("AttributeValueCount");
            }
        }

        public string AttributeNameCount
        {
            get => this._attributeNameCount;
            set
            {
                this._attributeNameCount = value;
                base.NotifyPropertyChanged("AttributeNameCount");
            }
        }

        public string ItemNamesSizeBytes
        {
            get => this._itemNamesSizeBytes;
            set
            {
                this._itemNamesSizeBytes = value;
                base.NotifyPropertyChanged("ItemNamesSizeBytes");
            }
        }

        public string AttributeValuesSizeBytes
        {
            get => this._attributeValuesSizeBytes;
            set
            {
                this._attributeValuesSizeBytes = value;
                base.NotifyPropertyChanged("AttributeValuesSizeBytes");
            }
        }

        public string AttributeNamesSizeBytes
        {
            get => this._attributeNamesSizeBytes;
            set
            {
                this._attributeNamesSizeBytes = value;
                base.NotifyPropertyChanged("AttributeNamesSizeBytes");
            }
        }

    }
}
