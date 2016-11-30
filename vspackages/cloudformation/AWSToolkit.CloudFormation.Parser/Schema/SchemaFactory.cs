using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using ThirdParty.Json.LitJson;

using log4net;



namespace Amazon.AWSToolkit.CloudFormation.Parser.Schema
{
    public static class SchemaFactory
    {
        const string CLOUDFORMATION_SCHEMA_S3_LOCATION = "CloudFormationSchema/CloudFormationV1.schema";
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(SchemaFactory));

        static SchemaDocument SCHEMA_OBJECT;
        public static SchemaDocument GetSchema()
        {
            if (SCHEMA_OBJECT == null)
                SCHEMA_OBJECT = LoadSchema();

            return SCHEMA_OBJECT;
        }

        static SchemaDocument LoadSchema()
        {
            LOGGER.Debug("Begin loading CloudFormation Schema");
            try
            {
                string content = S3FileFetcher.Instance.GetFileContent(CLOUDFORMATION_SCHEMA_S3_LOCATION, S3FileFetcher.CacheMode.IfDifferent);

                if (content == null)
                {
                    using (StreamReader reader = new StreamReader(typeof(SchemaFactory).Assembly.GetManifestResourceStream("Amazon.AWSToolkit.CloudFormation.Parser.Schema.CloudFormationV1.schema")))
                        content = reader.ReadToEnd();
                }

                var jsonData = JsonMapper.ToObject(content);
                var rootSchemaData = jsonData["root-schema-object"];
                var rootSchemaObject = ConvertToSchema(rootSchemaData);

                var intrinsicFunctionData = jsonData["intrinsic-functions"];
                var intrinsicFunctions = ConvertToIntrinsicFunctions(intrinsicFunctionData);

                var pseudoParametersData = jsonData["pseudo-parameters"];
                var pseudoParameters = ConvertToPseudoParameters(pseudoParametersData);

                return new SchemaDocument(rootSchemaObject, intrinsicFunctions, pseudoParameters);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error loading CloudFormation schema", e);
                throw;
            }
        }

        static Dictionary<string, PseudoParameter> ConvertToPseudoParameters(JsonData jsonData)
        {
            var vals = new Dictionary<string, PseudoParameter>();

            if (!jsonData.IsObject)
                return vals;

            foreach (var name in jsonData.PropertyNames)
            {
                var data = jsonData[name];

                string type = null;
                if (data["type"] != null && data["type"].IsString)
                    type = (string)data["type"];

                string description = null;
                if (data["description"] != null && data["description"].IsString)
                    description = (string)data["description"];

                string arrayType = null;
                if (data["array-type"] != null && data["array-type"].IsString)
                    arrayType = (string)data["array-type"];

                var func = new PseudoParameter(name, type, arrayType, description);
                vals[name] = func;
            }

            return vals;
        }


        static Dictionary<string, IntrinsicFunction> ConvertToIntrinsicFunctions(JsonData jsonData)
        {
            var funcs = new Dictionary<string, IntrinsicFunction>();

            if (!jsonData.IsObject)
                return funcs;

            foreach (var funcName in jsonData.PropertyNames)
            {
                var funcData = jsonData[funcName];

                string parameter = null;
                if (funcData["parameter"] != null && funcData["parameter"].IsString)
                    parameter = (string)funcData["parameter"];

                string returnType = null;
                if (funcData["return-type"] != null && funcData["return-type"].IsString)
                    returnType = (string)funcData["return-type"];

                string description = null;
                if (funcData["description"] != null && funcData["description"].IsString)
                    description = (string)funcData["description"];

                string skeleton = null;
                if (funcData["skeleton"] != null && funcData["skeleton"].IsString)
                    skeleton = (string)funcData["skeleton"];

                var func = new IntrinsicFunction(funcName, parameter, returnType, description, skeleton);
                funcs[funcName] = func;
            }

            return funcs;
        }

