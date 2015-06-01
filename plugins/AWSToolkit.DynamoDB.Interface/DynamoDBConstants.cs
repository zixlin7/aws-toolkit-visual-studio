using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.DynamoDBv2.DocumentModel;

namespace Amazon.AWSToolkit.DynamoDB
{
    public static class DynamoDBConstants
    {
        public const string TYPE_STRING = "S";
        public const string TYPE_STRING_SET = "SS";
        public const string TYPE_NUMERIC = "N";
        public const string TYPE_NUMERIC_SET = "NS";
        public const string TYPE_BINARY = "B";
        public const string TYPE_BINARY_SET = "BS";
        public const string HASH_KEY = "HASH";
        public const string RANGE_KEY = "RANGE";
        public const string PROJECTION_TYPE_ALL = "ALL";
        public const string PROJECTION_TYPE_KEYS_ONLY = "KEYS_ONLY";
        public const string PROJECTION_TYPE_INCLUDE = "INCLUDE";

        public const int MAX_LSI_PER_TABLE = 5;
        public const int MAX_GSI_PER_TABLE = 5;


        public static string ToConstant(DynamoDBEntryType type, bool isSet)
        {
            switch (type)
            {
                case DynamoDBEntryType.String:
                    return isSet ? TYPE_STRING_SET : TYPE_STRING;
                case DynamoDBEntryType.Numeric:
                    return isSet ? TYPE_NUMERIC_SET : TYPE_NUMERIC;
                case DynamoDBEntryType.Binary:
                default:
                    return isSet ? TYPE_BINARY_SET : TYPE_BINARY;
            }
        }

        public static DynamoDBEntryType FromConstant(string value)
        {
            if (value == TYPE_STRING)
                return DynamoDBEntryType.String;
            else if (value == TYPE_NUMERIC)
                return DynamoDBEntryType.Numeric;
            else if (value == TYPE_BINARY)
                return DynamoDBEntryType.Binary;
            throw new InvalidCastException();
        }

        public static string ToAttributeType(string value)
        {
            if (value.Equals("String", StringComparison.InvariantCulture))
            {
                return DynamoDBConstants.TYPE_STRING;
            }
            else if (value.Equals("Numeric", StringComparison.InvariantCulture))
            {
                return DynamoDBConstants.TYPE_NUMERIC;
            }
            else if (value.Equals("Binary", StringComparison.InvariantCulture))
            {
                return DynamoDBConstants.TYPE_BINARY;
            }

            throw new ArgumentException("Invalid Attribute Type.", "value");
        }

        public static string FromAttributeType(string value)
        {
            if (value.Equals(DynamoDBConstants.TYPE_STRING, StringComparison.InvariantCulture))
            {
                return "String" ;
            }
            else if (value.Equals(DynamoDBConstants.TYPE_NUMERIC, StringComparison.InvariantCulture))
            {
                return "Numeric";
            }
            else if (value.Equals(DynamoDBConstants.TYPE_BINARY, StringComparison.InvariantCulture))
            {
                return "Binary";
            }

            throw new ArgumentException("Invalid Attribute Type.", "value");
        }

        public const string TABLE_STATUS_CREATING = "Creating";
        public const string TABLE_STATUS_ACTIVE = "Active";
        public const string TABLE_STATUS_DELETING = "Deleting";
    }
}
