using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;

using Amazon.DynamoDBv2.DocumentModel;

namespace Amazon.AWSToolkit.DynamoDB.Model
{
    public class ScanCondition : BaseModel
    {
        string _attributeName;
        public string AttributeName
        {
            get => this._attributeName;
            set
            {
                this._attributeName = value;
                base.NotifyPropertyChanged("AttributeName");
                base.NotifyPropertyChanged("HasAttributeName");
            }
        }

        public bool HasAttributeName => !string.IsNullOrEmpty(_attributeName);

        public bool IsNumeric => this.DataType.SystemName == DynamoDBConstants.TYPE_NUMERIC || this.DataType.SystemName == DynamoDBConstants.TYPE_NUMERIC_SET;

        public bool IsSet => this.DataType.SystemName == DynamoDBConstants.TYPE_STRING_SET || this.DataType.SystemName == DynamoDBConstants.TYPE_NUMERIC_SET || this.Operator.Operator == ScanOperator.Between;

        DataTypes _dataType = DataTypes.AttributeDataTypes[0];
        public DataTypes DataType
        {
            get => this._dataType;
            set
            {
                this._dataType = value;
                base.NotifyPropertyChanged("DataType");


                switch(this.DataType.SystemName)
                {
                    case DynamoDBConstants.TYPE_NUMERIC:
                        if(!ConditionsTypes.NumericOperators.Contains(this.Operator))
                        {
                            this.Operator = ConditionsTypes.NumericOperators[0];
                        }
                        break;
                    case DynamoDBConstants.TYPE_NUMERIC_SET:
                        if (!ConditionsTypes.NumericSetOperators.Contains(this.Operator))
                        {
                            this.Operator = ConditionsTypes.NumericSetOperators[0];
                        }
                        break;
                    case DynamoDBConstants.TYPE_STRING:
                        if (!ConditionsTypes.StringOperators.Contains(this.Operator))
                        {
                            this.Operator = ConditionsTypes.StringOperators[0];
                        }
                        break;
                    case DynamoDBConstants.TYPE_STRING_SET:
                        if (!ConditionsTypes.StringSetOperators.Contains(this.Operator))
                        {
                            this.Operator = ConditionsTypes.StringSetOperators[0];
                        }
                        break;
                }

                base.NotifyPropertyChanged("IsSet");
            }
        }

        public void SetDataType(string systemDatTypeName)
        {
            foreach (var dataType in DataTypes.AttributeDataTypes)
            {
                if (dataType.SystemName == systemDatTypeName)
                {
                    DataType = dataType;
                    break;
                }
            }
        }

        ConditionsTypes _operator = ConditionsTypes.StringOperators[0];
        public ConditionsTypes Operator
        {
            get => this._operator;
            set
            {
                this._operator = value;
                base.NotifyPropertyChanged("Operator");
                base.NotifyPropertyChanged("IsSet");
            }
        }

        List<string> _values = new List<string>();
        public IEnumerable<string> Values
        {
            get => this._values;
            set
            {
                _values = new List<string>(value);
                base.NotifyPropertyChanged("Values");
            }
        }

        public string FormattedValues
        {
            get
            {
                // todo: might want to consider additional formatting based on operator,
                // eg 'between A and B'
                if (IsSet)
                {
                    if (Values.Count<string>() > 0)
                    {
                        StringBuilder sb = new StringBuilder("[");
                        foreach (var val in Values)
                        {
                            if (sb.Length > 1)
                                sb.AppendFormat(", {0}", val);
                            else
                                sb.AppendFormat(" {0}", val);
                        }
                        sb.Append(" ]");
                        return sb.ToString();
                    }
                    else
                        return string.Empty;
                }
                else
                {
                    return Values.FirstOrDefault<string>();
                }
            }
        }
    }

    public class DataTypes
    {
        public static readonly IList<DataTypes> KeyDataTypes;
        public static readonly IList<DataTypes> AttributeDataTypes;

        static DataTypes()
        {
            KeyDataTypes = new List<DataTypes>();
            KeyDataTypes.Add(new DataTypes(DynamoDBConstants.TYPE_STRING, "String"));
            KeyDataTypes.Add(new DataTypes(DynamoDBConstants.TYPE_NUMERIC, "Numeric"));

            AttributeDataTypes = new List<DataTypes>();
            AttributeDataTypes.Add(new DataTypes(DynamoDBConstants.TYPE_STRING, "String"));
            AttributeDataTypes.Add(new DataTypes(DynamoDBConstants.TYPE_STRING_SET, "String Set"));
            AttributeDataTypes.Add(new DataTypes(DynamoDBConstants.TYPE_NUMERIC, "Numeric"));
            AttributeDataTypes.Add(new DataTypes(DynamoDBConstants.TYPE_NUMERIC_SET, "Numeric Set"));
        }

