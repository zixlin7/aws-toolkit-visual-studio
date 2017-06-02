using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CloudFormation.Parser.Schema;

namespace Amazon.AWSToolkit.CloudFormation.Parser
{
    public enum TemplateTokenType { Object, Array, ValidKey, DuplicateKey, InvalidKey, ScalerValue, NotAllowedValue,
        IntrinsicFunction, 
        UnknownReference, InvalidTypeReference,
        UnknownResource, UnknownResourceAttribute,
        UnknownMapName, UnknownMapKey, UnknownMapValue,
        UnknownConditionType, UnknownConditionTrue, UnknownConditionFalse
    };

    public class TemplateToken
    {
        List<TemplateToken> _childTokens = new List<TemplateToken>();
        TemplateToken _parentToken;
        string _overrideDescription;

        public TemplateToken(TemplateTokenType type, SchemaObject schema, IntrinsicFunction func, string keyChain, int position, int length, string value)
        {
            this.Type = type;
            this.Postion = position;
            this.Length = length;
            this.Value = value;
            this.KeyChain = keyChain;

            this.Schema = schema;
            this.IntrinsicFunction = func;
        }

        public TemplateTokenType Type
        {
            get;
            set;
        }

        SchemaObject _schema;
        public SchemaObject Schema
        {
            get
            {
                if(this._schema == null && this.IntrinsicFunction != null)
                {
                    return SchemaDocument.DefaultJSONSchema;
                }

                return this._schema;
            }
            private set { this._schema = value; }
        }

        public IntrinsicFunction IntrinsicFunction
        {
            get;
            private set;
        }

        public string KeyChain
        {
            get;
            private set;
        }

        public string Value
        {
            get;
            private set;
        }

        public int Postion
        {
            get;
            private set;
        }

        public int Length
        {
            get;
            set;
        }

        public string Decription
        {
            get
            {
                if (!string.IsNullOrEmpty(this._overrideDescription))
                    return this._overrideDescription;

                switch (this.Type)
                {
                    case TemplateTokenType.DuplicateKey:
                        return string.Format("{0} key is already defined within this object.", this.Value);
                    case TemplateTokenType.InvalidKey:
                        return string.Format("{0} key is invalid for this object.", this.Value);
                    case TemplateTokenType.NotAllowedValue:
                        if(string.IsNullOrEmpty(this.Value))
                            return string.Format("Blank is not valid value for the property {0}.", this.KeyChain);
                        else
                            return string.Format("{0} is not an allowed value for the property {1}.", this.Value, this.KeyChain);
                    case TemplateTokenType.UnknownReference:
                        return string.Format("{0} is an unknown reference.", this.Value);
                    case TemplateTokenType.InvalidTypeReference:
                        return string.Format("{0} is an invalid type for this reference.", this.Value);
                    case TemplateTokenType.UnknownResource:
                        return string.Format("{0} is an unknown resource for this template.", this.Value);
                    case TemplateTokenType.UnknownResourceAttribute:
                        return string.Format("{0} is an unknown attribute for this resource.", this.Value);
                    case TemplateTokenType.UnknownMapName:
                        return string.Format("{0} is an unknown mapping for this template.", this.Value);
                    case TemplateTokenType.UnknownMapKey:
                        return string.Format("The key {0} does not exist in this map.", this.Value);
                    case TemplateTokenType.UnknownMapValue:
                        return string.Format("The value {0} does not exist for the key in this map.", this.Value);
                    case TemplateTokenType.UnknownConditionType:
                        return string.Format("The value {0} is not a valid condition defined in the template.", this.Value);
                    case TemplateTokenType.UnknownConditionTrue:
                        return string.Format("Condition is missing the true clause");
                    case TemplateTokenType.UnknownConditionFalse:
                        return string.Format("Condition is missing the false clause");
                }
                if (this.Schema != null)
                    return this.Schema.Description;
                if (this.IntrinsicFunction != null)
                    return this.IntrinsicFunction.Description;

                return null;
            }
            set
            {
                this._overrideDescription = value;
            }
        }

        public void AddChildTemplateToken(TemplateToken token)
        {
            this._childTokens.Add(token);
            token.ParentToken = this;
        }

        public IList<TemplateToken> ChildTokens
        {
            get { return this._childTokens; }
        }

        public TemplateToken ParentToken
        {
            get { return this._parentToken; }
            private set { this._parentToken = value;}
        }

        public SchemaObject ParentSchema
        {
            get
            {
                var level = this.ParentToken;
                while (level != null)
                {
                    if (level.Schema != null)
                        return level.Schema;

                    level = level.ParentToken;
                }

                return null;
            }
        }
    }
}
