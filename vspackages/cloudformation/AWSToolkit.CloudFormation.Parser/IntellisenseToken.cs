using Amazon.AWSToolkit.CloudFormation.Parser.Schema;

namespace Amazon.AWSToolkit.CloudFormation.Parser
{
    public enum IntellisensePositionType { Key, Value, RefValue, 
        FnGetAttResource, FnGetAttAttribute, 
        FnFindInMapMapName, FnFindInMapKey, FnFindInMapValue, 
        FnIfCondition, FnIfTrue, FnIfFalse,
        Condition }
    public enum IntellisenseTokenType { Generic, ObjectKey, AllowedValue, Reference, Condition, IntrinsicFunction, NamedArrayElement }

    public class IntellisenseToken
    {
        public IntellisenseToken(SchemaObject schema, IntellisenseTokenType type, string displayName, string code, string description)
        {
            this.Schema = schema;
            this.Type = type;
            this.DisplayName = displayName;
            this.Code = code;
            this.Description = description;
        }

        public SchemaObject Schema
        {
            get;
        }

        public IntellisenseTokenType Type
        {
            get;
        }

        public string DisplayName
        {
            get;
        }

        public string Code
        {
            get;
        }

        public string Description
        {
            get;
        }
    }
}
