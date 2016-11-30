using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.Runtime.Internal.Settings;

using log4net;

namespace Amazon.AWSToolkit.DynamoDB.View.Columns
{
    public class DynamoDBColumnDefinition
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(DynamoDBColumnDefinition));

        const string DYNAMODB_TABLE_SETTINGS = "DynamoDBTableSettings";
        const string ATTRIBUTES_SETTING = "Attributes";
        const char COLLECTION_DELIMITER = ':';

        public DynamoDBColumnDefinition(string attributeName, string defaultDataType)
        {
            this.AttributeName = attributeName;
            this.DefaultDataType = defaultDataType;
        }

        public string AttributeName
        {
            get;
            private set;
        }

        public string DefaultDataType
        {
            get;
            private set;
        }

        public string Serialize()
        {
            return string.Format("Name={0},Type={1}", this.AttributeName, this.DefaultDataType);
        }

        public static DynamoDBColumnDefinition Deserialize(string format)
        {
            var tokens = format.Split(',');

            var nameToken = tokens.FirstOrDefault(x => x.StartsWith("Name="));
            if (nameToken == null)
                return null;

            var name = nameToken.Substring(5);

            var typeToken = tokens.FirstOrDefault(x => x.StartsWith("Type="));
            if (typeToken == null)
                return null;

            var type = typeToken.Substring(5);

            return new DynamoDBColumnDefinition(name, type);
        }

        public static IEnumerable<DynamoDBColumnDefinition> ReadCachedDefinitions(string settingsKey)
        {
            try
            {
                var settings = PersistenceManager.Instance.GetSettings(DYNAMODB_TABLE_SETTINGS);
                var os = settings[settingsKey];

                var values = new List<DynamoDBColumnDefinition>();

                if (os[ATTRIBUTES_SETTING] != null)
                {
                    var tokens = os[ATTRIBUTES_SETTING].Split(DynamoDBColumnDefinition.COLLECTION_DELIMITER);
                    foreach (var token in tokens)
                    {
                        var def = DynamoDBColumnDefinition.Deserialize(token);
                        if (def == null)
                            continue;

                        values.Add(def);
                    }
                }

                return values;
            }
            catch(Exception e)
            {
                LOGGER.Warn("Error reading cached column definitions", e);
                return new DynamoDBColumnDefinition[0];
            }
        }

        public static void WriteCachedDefinitions(string settingsKey, IEnumerable<DynamoDBColumnDefinition> definitions)
        {
            try
            {
                var settings = PersistenceManager.Instance.GetSettings(DYNAMODB_TABLE_SETTINGS);
                var os = settings[settingsKey];

                var builder = new StringBuilder();
                foreach (var def in definitions)
                {
                    if (builder.Length > 0)
                        builder.Append(DynamoDBColumnDefinition.COLLECTION_DELIMITER);
                    builder.AppendFormat(def.Serialize());
                }

                os[ATTRIBUTES_SETTING] = builder.ToString();
                PersistenceManager.Instance.SaveSettings(DYNAMODB_TABLE_SETTINGS, settings);
            }
            catch (Exception e)
            {
                LOGGER.Warn("Error writing cached column definitions", e);
            }
        }
    }
}
