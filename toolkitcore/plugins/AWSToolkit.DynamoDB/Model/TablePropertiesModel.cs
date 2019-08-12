using System.Collections.Generic;

namespace Amazon.AWSToolkit.DynamoDB.Model
{
    public class TablePropertiesModel : CreateTableModel
    {
        public TablePropertiesModel(string tableName)
        {
            this.TableName = tableName;
            this.ExistingGlobalSecondaryIndexes = new Dictionary<string, SecondaryIndex>();
        }

        string _status;
        public string Status
        {
            get => this._status;
            set
            {
                this._status = value;
                base.NotifyPropertyChanged("Status");
            }
        }

        string _originalReadCapacityUnits;
        public string OriginalReadCapacityUnits
        {
            get => this._originalReadCapacityUnits;
            set
            {
                this._originalReadCapacityUnits = value;
                base.NotifyPropertyChanged("OriginalReadCapacityUnits");
            }
        }

        string _originalWriteCapacityUnits;
        public string OriginalWriteCapacityUnits
        {
            get => this._originalWriteCapacityUnits;
            set
            {
                this._originalWriteCapacityUnits = value;
                base.NotifyPropertyChanged("OriginalWriteCapacityUnits");
            }
        }

        public string HashKeyTypeLabel
        {
            get
            {
                if (this.IsHashKeyString)
                    return "String";
                else if (this.IsHashKeyNumeric)
                    return "Numeric";
                else
                    return "Binary";
            }
        }

        public string RangeKeyTypeLabel
        {
            get 
            {
                if (string.IsNullOrEmpty(this.RangeKeyName))
                    return null;

                if (this.IsRangeKeyString)
                    return "String";
                else if (this.IsRangeKeyNumeric)
                    return "Numeric";
                else
                    return "Binary";
            }
        }


        bool _ttlEnabled;
        public bool TTLEnabled
        {
            get => this._ttlEnabled;
            set
            {
                this._ttlEnabled = value;
                base.NotifyPropertyChanged("TTLEnabled");
            }
        }

        string _ttlAttributeName;
        public string TTLAttributeName
        {
            get => this._ttlAttributeName;
            set
            {
                this._ttlAttributeName = value;
                base.NotifyPropertyChanged("TTLAttributeName");
            }
        }

        bool _originalTtlEnabled;
        public bool OriginalTTLEnabled
        {
            get => this._originalTtlEnabled;
            set
            {
                this._originalTtlEnabled = value;
                base.NotifyPropertyChanged("OriginalTTLEnabled");
            }
        }

        string _originalTtlAttributeName;
        public string OriginalTTLAttributeName
        {
            get => this._originalTtlAttributeName;
            set
            {
                this._originalTtlAttributeName = value;
                base.NotifyPropertyChanged("OriginalTTLAttributeName");
            }
        }

        bool _ttlInfoAvailable;
        public bool TTLInfoAvailable
        {
            get => this._ttlInfoAvailable;
            set
            {
                this._ttlInfoAvailable = value;
                base.NotifyPropertyChanged("TTLInfoAvailable");
            }
        }

        public bool TTLInfoUnavailable => !TTLInfoAvailable;

        public Dictionary<string, SecondaryIndex> ExistingGlobalSecondaryIndexes
        {
            get;
        }
    }
}
