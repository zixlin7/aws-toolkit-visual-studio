﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.CloudFormation.Parser.Schema
{
    public class SchemaObject
    {
        Dictionary<string, SchemaObject> _propertySchemaObjects = new Dictionary<string, SchemaObject>();
        Dictionary<string, SchemaObject> _childSchemaObjects = new Dictionary<string, SchemaObject>();
        Dictionary<string, ReturnValue> _returnValues = new Dictionary<string, ReturnValue>();
        SchemaObject _defaultChildSchemaObject;

        Dictionary<string, AllowedValue> _allowedValues = new Dictionary<string, AllowedValue>();


        public SchemaObject(string schemaType, string arraySchemaType, IEnumerable<string> resourceRefType, string description, bool required, string schemaLookupProperty,
            bool disableReferences, bool disableFunctions)
        {
            this.SchemaType = schemaType;
            this.ArraySchemaType = arraySchemaType;
            this.ResourceRefType = new HashSet<string>();
            if (resourceRefType != null)
            {
                foreach (var item in resourceRefType)
                {
                    if(item != null)
                        this.ResourceRefType.Add(item);
                }
            }
            this.Required = required;
            this.Description = description;
            this.SchemaLookupProperty = schemaLookupProperty;
            this.DisableReferences = disableReferences;
            this.DisableFunctions = disableFunctions;
        }

        public void AddPropertySchemaObject(string keyName, SchemaObject schema)
        {
            this._propertySchemaObjects[keyName] = schema;
            schema.ParentSchema = this;
        }

        public void AddChildSchemaObject(string keyName, SchemaObject schema)
        {
            this._childSchemaObjects[keyName] = schema;
            schema.ParentSchema = this;
        }

        public void SetDefaultChildSchemaObject(SchemaObject schema)
        {
            this._defaultChildSchemaObject = schema;
        }

        public void AddAllowedValue(string value, string displayLabel)
        {
            if (displayLabel == null)
                displayLabel = value;

            if (!this._allowedValues.ContainsKey(value))
                this._allowedValues.Add(value, new AllowedValue(value, displayLabel));
        }

        public bool IsAllowedValue(string value)
        {
            if (this._allowedValues.Count == 0 || this._allowedValues.ContainsKey(TemplateParser.WILD_CARD))
                return true;

            return this._allowedValues.ContainsKey(value);
        }

        public IEnumerable<AllowedValue> AllowedValues
        {
            get { return this._allowedValues.Values; }
        }

        public int AllowedValuesCount
        {
            get { return this._allowedValues.Count; }
        }

        public string Description
        {
            get;
            private set;
        }

        public string SchemaType
        {
            get;
            private set;
        }

        public string ArraySchemaType
        {
            get;
            private set;
        }

        public HashSet<string> ResourceRefType
        {
            get;
            private set;
        }

        public string SchemaLookupProperty
        {
            get;
            private set;
        }

        public bool DisableReferences
        {
            get;
            private set;
        }

        public bool DisableFunctions
        {
            get;
            private set;
        }

        public SchemaObject ParentSchema
        {
            get;
            private set;
        }

        public bool Required
        {
            get;
            private set;
        }

        public bool IsValidKey(string key)
        {
            return this._childSchemaObjects.ContainsKey(key);
        }

        public SchemaObject GetPropertySchema(string key)
        {
            SchemaObject schema = null;
            if (!this._propertySchemaObjects.TryGetValue(key, out schema))
                return null;

            return schema;
        }

        public SchemaObject GetChildSchema(string key)
        {
            SchemaObject schema = null;
            if (!this._childSchemaObjects.TryGetValue(key, out schema))
                return null;

            return schema;
        }

        public SchemaObject GetDefaultChildSchemaObject()
        {
            return this._defaultChildSchemaObject;
        }

        public IEnumerable<string> PropertySchemaNames
        {
            get { return this._propertySchemaObjects.Keys; }
        }

        public IEnumerable<string> ChildSchemaNames
        {
            get { return this._childSchemaObjects.Keys; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var key in _childSchemaObjects.Keys)
            {
                if (sb.Length > 0)
                    sb.Append(", ");

                sb.Append(key);
            }
            return sb.ToString();
        }

        public IEnumerable<ReturnValue> ReturnValues
        {
            get { return this._returnValues.Values; }
        }

        public void AddReturnValue(ReturnValue rt)
        {
            this._returnValues[rt.Name] = rt;
        }

        public ReturnValue GetReturnValue(string name)
        {
            ReturnValue value = null;
            this._returnValues.TryGetValue(name, out value);
            return value;
        }

        public class ReturnValue
        {
            public ReturnValue(string name, string description)
            {
                this.Name = name;
                this.Description = description;
            }

            public string Name
            {
                get;
                private set;
            }

            public string Description
            {
                get;
                private set;
            }
        }

        public class AllowedValue
        {
            public AllowedValue(string value, string displayedLabel)
            {
                this.DisplayLabel = displayedLabel;
                this.Value = value;
            }

            public string DisplayLabel
            {
                get;
                private set;
            }

            public string Value
            {
                get;
                private set;
            }
        }
    }
}
