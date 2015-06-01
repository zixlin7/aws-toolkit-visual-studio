using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.DynamoDBv2;

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
            get { return this._status; }
            set
            {
                this._status = value;
                base.NotifyPropertyChanged("Status");
            }
        }

        string _originalReadCapacityUnits;
        public string OriginalReadCapacityUnits
        {
            get { return this._originalReadCapacityUnits; }
            set
            {
                this._originalReadCapacityUnits = value;
                base.NotifyPropertyChanged("OriginalReadCapacityUnits");
            }
        }

        string _originalWriteCapacityUnits;
        public string OriginalWriteCapacityUnits
        {
            get { return this._originalWriteCapacityUnits; }
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

        public Dictionary<string, SecondaryIndex> ExistingGlobalSecondaryIndexes
        {
            get;
            private set;
        }
    }
}