        static SchemaObject ConvertToSchema(JsonData jsonData)
        {
            var type = (string)jsonData["type"];
            string arrayType = null;
            if (type == SchemaType.Array && jsonData["array-type"] != null && jsonData["array-type"].IsString)
                arrayType = (string)jsonData["array-type"];

            string[] resourceRefType = null;
            if (jsonData["resource-ref-type"] != null)
            {
                if(jsonData["resource-ref-type"].IsString)
                {
                    resourceRefType = new string[]{(string)jsonData["resource-ref-type"]};
                }
                else if (jsonData["resource-ref-type"].IsArray)
                {
                    resourceRefType = new string[jsonData["resource-ref-type"].Count];

                    int i = 0;
                    foreach (JsonData item in jsonData["resource-ref-type"])
                    {
                        if(item.IsString)
                            resourceRefType[i++] = (string)item;
                    }
                }
            }            

            string description = null;
            if (jsonData["description"] != null && jsonData["description"].IsString)
                description = (string)jsonData["description"];

            bool required = false;
            if (jsonData["required"] != null && jsonData["required"].IsBoolean)
                required = (bool)jsonData["required"];

            string schemaLookupProperty = null;
            if (jsonData["schema-lookup-property"] != null && jsonData["schema-lookup-property"].IsString)
                schemaLookupProperty = (string)jsonData["schema-lookup-property"];

            bool disableReferences = false;
            if (jsonData["disable-refs"] != null && jsonData["disable-refs"].IsBoolean)
                disableReferences = (bool)jsonData["disable-refs"];

            bool disableFunctions = false;
            if (jsonData["disable-functions"] != null && jsonData["disable-functions"].IsBoolean)
                disableFunctions = (bool)jsonData["disable-functions"];

            var schema = new SchemaObject(type, arrayType, resourceRefType, description, required, schemaLookupProperty, disableReferences, disableFunctions);

            var properties = jsonData["properties"];
            if (properties != null)
            {
                foreach (var key in properties.PropertyNames)
                {
                    var data = properties[key];
                    var schemaData = ConvertToSchema(data);
                    schema.AddPropertySchemaObject(key, schemaData);
                }
            }

            var childSchemas = jsonData["child-schemas"];
            if (childSchemas != null)
            {
                foreach (var key in childSchemas.PropertyNames)
                {
                    var data = childSchemas[key];
                    var schemaData = ConvertToSchema(data);
                    schema.AddChildSchemaObject(key, schemaData);
                }
            }

            var defaultChildSchema = jsonData["default-child-schema"];
            if (defaultChildSchema != null)
            {
                var schemaData = ConvertToSchema(defaultChildSchema);
                schema.SetDefaultChildSchemaObject(schemaData);
            }

            var allowedValues = jsonData["allowed-values"];
            if (allowedValues != null && allowedValues.IsArray)
            {
                foreach (JsonData item in allowedValues)
                {
                    if (item.IsObject)
                    {
                        string value = null;
                        if (item["value"] != null && item["value"].IsString)
                            value = (string)item["value"];

                        string displayLabel = null;
                        if (item["display-label"] != null && item["display-label"].IsString)
                            displayLabel = (string)item["display-label"];

                        if (string.IsNullOrWhiteSpace(displayLabel))
                            value = displayLabel;

                        if (!string.IsNullOrWhiteSpace(value))
                            schema.AddAllowedValue(value, displayLabel);
                    }
                    else
                    {
                        schema.AddAllowedValue(item.ToString(), item.ToString());
                    }
                }
            }

            var returnedValues = jsonData["return-values"];
            if (returnedValues != null && returnedValues.IsArray)
            {
                foreach (JsonData data in returnedValues)
                {
                    string returnName = (string)data["name"];
                    string returnDescription = null;
                    if (data["description"] != null)
                        returnDescription = (string)data["description"];

                    schema.AddReturnValue(new SchemaObject.ReturnValue(returnName, returnDescription));
                }
            }

            return schema;
        }
    }
}
