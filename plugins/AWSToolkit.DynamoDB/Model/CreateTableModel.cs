using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;
using Amazon.DynamoDBv2.DocumentModel;
using System.Collections.ObjectModel;

namespace Amazon.AWSToolkit.DynamoDB.Model
{
    public class CreateTableModel : BaseModel
    {
        public CreateTableModel()
        {
            _localSecondaryIndexes = new ObservableCollection<SecondaryIndex>();
            _globalSecondaryIndexes = new ObservableCollection<SecondaryIndex>();
        }

        string _tableName;
        public string TableName
        {
            get { return this._tableName; }
            set
            {
                this._tableName = value;
                base.NotifyPropertyChanged("TableName");
            }
        }

        string _hashKeyName;
        public string HashKeyName
        {
            get { return this._hashKeyName; }
            set
            {
                this._hashKeyName = value;
                base.NotifyPropertyChanged("HashKeyName");
            }
        }

        DynamoDBEntryType _hashKeyType = DynamoDBEntryType.String;
        public DynamoDBEntryType HashKeyType
        {
            get { return this._hashKeyType; }
            set
            {
                switch (value)
                {
                    case DynamoDBEntryType.String:
                        this.IsHashKeyString = true;
                        break;
                    case DynamoDBEntryType.Numeric:
                        this.IsHashKeyNumeric = true;
                        break;
                    case DynamoDBEntryType.Binary:
                        this.IsHashKeyBinary = true;
                        break;
                    default:
                        return;
                }
                this.NotifyIsHashKeyChange();
            }
        }
        public bool IsHashKeyString
        {
            get { return this._hashKeyType == DynamoDBEntryType.String; }
            set
            {
                this._hashKeyType = DynamoDBEntryType.String;
                this.NotifyIsHashKeyChange();
            }
        }
        public bool IsHashKeyNumeric
        {
            get { return this._hashKeyType == DynamoDBEntryType.Numeric; }
            set
            {
                this._hashKeyType = DynamoDBEntryType.Numeric;
                this.NotifyIsHashKeyChange();
            }
        }
        public bool IsHashKeyBinary
        {
            get { return this._hashKeyType == DynamoDBEntryType.Binary; }
            set
            {
                this._hashKeyType = DynamoDBEntryType.Binary;
                this.NotifyIsHashKeyChange();
            }
        }
        private void NotifyIsHashKeyChange()
        {
            base.NotifyPropertyChanged("IsHashKeyString");
            base.NotifyPropertyChanged("IsHashKeyNumeric");
            base.NotifyPropertyChanged("IsHashKeyBinary");
        }

        bool _useLocalSecondaryIndexes;
        public bool UseLocalSecondaryIndexes
        {
            get { return this._useLocalSecondaryIndexes; }
            set
            {
                this._useLocalSecondaryIndexes = value;
                base.NotifyPropertyChanged("UseLocalSecondaryIndexes");
            }
        }

        bool _useGlobalSecondaryIndexes;
        public bool UseGlobalSecondaryIndexes
        {
            get { return this._useGlobalSecondaryIndexes; }
            set
            {
                this._useGlobalSecondaryIndexes = value;
                base.NotifyPropertyChanged("UseGlobalSecondaryIndexes");
            }
        }

        private ObservableCollection<SecondaryIndex> _localSecondaryIndexes;

        public ObservableCollection<SecondaryIndex> LocalSecondaryIndexes
        {
            get { return _localSecondaryIndexes; }
            set { _localSecondaryIndexes = value; }
        }

        private ObservableCollection<SecondaryIndex> _globalSecondaryIndexes;

        public ObservableCollection<SecondaryIndex> GlobalSecondaryIndexes
        {
            get { return _globalSecondaryIndexes; }
            set { _globalSecondaryIndexes = value; }
        }

        bool _useRangeKey;
        public bool UseRangeKey
        {
            get { return this._useRangeKey; }
            set
            {
                this._useRangeKey = value;
                base.NotifyPropertyChanged("UseRangeKey");
            }
        }

