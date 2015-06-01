using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.CloudFormation.Parser.Schema
{
    public static class SchemaType
    {
        public const string Object = "Object";
        public const string Array = "Array";
        public const string NamedArray = "Named-Array";
        public const string String = "String";
        public const string Number = "Number";
        public const string Boolean = "Boolean";
        public const string Json = "Json";
        public const string Policy = "Policy";
        public const string Resource = "Resource";
        public const string ConditionDefinition = "ConditionDefinition";
        public const string ConditionDeclaration = "ConditionDeclaration";
        public const string PropertyCondition = "PropertyCondition";
        public const string Reference = "Reference";
    };
}