        public DataTypes(string systemName, string displayName)
        {
            DisplayName = displayName;
            this.SystemName = systemName;
        }

        public string DisplayName
        {
            get;
        }

        public string SystemName
        {
            get;
        }

        public override string ToString()
        {
            return this.DisplayName;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is DataTypes))
                return false;

            return this.SystemName == ((DataTypes)obj).SystemName;
        }

        public override int GetHashCode()
        {
            return this.SystemName.GetHashCode();
        }
    }

    public class ConditionsTypes
    {
        public static readonly IList<ConditionsTypes> StringOperators;
        public static readonly IList<ConditionsTypes> StringSetOperators;

        public static readonly IList<ConditionsTypes> NumericOperators;
        public static readonly IList<ConditionsTypes> NumericSetOperators;

        static ConditionsTypes()
        {
            StringOperators = new List<ConditionsTypes>();
            StringOperators.Add(new ConditionsTypes(ScanOperator.Equal, "Equal"));
            StringOperators.Add(new ConditionsTypes(ScanOperator.NotEqual, "Not Equal"));
            StringOperators.Add(new ConditionsTypes(ScanOperator.LessThanOrEqual, "Less than or Equal"));
            StringOperators.Add(new ConditionsTypes(ScanOperator.LessThan, "Less than"));
            StringOperators.Add(new ConditionsTypes(ScanOperator.GreaterThanOrEqual, "Greater than or Equal"));
            StringOperators.Add(new ConditionsTypes(ScanOperator.GreaterThan, "Greater than"));
            StringOperators.Add(new ConditionsTypes(ScanOperator.IsNotNull, "Not Null"));
            StringOperators.Add(new ConditionsTypes(ScanOperator.IsNull, "Null"));
            StringOperators.Add(new ConditionsTypes(ScanOperator.Contains, "Contains"));
            StringOperators.Add(new ConditionsTypes(ScanOperator.NotContains, "Not Contains"));
            StringOperators.Add(new ConditionsTypes(ScanOperator.BeginsWith, "Begins With"));
            StringOperators.Add(new ConditionsTypes(ScanOperator.In, "In"));
            StringOperators.Add(new ConditionsTypes(ScanOperator.Between, "Between"));


            StringSetOperators = new List<ConditionsTypes>();
            StringSetOperators.Add(new ConditionsTypes(ScanOperator.IsNotNull, "Not Null"));
            StringSetOperators.Add(new ConditionsTypes(ScanOperator.IsNull, "Null"));
            StringSetOperators.Add(new ConditionsTypes(ScanOperator.Contains, "Contains"));
            StringSetOperators.Add(new ConditionsTypes(ScanOperator.NotContains, "Not Contains"));
            StringSetOperators.Add(new ConditionsTypes(ScanOperator.In, "In"));

            NumericOperators = new List<ConditionsTypes>();
            NumericOperators.Add(new ConditionsTypes(ScanOperator.Equal, "Equal"));
            NumericOperators.Add(new ConditionsTypes(ScanOperator.NotEqual, "Not Equal"));
            NumericOperators.Add(new ConditionsTypes(ScanOperator.LessThanOrEqual, "Less than or Equal"));
            NumericOperators.Add(new ConditionsTypes(ScanOperator.LessThan, "Less than"));
            NumericOperators.Add(new ConditionsTypes(ScanOperator.GreaterThanOrEqual, "Greater than or Equal"));
            NumericOperators.Add(new ConditionsTypes(ScanOperator.GreaterThan, "Greater than"));
            NumericOperators.Add(new ConditionsTypes(ScanOperator.IsNotNull, "Not Null"));
            NumericOperators.Add(new ConditionsTypes(ScanOperator.IsNull, "Null"));
            NumericOperators.Add(new ConditionsTypes(ScanOperator.BeginsWith, "Begins With"));
            NumericOperators.Add(new ConditionsTypes(ScanOperator.In, "In"));
            NumericOperators.Add(new ConditionsTypes(ScanOperator.Between, "Between"));


            NumericSetOperators = StringSetOperators;
        }



        public ConditionsTypes()
        {

        }

        public ConditionsTypes(ScanOperator op, string displayName)
        {
            this.Operator = op;
            this.DisplayName = displayName;
        }


        public string DisplayName
        {
            get;
        }

        public ScanOperator Operator
        {
            get;
        }

        public override string ToString()
        {
            return this.DisplayName;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ConditionsTypes))
                return false;

            return this.Operator == ((ConditionsTypes)obj).Operator;
        }

        public override int GetHashCode()
        {
            return this.Operator.GetHashCode();
        }
    }
}