        string _rangeKeyName;
        public string RangeKeyName
        {
            get { return this._rangeKeyName; }
            set
            {
                this._rangeKeyName = value;
                base.NotifyPropertyChanged("RangeKeyName");
            }
        }

        DynamoDBEntryType _rangeKeyType = DynamoDBEntryType.String;
        public DynamoDBEntryType RangeKeyType
        {
            get { return this._rangeKeyType; }
            set
            {
                switch (value)
                {
                    case DynamoDBEntryType.String:
                        this.IsRangeKeyString = true;
                        break;
                    case DynamoDBEntryType.Numeric:
                        this.IsRangeKeyNumeric = true;
                        break;
                    case DynamoDBEntryType.Binary:
                        this.IsRangeKeyBinary = true;
                        break;
                    default:
                        return;
                }
                this.NotifyIsRangeKeyChange();
            }
        }
        public bool IsRangeKeyString
        {
            get { return this._rangeKeyType == DynamoDBEntryType.String; }
            set
            {
                this._rangeKeyType = DynamoDBEntryType.String;
                this.NotifyIsRangeKeyChange();
            }
        }
        public bool IsRangeKeyNumeric
        {
            get { return this._rangeKeyType == DynamoDBEntryType.Numeric; }
            set
            {
                this._rangeKeyType = DynamoDBEntryType.Numeric;
                this.NotifyIsRangeKeyChange();
            }
        }
        public bool IsRangeKeyBinary
        {
            get { return this._rangeKeyType == DynamoDBEntryType.Binary; }
            set
            {
                this._rangeKeyType = DynamoDBEntryType.Binary;
                this.NotifyIsRangeKeyChange();
            }
        }
        private void NotifyIsRangeKeyChange()
        {
            base.NotifyPropertyChanged("IsRangeKeyString");
            base.NotifyPropertyChanged("IsRangeKeyNumeric");
            base.NotifyPropertyChanged("IsRangeKeyBinary");
        }

        string _readsCapacityUnits = "3";
        public string ReadCapacityUnits
        {
            get { return this._readsCapacityUnits; }
            set
            {
                this._readsCapacityUnits = value;
                base.NotifyPropertyChanged("ReadCapacityUnits");
            }
        }

        string _writeCapacityUnits = "1";
        public string WriteCapacityUnits
        {
            get { return this._writeCapacityUnits; }
            set
            {
                this._writeCapacityUnits = value;
                base.NotifyPropertyChanged("WriteCapacityUnits");
            }
        }

        bool _useBasicAlarms = false;
        public bool UseBasicAlarms
        {
            get { return this._useBasicAlarms; }
            set
            {
                this._useBasicAlarms = value;
                base.NotifyPropertyChanged("UseBasicAlarms");
            }
        }

        string _alarmEmail;
        public string AlarmEmail
        {
            get { return this._alarmEmail; }
            set
            {
                this._alarmEmail = value;
                base.NotifyPropertyChanged("AlarmEmail");
            }
        }

        Percentages _percentage = Percentages.DEFAULT;
        public Percentages SelectedPercentage
        {
            get { return this._percentage;}
            set
            {
                this._percentage = value;
                base.NotifyPropertyChanged("SelectedPercentage");
            }
        }

        public IEnumerable<Percentages> PossiblePercentage
        {
            get { return Percentages.OPTIONS; }
        }

        public class Percentages
        {
            public static readonly Percentages DEFAULT = new Percentages(.80);
            public static readonly IEnumerable<Percentages> OPTIONS = new Percentages[]
                {
                    new Percentages(.95),
                    new Percentages(.90),
                    new Percentages(.85),
                    DEFAULT,
                    new Percentages(.75),
                };

            public Percentages(double value)
            {
                this.Value = value;
                this.DisplayName = string.Format("{0}%", (int)(this.Value * 100));
            }

            public double Value
            {
                get;
                private set;
            }

