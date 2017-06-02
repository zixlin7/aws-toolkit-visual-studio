using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.CloudFormation.Parser.Schema
{
    public class SchemaDocument
    {
        Dictionary<string, IntrinsicFunction> _intrinsicFunctions;
        Dictionary<string, PseudoParameter> _pseudoParameters;
        public SchemaDocument(SchemaObject rootSchemaObject, Dictionary<string, IntrinsicFunction> intrinsicFunctions, Dictionary<string, PseudoParameter> pseudoParameters)
        {
            this.RootSchemaObject = rootSchemaObject;
            this._intrinsicFunctions = intrinsicFunctions;
            this._pseudoParameters = pseudoParameters;

        }

        public SchemaObject RootSchemaObject
        {
            get;
            private set;
        }

        public static SchemaObject DefaultJSONSchema { get; } = DefaultJSONSchema = new SchemaObject(SchemaType.Json, null, null, null, false, null, false, false);

        public IntrinsicFunction GetIntrinsicFunction(string name)
        {
            IntrinsicFunction func = null;
            this._intrinsicFunctions.TryGetValue(name, out func);
            return func;
        }

        public IEnumerable<IntrinsicFunction> IntrinsicFunctions
        {
            get { return this._intrinsicFunctions.Values; }
        }

        public PseudoParameter GetPseudoParameter(string name)
        {
            PseudoParameter val = null;
            this._pseudoParameters.TryGetValue(name, out val);
            return val;
        }

        public IEnumerable<PseudoParameter> PseudoParameters
        {
            get { return this._pseudoParameters.Values; }
        }


        HashSet<string> _awsCustomParameterTypes;
        public HashSet<string> AWSCustomParameterTypes
        {
            get
            {
                if(this._awsCustomParameterTypes == null)
                {
                    lock(this)
                    {
                        if(this._awsCustomParameterTypes == null)
                        {
                            this._awsCustomParameterTypes = new HashSet<string>();

                            var parameters = this.RootSchemaObject.GetPropertySchema("Parameters");
                            var parameterTypes = parameters.GetDefaultChildSchemaObject().GetPropertySchema("Type").AllowedValues;

                            foreach (var type in parameterTypes)
                            {
                                if (type.Value.StartsWith("AWS::"))
                                    this._awsCustomParameterTypes.Add(type.Value);
                            }
                        }
                    }
                }

                return this._awsCustomParameterTypes;
            }
        }
    }
}