            public string DisplayName
            {
                get;
                private set;
            }
        }
    }

    public class IndexKeyDefintion : BaseModel, ICloneable
    {
        private string _name = string.Empty;
        private DynamoDBv2.ScalarAttributeType _type = DynamoDBv2.ScalarAttributeType.S;

        public object Clone()
        {
            var clone = new IndexKeyDefintion()
            {
                _name = this._name,
                _type = this._type
            };

            return clone;
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; base.NotifyPropertyChanged("Name"); }
        }


        public bool IsString
        {
            get { return this._type == DynamoDBv2.ScalarAttributeType.S; }
            set 
            {
                if (value)
                    this._type = DynamoDBv2.ScalarAttributeType.S;

                base.NotifyPropertyChanged("IsString"); 
            }
        }

        public DynamoDBv2.ScalarAttributeType Type
        {
            get {return this._type;}
            set { this._type = value; }
        }

        public bool IsNumeric
        {
            get { return this._type == DynamoDBv2.ScalarAttributeType.N; }
            set
            {
                if (value)
                    this._type = DynamoDBv2.ScalarAttributeType.N;

                base.NotifyPropertyChanged("IsNumeric");
            }
        }

        public bool IsBinary
        {
            get { return this._type == DynamoDBv2.ScalarAttributeType.B; }
            set
            {
                if (value)
                    this._type = DynamoDBv2.ScalarAttributeType.B;

                base.NotifyPropertyChanged("IsBinary");
            }
        }

        public string DisplayName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this._name))
                    return string.Empty;
                return string.Format("{0} ({1})", this.Name, this._type);
            }
        }
    }

    public class ProjectAttributeDefinition : BaseModel, ICloneable
    {
        public ProjectAttributeDefinition()
        {
            _projectionColumnList = new ObservableCollection<StringWrapper>();
        }

        public object Clone()
        {
            var clone = new ProjectAttributeDefinition
            {
                _projectionType = this._projectionType,
                _projectionTypeDisplayValue = this._projectionTypeDisplayValue,
                _isCustomProjectionAllowed = this._isCustomProjectionAllowed
            };

            clone._projectionColumnList = new ObservableCollection<StringWrapper>();
            foreach (var item in this.ProjectionColumnList)
            {
                clone._projectionColumnList.Add(item);
            }

            return clone;
        }

        private string _projectionType = DynamoDBConstants.PROJECTION_TYPE_ALL;

        public string ProjectionType
        {
            get { return _projectionType; }
            set
            {
                _projectionType = value;
                base.NotifyPropertyChanged("ProjectionType");
                base.NotifyPropertyChanged("ProjectionTypeDisplayValue");
                this.IsCustomProjectionAllowed =
                    _projectionType.Equals(DynamoDBConstants.PROJECTION_TYPE_INCLUDE, StringComparison.InvariantCultureIgnoreCase);

                if (!_isCustomProjectionAllowed)
                {
                    this.ProjectionColumnList.Clear();
                }
            }
        }

        private string _projectionTypeDisplayValue;

        public string ProjectionTypeDisplayValue
        {
            get
            {
                var projectType = ToProjectionTypeDisplayValue(this.ProjectionType);
                if (IsCustomProjectionAllowed)
                    return this.ProjectionColumns;

                return ToProjectionTypeDisplayValue(this.ProjectionType);
            }
            set
            {
                _projectionTypeDisplayValue = value;
            }
        }

        private bool _isCustomProjectionAllowed = false;

        public bool IsCustomProjectionAllowed
        {
            get
            {
                return _isCustomProjectionAllowed;
            }
            set
            {
                _isCustomProjectionAllowed = value;
                base.NotifyPropertyChanged("IsCustomProjectionAllowed");
            }
        }

        public string ProjectionColumns
        {
            get 
            {
                StringBuilder sb = new StringBuilder();
                foreach (var column in this.ProjectionColumnList)
                {
                    if (string.IsNullOrWhiteSpace(column.Value))
                        continue;

                    if (sb.Length != 0)
                        sb.Append(", ");
                    sb.Append(column.Value);
                }
                return sb.ToString(); 
            }
        }

        private ObservableCollection<StringWrapper> _projectionColumnList = new ObservableCollection<StringWrapper>();

        public ObservableCollection<StringWrapper> ProjectionColumnList
        {
            get { return _projectionColumnList; }
            set { _projectionColumnList = value; base.NotifyPropertyChanged("ProjectionColumnList"); }
        }

        private static string ToProjectionTypeDisplayValue(string value)
        {
            if (value.Equals(DynamoDBConstants.PROJECTION_TYPE_ALL, StringComparison.InvariantCulture))
            {
                return "All Attributes";
            }
            else if (value.Equals(DynamoDBConstants.PROJECTION_TYPE_KEYS_ONLY, StringComparison.InvariantCulture))
            {
                return "Table and Index Keys";
            }
            else if (value.Equals(DynamoDBConstants.PROJECTION_TYPE_INCLUDE, StringComparison.InvariantCulture))
            {
                return "Specify Attributes";
            }

            throw new ArgumentException("Invalid Projection Type.", "value");
        }
    }

    public class SecondaryIndex : BaseModel, ICloneable
    {
        public SecondaryIndex()
        {
            
        }

        public object Clone()
        {
            var clone = new SecondaryIndex
            {
                _name = this._name,
                _isExisting = this._isExisting,
                _hashKeyName = this._hashKeyName.Clone() as IndexKeyDefintion,
                _rangeKeyName = this._rangeKeyName.Clone() as IndexKeyDefintion,
                _projectAttributeDefinition = this._projectAttributeDefinition.Clone() as ProjectAttributeDefinition,
                _readCapacity = this._readCapacity,
                _writeCapacity = this._writeCapacity,
                IndexStatus = this.IndexStatus
            };

            return clone;
        }

        private string _name;

        public string Name
        {
            get { return _name; }
            set { _name = value; base.NotifyPropertyChanged("Name"); }
        }

        bool _isExisting;
        public bool IsExisting
        {
            get { return _isExisting; }
            set { _isExisting = value; base.NotifyPropertyChanged("IsExisting"); }
        }

        private IndexKeyDefintion _hashKeyName = new IndexKeyDefintion();

        public IndexKeyDefintion HashKey
        {
            get { return _hashKeyName; }
            set { _hashKeyName = value; base.NotifyPropertyChanged("HashKey"); }
        }

        private IndexKeyDefintion _rangeKeyName = new IndexKeyDefintion();

        public IndexKeyDefintion RangeKey
        {
            get { return _rangeKeyName; }
            set { _rangeKeyName = value; base.NotifyPropertyChanged("RangeKey");  }
        }

        private ProjectAttributeDefinition _projectAttributeDefinition = new ProjectAttributeDefinition();
        public ProjectAttributeDefinition ProjectAttributeDefinition
        {
            get
            {
                return _projectAttributeDefinition;
            }
            set
            {
                _projectAttributeDefinition = value;
                base.NotifyPropertyChanged("ProjectAttributeDefinition");
            }
        }

        long _readCapacity = 3;
        public long ReadCapacity
        {
            get { return _readCapacity; }
            set { _readCapacity = value; base.NotifyPropertyChanged("ReadCapacity"); }
        }

        long _writeCapacity = 1;
        public long WriteCapacity
        {
            get { return _writeCapacity; }
            set { _writeCapacity = value; base.NotifyPropertyChanged("WriteCapacity"); }
        }

        public string IndexStatus
        {
            get;
            set;
        }

        public bool HasRangeKey
        {
            get { return (RangeKey != null && !string.IsNullOrEmpty(RangeKey.Name)); }
        }
    }

    public class StringWrapper
    {
        public StringWrapper()
        {
            Value = string.Empty;
        }

        string value;
        public string Value 
        {
            get { return this.value; }
            set { this.value = value; }
        }
    }
}
