using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CloudFormation.Parser.Schema;

using ThirdParty.Json.LitJson;


namespace Amazon.AWSToolkit.CloudFormation.Parser
{
    public class TemplateParser
    {
        public const string WILD_CARD = "*";

        const string CONDITION_DISPLAY = "{0} (Condition)";
        const string PARAM_REF_DISPLAY = "{0} (Reference Parameter)";
        const string RESOURCE_REF_DISPLAY = "{0} (Reference Resource)";
        const string MAPPING_DISPLAY = "{0} (Mapping)";
        const string MAPPING_KEY_DISPLAY = "{0} (Mapping Key)";
        const string MAPPING_VALUE_DISPLAY = "{0} (Mapping Value)";

        static Func<TemplateToken, int, bool> isCaretInside = (token, caret) => (token != null && token.Postion <= caret && caret <= token.Postion + token.Length);

        static Dictionary<string, string> BASIC_RESOURCES_TEMPLATES = new Dictionary<string, string>();


        SchemaDocument _schema;
        JsonDocument _jsonDocument;

        TemplateToken _rootTemplateToken = null;
        List<TemplateToken> _highlightedTemplateTokens;
        List<TemplateToken> _refTokensToPostValidate;
        List<Tuple<TemplateToken,TemplateToken>> _fnGetAttTokensToPostValidate;
        List<Tuple<TemplateToken, TemplateToken, TemplateToken>> _fnIfTokensToPostValidate;
        List<Tuple<TemplateToken, TemplateToken, TemplateToken>> _fnFindInMapTokensToPostValidate;
        List<TemplateToken> _conditionsToPostValidate;
        List<TemplateToken> _resourceTokensToPostValidate;

        List<IntellisenseToken> _intellisenseToken;
        IntellisensePositionType? _intellisensePositionType;


        int _intellisenseStartingPostion = -1;
        int _intellisenseEndingPostion = -1;
        string _intellisenseSchemaType;
        HashSet<string> _intellisenseResourceRefType;
        string[] _intellisenseKeyChain;
        bool _disableReferencesForIntellisense;

        int _caretPosition;
        bool _caretPositionFound;

        Dictionary<string, List<string>> _referencesByType;
        List<string> _allDefinedConditions;

        public ParserResults Parse(string document)
        {
            return Parse(document, -1);
        }

        public ParserResults Parse(string document, int caretPosition)
        {
            this._rootTemplateToken = null;
            this._highlightedTemplateTokens = new List<TemplateToken>();
            this._refTokensToPostValidate = new List<TemplateToken>();
            this._fnGetAttTokensToPostValidate = new List<Tuple<TemplateToken, TemplateToken>>();
            this._fnIfTokensToPostValidate = new List<Tuple<TemplateToken, TemplateToken, TemplateToken>>();
            this._resourceTokensToPostValidate = new List<TemplateToken>();
            this._fnFindInMapTokensToPostValidate = new List<Tuple<TemplateToken, TemplateToken, TemplateToken>>();
            this._intellisenseToken = new List<IntellisenseToken>();
            this._caretPositionFound = false;
            this._caretPosition = caretPosition;

            this._intellisensePositionType = null;
            this._intellisenseStartingPostion = -1;
            this._intellisenseEndingPostion = -1;
            this._disableReferencesForIntellisense = false;

            this._referencesByType = new Dictionary<string, List<string>>();

            this._conditionsToPostValidate = new List<TemplateToken>();
            this._allDefinedConditions = new List<string>();

            this._jsonDocument = new JsonDocument(document);
            this._schema = SchemaFactory.GetSchema();

            // Schema was failed to be loaded so skip parsing the document
            if (this._schema == null)
                return null;

            try
            {
                ParseObject(this._schema.RootSchemaObject);
            }
            catch (Exception) { }

            try
            {
                // Validate the references tokens to make sure they are point to valid references.
                PostValidateRefTokens();

                var resourcesDefined = GetDefinedResources();

                // Validate the resource and attributes used in any Fn:GetAtt function calls
                PostValidateFnGetAttTokens(resourcesDefined);

                // Make sure all attribute (i.e. DependsOn) are pointing to valid resources defined in the template
                PostValidateResourceTokens(resourcesDefined);

                // Make sure all Fn::FindInMap functions are pointing to valid maps and map attributes
                PostValidateFnFindInMapTokens();

                // Make sure all Conditions used by resources are valid.
                PostValidateConditionTokens();

                // Add any reference intellisense items
                AddReferencesToIntellisense();
            }
            catch (Exception) { }

            return new ParserResults(this._rootTemplateToken, this._highlightedTemplateTokens, this._intellisenseToken, this._intellisenseStartingPostion, this._intellisenseEndingPostion, new List<ErrorToken>());
        }

        // Gets all the defined resources in the document
        Dictionary<string, Tuple<string, SchemaObject>> GetDefinedResources()
        {
            SchemaObject resourceRootSchema = this._schema.RootSchemaObject.GetPropertySchema("Resources");
            Dictionary<string, Tuple<string, SchemaObject>> resourcesDefined = new Dictionary<string, Tuple<string, SchemaObject>>();
            foreach (var kvp in this._referencesByType)
            {
                if (!kvp.Key.StartsWith("AWS::"))
                    continue;

                SchemaObject resourceSchema = resourceRootSchema.GetChildSchema(kvp.Key);

                var tuple = new Tuple<string, SchemaObject>(kvp.Key, resourceSchema);
                foreach (var resourceName in kvp.Value)
                    resourcesDefined[resourceName] = tuple;
            }
            return resourcesDefined;
        }

        void GetDefinedMaps(out Dictionary<string, Dictionary<string, HashSet<string>>> definedMaps, out HashSet<string> uniqueSetOfValues)
        {
            TemplateToken rootMappingToken = null;
            foreach (var token in this._rootTemplateToken.ChildTokens)
            {
                if (string.Equals(token.Value, "Mappings"))
                {
                    // Check to see if the mapping token has a value
                    if (token.ChildTokens.Count == 1)
                        rootMappingToken = token.ChildTokens[0];
                    break;
                }
            }

            definedMaps = new Dictionary<string, Dictionary<string, HashSet<string>>>();
            uniqueSetOfValues = new HashSet<string>();
            if (rootMappingToken != null)
            {
                foreach (var mapNameToken in rootMappingToken.ChildTokens)
                {
                    if (string.IsNullOrEmpty(mapNameToken.Value) || definedMaps.ContainsKey(mapNameToken.Value))
                        continue;

                    Dictionary<string, HashSet<string>> mapKeyValuePair = new Dictionary<string, HashSet<string>>();
                    definedMaps[mapNameToken.Value] = mapKeyValuePair;

                    if (mapNameToken.ChildTokens.Count != 1)
                        continue;
                    foreach (var makKeyName in mapNameToken.ChildTokens[0].ChildTokens)
                    {
                        if (makKeyName.ChildTokens.Count != 1 || makKeyName.ChildTokens[0].ChildTokens.Count == 0)
                            continue;

                        HashSet<string> values = new HashSet<string>();
                        mapKeyValuePair[makKeyName.Value] = values;
                        foreach (var valueToken in makKeyName.ChildTokens[0].ChildTokens)
                        {
                            if (string.IsNullOrEmpty(valueToken.Value))
                                continue;

                            if (!values.Contains(valueToken.Value))
                                values.Add(valueToken.Value);
                            if (!uniqueSetOfValues.Contains(valueToken.Value))
                                uniqueSetOfValues.Add(valueToken.Value);
                        }
                    }
                }
            }
        }

        void AddReferencesToIntellisense()
        {
            if (this._disableReferencesForIntellisense)
                return;

            if (this._intellisensePositionType.HasValue && this._intellisensePositionType.Value == IntellisensePositionType.Value &&
                this._intellisenseKeyChain != null && this._intellisenseKeyChain.Length > 0 && !string.Equals(this._intellisenseKeyChain[0], "Parameters", StringComparison.InvariantCultureIgnoreCase))
            {
                if (this._intellisenseSchemaType == null || this._intellisenseSchemaType == SchemaType.Json || this._intellisenseSchemaType == SchemaType.Policy)
                {
                    foreach (var kvp in this._referencesByType)
                    {
                        foreach (var reference in kvp.Value)
                        {
                            var displayName = string.Format(kvp.Key.StartsWith("AWS::") ? RESOURCE_REF_DISPLAY : PARAM_REF_DISPLAY, reference);
                            var codeSnippet = string.Format("{{ \"Ref\" : \"{0}\" }}", reference);

                            var token = new IntellisenseToken(null, IntellisenseTokenType.Reference, displayName, codeSnippet, null);
                            this._intellisenseToken.Add(token);
                        }
                    }
                }
                else if(this._intellisenseSchemaType != null)
                {
                    foreach (var kvp in this._referencesByType)
                    {
                        if (IsRefType(kvp.Key))
                            continue;

                        foreach (var name in kvp.Value)
                        {
                            var token = new IntellisenseToken(null, IntellisenseTokenType.Reference, string.Format(PARAM_REF_DISPLAY, name), string.Format("{{ \"Ref\" : \"{0}\" }}", name), null);
                            this._intellisenseToken.Add(token);
                        }
                    }

                    if (this._intellisenseResourceRefType != null && this._intellisenseResourceRefType.Count > 0)
                    {
                        var refs = FindResourceReferences(this._intellisenseResourceRefType);
                        foreach (var name in refs)
                        {
                            var token = new IntellisenseToken(null, IntellisenseTokenType.Reference, string.Format(RESOURCE_REF_DISPLAY, name), string.Format("{{ \"Ref\" : \"{0}\" }}", name), null);
                            this._intellisenseToken.Add(token);
                        }
                    }
                    // Outputs can have any references
                    else if(this._intellisenseKeyChain != null && this._intellisenseKeyChain[0] == "Outputs")
                    {
                        foreach (var kvp in this._referencesByType)
                        {
                            if (!kvp.Key.StartsWith("AWS::"))
                                continue;

                            foreach (var name in kvp.Value)
                            {
                                var token = new IntellisenseToken(null, IntellisenseTokenType.Reference, string.Format(RESOURCE_REF_DISPLAY, name), string.Format("{{ \"Ref\" : \"{0}\" }}", name), null);
                                this._intellisenseToken.Add(token);
                            }
                        }
                    }
                }

                foreach (var pseudoParam in this._schema.PseudoParameters)
                {
                    if (this._intellisenseSchemaType == null || pseudoParam.Type == this._intellisenseSchemaType)
                    {
                        var intelToken = new IntellisenseToken(null, IntellisenseTokenType.Reference, string.Format(PARAM_REF_DISPLAY, pseudoParam), string.Format("{{ \"Ref\" : \"{0}\" }}", pseudoParam), pseudoParam.Description);
                        this._intellisenseToken.Add(intelToken);
                    }
                }

            }
        }

        List<string> FindResourceReferences(HashSet<string> resourceRefTypes)
        {
            List<string> resourceReference = new List<string>();

            if (resourceRefTypes != null)
            {
                foreach (var resourceRefType in resourceRefTypes)
                {
                    List<String> rf = new List<string>();
                    if(this._referencesByType.TryGetValue(resourceRefType, out rf))
                        resourceReference.AddRange(rf);
                }
            }

            return resourceReference;
        }

        List<string> FindParameterReferences()
        {
            List<string> references = new List<string>();

            foreach (var kvp in this._referencesByType)
            {
                if(IsRefType(kvp.Key))
                    continue;

                references.AddRange(kvp.Value);
            }

            return references;
        }

        void PostValidateRefTokens()
        {
            if (this._refTokensToPostValidate.Count == 0)
                return;

            // Build a list of all possible reference so we can inform the user
            // if the bad reference is because it is unknown or just the wrong type.
            HashSet<string> allPossibleResources = new HashSet<string>();
            foreach (var list in this._referencesByType.Values)
            {
                foreach (var reference in list)
                    allPossibleResources.Add(reference);
            }

            foreach (var token in this._refTokensToPostValidate)
            {
                bool useGlobalList = false;
                TemplateToken keyToken = null;
                // The first parent is to the "Ref" key so we need to go they key of intrinsic function
                if(token.ParentToken != null && token.ParentToken.ParentToken != null)
                    keyToken = token.ParentToken.ParentToken;

                string schemaType = null;
                List<string> schemaTypeReferences;
                List<string> resourceReferences = null;
                if (keyToken != null && keyToken.Schema != null)
                {
                    schemaTypeReferences = FindParameterReferences();

                    var resourceReferenceType = keyToken.Schema.ResourceRefType;
                    if (keyToken.Schema.SchemaType == SchemaType.Json || keyToken.Schema.SchemaType == SchemaType.Policy ||
                        keyToken.Schema.ArraySchemaType == SchemaType.Json || keyToken.Schema.ArraySchemaType == SchemaType.Policy)
                    {
                        foreach (var kvp in this._referencesByType)
                        {
                            if (kvp.Key.StartsWith("AWS::"))
                            {
                                if (resourceReferences == null)
                                    resourceReferences = new List<string>();

                                resourceReferences.AddRange(kvp.Value);
                            }
                        }
                    }
                    else if (resourceReferenceType.Count > 0)
                    {
                        resourceReferences = FindResourceReferences(resourceReferenceType);
                    }
                }
                else
                {
                    // The reference is most likely inside a metadata section where there is no key schema.  
                    // In that case lets just determine if the reference exists.
                    schemaTypeReferences = allPossibleResources.ToList();
                    useGlobalList = true;
                }

                if (this._schema.GetPseudoParameter(token.Value) != null)
                {
                    token.Decription = this._schema.GetPseudoParameter(token.Value).Description;
                }
                else if (!(schemaTypeReferences != null && schemaTypeReferences.Contains(token.Value) || (resourceReferences != null && resourceReferences.Contains(token.Value))))
                {
                    if (allPossibleResources.Contains(token.Value))
                    {
                        // If this is a reference in the output section then all references are valid or the resource ref is wild card
                        if (!(token.KeyChain.StartsWith("/Outputs") || (keyToken != null && keyToken.Schema != null && keyToken.Schema.ResourceRefType.Contains(WILD_CARD))))
                        {
                            token.Type = TemplateTokenType.InvalidTypeReference;
                        }
                    }
                    else
                        token.Type = TemplateTokenType.UnknownReference;
                }

                if (token.KeyChain.StartsWith("/Outputs") || (keyToken != null && keyToken.Schema != null && keyToken.Schema.ResourceRefType.Contains(WILD_CARD)))
                {
                    useGlobalList = true;
                }

                // Check to see if there are any intellisense tokens that should be generated for this reference value
                PostIntellisenseRefTokens(schemaType, useGlobalList, schemaTypeReferences, resourceReferences, allPossibleResources, token);
            }
        }

        void PostIntellisenseRefTokens(string schemaType, bool useGlobalList, List<string> schemaTypeReference, List<string> resourceReference, HashSet<string> allPossibleResources, TemplateToken refToken)
        {
            if (this._intellisensePositionType.HasValue && this._intellisensePositionType.Value == IntellisensePositionType.RefValue)
            {
                if (isCaretInside(refToken, this._caretPosition))
                {
                    if (!useGlobalList)
                    {
                        // Add all parameters because CloudFormation will do type conversion like numbers to strings
                        foreach (var kvp in this._referencesByType)
                        {
                            if (IsRefType(kvp.Key))
                                continue;

                            foreach (var name in kvp.Value)
                            {
                                var token = new IntellisenseToken(null, IntellisenseTokenType.Reference, string.Format(PARAM_REF_DISPLAY, name), string.Format("\"{0}\"", name), null);
                                this._intellisenseToken.Add(token);
                            }
                        }
                        if (resourceReference != null)
                        {
                            foreach (var reference in resourceReference)
                            {
                                var intelToken = new IntellisenseToken(null, IntellisenseTokenType.Reference, string.Format(RESOURCE_REF_DISPLAY, reference), string.Format("\"{0}\"", reference), null);
                                this._intellisenseToken.Add(intelToken);
                            }
                        }
                    }
                    else
                    {
                        // Using the global list so we can't easily distinghing between parameters and resources
                        foreach (var reference in allPossibleResources)
                        {
                            var intelToken = new IntellisenseToken(null, IntellisenseTokenType.Reference, string.Format("Reference {0}", reference), string.Format("\"{0}\"", reference), null);
                            this._intellisenseToken.Add(intelToken);
                        }
                    }

                    foreach (var pseudoParam in this._schema.PseudoParameters)
                    {
                        if (schemaType == null || pseudoParam.Type == schemaType)
                        {
                            var intelToken = new IntellisenseToken(null, IntellisenseTokenType.Reference, string.Format(PARAM_REF_DISPLAY, pseudoParam), string.Format("\"{0}\"", pseudoParam), pseudoParam.Description);
                            this._intellisenseToken.Add(intelToken);
                        }
                    }
                }
            }
        }

        void PostValidateFnGetAttTokens(Dictionary<string, Tuple<string, SchemaObject>> resourcesDefined)
        {
            if (this._fnGetAttTokensToPostValidate.Count == 0)
                return;

            foreach (var fnGetAttTuple in this._fnGetAttTokensToPostValidate)
            {
                var resourceToken = fnGetAttTuple.Item1;
                var attributeToken = fnGetAttTuple.Item2;
                if ((resourceToken == null || resourceToken.Value == null) && attributeToken != null)
                {
                    attributeToken.Type = TemplateTokenType.UnknownResourceAttribute;
                }
                else if (resourceToken.Value != null && !resourcesDefined.ContainsKey(resourceToken.Value))
                {
                    resourceToken.Type = TemplateTokenType.UnknownResource;
                    attributeToken.Type = TemplateTokenType.UnknownResourceAttribute;
                }
                else if(resourceToken.Value != null)
                {
                    var resourceTuple = resourcesDefined[resourceToken.Value];
                    var resourceType = resourceTuple.Item1;
                    SchemaObject resourceSchema = resourceTuple.Item2;
                    if (resourceSchema != null)
                    {
                        resourceToken.Decription = BuildResourceReturnDescription(resourceType, resourceSchema);

                        if (attributeToken != null)
                        {
                            var returnValue = resourceSchema.GetReturnValue(attributeToken.Value);
                            if (returnValue == null && resourceSchema.GetReturnValue(WILD_CARD) == null)
                                attributeToken.Type = TemplateTokenType.UnknownResourceAttribute;
                            else if(returnValue != null)
                                attributeToken.Decription = returnValue.Description;
                        }
                    }
                }

                PostIntellisenseFnGetAttTokens(resourcesDefined, resourceToken, attributeToken);
            }
        }

        void PostIntellisenseFnGetAttTokens(Dictionary<string, Tuple<string, SchemaObject>> resourcesDefined,
            TemplateToken resourceToken, TemplateToken attributeToken)
        {
            if (this._intellisensePositionType.HasValue && this._intellisensePositionType.Value == IntellisensePositionType.FnGetAttResource)
            {
                if (isCaretInside(resourceToken, this._caretPosition))
                {
                    foreach (var reference in resourcesDefined.Keys)
                    {
                        var codeSnippet = string.Format("\"{0}\"", reference);
                        if (resourceToken.Length == 0)
                            codeSnippet += ", \"\"";

                        var intelToken = new IntellisenseToken(null, IntellisenseTokenType.Reference, string.Format(RESOURCE_REF_DISPLAY, reference), codeSnippet, null);
                        this._intellisenseToken.Add(intelToken);
                    }
                }
            }
            else if (this._intellisensePositionType.HasValue && this._intellisensePositionType.Value == IntellisensePositionType.FnGetAttAttribute)
            {
                if (isCaretInside(attributeToken, this._caretPosition) && resourcesDefined.ContainsKey(resourceToken.Value))
                {
                    if (resourcesDefined.ContainsKey(resourceToken.Value))
                    {
                        var returnValues = resourcesDefined[resourceToken.Value].Item2.ReturnValues;
                        if (returnValues != null)
                        {
                            foreach (var returnValue in returnValues)
                            {
                                if (!WILD_CARD.Equals(returnValue))
                                {
                                    var intelToken = new IntellisenseToken(null, IntellisenseTokenType.Reference, returnValue.Name, string.Format("\"{0}\"", returnValue.Name), returnValue.Description);
                                    this._intellisenseToken.Add(intelToken);
                                }
                            }
                        }
                    }
                }
            }
        }

        void PostValidateResourceTokens(Dictionary<string, Tuple<string, SchemaObject>> resourcesDefined)
        {
            if (this._resourceTokensToPostValidate.Count == 0)
                return;

            foreach (var token in this._resourceTokensToPostValidate)
            {
                if (!resourcesDefined.ContainsKey(token.Value))
                {
                    token.Type = TemplateTokenType.UnknownResource;
                }

                if (isCaretInside(token, this._caretPosition))
                {
                    foreach (var resource in resourcesDefined.Keys)
                    {
                        if(token.KeyChain.Contains(string.Format("/{0}/", resource)))
                            continue;

                        var intelToken = new IntellisenseToken(null, IntellisenseTokenType.Reference, resource, string.Format("\"{0}\"", resource), null);
                        this._intellisenseToken.Add(intelToken);
                    }
                }
            }
        }

        void PostValidateConditionTokens()
        {
            foreach (var conditionToken in this._conditionsToPostValidate)
            {
                if (conditionToken.Type != TemplateTokenType.ScalerValue || !this._allDefinedConditions.Contains(conditionToken.Value))
                    conditionToken.Type = TemplateTokenType.UnknownConditionType;

                PostIntellisenseConditions(conditionToken);
            }

            bool foundIntellisense = false;
            // Starting from the end of the list of tokens so we can find the most exact token with intellisense first.
            foreach (var propertyConditionToken in this._fnIfTokensToPostValidate.OrderByDescending(x => 
                {
                    if (x.Item1 == null)
                        return -1;
                    else
                        return x.Item1.Postion;
                }))
            {
                if (propertyConditionToken.Item1 != null && !this._allDefinedConditions.Contains(propertyConditionToken.Item1.Value))
                    propertyConditionToken.Item1.Type = TemplateTokenType.UnknownConditionType;

                if (!foundIntellisense && propertyConditionToken.Item1 != null)
                {
                    foundIntellisense = PostIntellisenseConditions(propertyConditionToken.Item1);
                }

                if (!foundIntellisense && propertyConditionToken.Item2 != null)
                {
                    foundIntellisense = PostIntellisenseConditions(propertyConditionToken.Item2);
                }

                if (!foundIntellisense && propertyConditionToken.Item3 != null)
                {
                    foundIntellisense = PostIntellisenseConditions(propertyConditionToken.Item3);
                }
            }
        }

        bool PostIntellisenseConditions(TemplateToken conditionToken)
        {
            if (isCaretInside(conditionToken, this._caretPosition))
            {
                if (this._intellisensePositionType.HasValue &&
                    (this._intellisensePositionType.Value == IntellisensePositionType.Condition ||
                    this._intellisensePositionType.Value == IntellisensePositionType.FnIfCondition))
                {
                    foreach (var definedCondition in this._allDefinedConditions)
                    {
                        var token = new IntellisenseToken(null, IntellisenseTokenType.Condition, string.Format(CONDITION_DISPLAY, definedCondition), string.Format("\"{0}\"", definedCondition), null);
                        this._intellisenseToken.Add(token);
                    }

                    return true;
                }

                if (this._intellisensePositionType.HasValue &&
                    (this._intellisensePositionType.Value == IntellisensePositionType.FnIfTrue ||
                    this._intellisensePositionType.Value == IntellisensePositionType.FnIfFalse))
                {
                    this.AddParameterReferencesToIntellisense();

                    if (conditionToken.ParentSchema != null)
                    {
                        AddAllowedValuesToIntellisense(conditionToken.ParentSchema);
                    }

                    return true;
                }
            }

            return false;
        }

        void PostValidateFnFindInMapTokens()
        {
            if (this._fnFindInMapTokensToPostValidate.Count == 0)
                return;

            Dictionary<string, Dictionary<string, HashSet<string>>> definedMaps;
            HashSet<string> uniqueSetOfValues;
            GetDefinedMaps(out definedMaps, out uniqueSetOfValues);

            foreach (var tuple in this._fnFindInMapTokensToPostValidate)
            {
                TemplateToken mapNameToken = tuple.Item1;
                TemplateToken mapKeyNameToken = tuple.Item2;
                TemplateToken valueNameToken = tuple.Item3;

                Dictionary<string, HashSet<string>> mapValues = null;
                // Check to see if the map name exists
                if (mapNameToken.Type == TemplateTokenType.Object)
                {
                    continue;
                }
                else
                {
                    if (mapNameToken != null && !definedMaps.TryGetValue(mapNameToken.Value, out mapValues))
                    {
                        mapNameToken.Type = TemplateTokenType.UnknownMapName;
                    }
                }

                // No defined map so the other tokens must be invalid if the exist
                if(mapValues == null)
                {
                    if(mapKeyNameToken != null)
                        mapKeyNameToken.Type = TemplateTokenType.UnknownMapKey;
                    if(valueNameToken != null)
                        valueNameToken.Type = TemplateTokenType.UnknownMapValue;
                }
                else 
                {
                    // Check to see if the key exists, if it doesn't then mark both the key and value invalid
                    // If the key name is valid then this is mostly likely a Ref to a parameter.
                    if(mapKeyNameToken != null && mapKeyNameToken.Value != null && !mapValues.ContainsKey(mapKeyNameToken.Value))
                    {
                        mapKeyNameToken.Type = TemplateTokenType.UnknownMapKey;
                        if(valueNameToken != null)
                            valueNameToken.Type = TemplateTokenType.UnknownMapValue;
                    }
                    // Check to see if the value is defined for that key.
                    // If the valueNameToken or has a null value then this is most likely a Ref to a parameter.
                    else if (valueNameToken != null && valueNameToken.Value != null
                        && !uniqueSetOfValues.Contains(valueNameToken.Value))
                    {
                        valueNameToken.Type = TemplateTokenType.UnknownMapValue;
                    }
                }

                PostIntellisenseFnFindInMapTokens(definedMaps, uniqueSetOfValues, mapNameToken, mapKeyNameToken, valueNameToken);
            }
        }

        void PostIntellisenseFnFindInMapTokens(Dictionary<string, Dictionary<string, HashSet<string>>> definedMaps, HashSet<string> uniqueSetOfValues,
            TemplateToken mapNameToken, TemplateToken mapKeyNameToken, TemplateToken valueNameToken)
        {
            bool addParameterReferences = false;

            // Intellisense for the map name
            if (this._intellisensePositionType.HasValue && this._intellisensePositionType.Value == IntellisensePositionType.FnFindInMapMapName)
            {
                if (isCaretInside(mapNameToken, this._caretPosition))
                {
                    foreach (var mapName in definedMaps.Keys)
                    {
                        var intelToken = new IntellisenseToken(null, IntellisenseTokenType.Reference, string.Format(MAPPING_DISPLAY, mapName), string.Format("\"{0}\"", mapName), null);
                        this._intellisenseToken.Add(intelToken);
                    }
                }
            }
            // Intellisense for the map key
            else if (this._intellisensePositionType.HasValue && this._intellisensePositionType.Value == IntellisensePositionType.FnFindInMapKey)
            {
                if (isCaretInside(mapKeyNameToken, this._caretPosition))
                {
                    Dictionary<string, HashSet<string>> keys;
                    if (mapNameToken.Value != null && definedMaps.TryGetValue(mapNameToken.Value, out keys))
                    {
                        foreach (var mapName in keys.Keys)
                        {
                            var intelToken = new IntellisenseToken(null, IntellisenseTokenType.Reference, string.Format(MAPPING_KEY_DISPLAY, mapName), string.Format("\"{0}\"", mapName), null);
                            this._intellisenseToken.Add(intelToken);
                        }
                    }

                    addParameterReferences = true;
                }
            }
            // Intellisense for the map value
            else if (this._intellisensePositionType.HasValue && this._intellisensePositionType.Value == IntellisensePositionType.FnFindInMapValue)
            {
                if (isCaretInside(valueNameToken, this._caretPosition))
                {
                    foreach (var value in uniqueSetOfValues)
                    {
                        var intelToken = new IntellisenseToken(null, IntellisenseTokenType.Reference, string.Format(MAPPING_VALUE_DISPLAY, value), string.Format("\"{0}\"", value), null);
                        this._intellisenseToken.Add(intelToken);
                    }

                    addParameterReferences = true;
                }
            }

            if (addParameterReferences)
            {
                this.AddParameterReferencesToIntellisense();
            }
        }

        private void AddParameterReferencesToIntellisense()
        {
            List<string> references;
            if (this._referencesByType.TryGetValue(SchemaType.String, out references))
            {
                foreach (var reference in references)
                {
                    var token = new IntellisenseToken(null, IntellisenseTokenType.Reference, string.Format(PARAM_REF_DISPLAY, reference), string.Format("{{ \"Ref\" : \"{0}\" }}", reference), null);
                    this._intellisenseToken.Add(token);
                }
            }

            var pseudoParam = this._schema.GetPseudoParameter("AWS::Region");
            this._intellisenseToken.Add(new IntellisenseToken(null, IntellisenseTokenType.Reference, string.Format(PARAM_REF_DISPLAY, pseudoParam), string.Format("{{ \"Ref\" : \"{0}\" }}", pseudoParam), pseudoParam.Description));

            var findFunc = this._schema.GetIntrinsicFunction("Fn::FindInMap");
            this._intellisenseToken.Add(new IntellisenseToken(null, IntellisenseTokenType.IntrinsicFunction, findFunc.Name, findFunc.Skeleton, findFunc.Description));

            var ifFunc = this._schema.GetIntrinsicFunction("Fn::If");
            this._intellisenseToken.Add(new IntellisenseToken(null, IntellisenseTokenType.IntrinsicFunction, ifFunc.Name, ifFunc.Skeleton, ifFunc.Description));
        }



        private string BuildResourceReturnDescription(string resourceType, SchemaObject resourceObject)
        {
            if (resourceObject == null)
                return null;
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Resource of type {0} with the following attributes: [", resourceType);

            bool first = true;
            foreach (var attribute in resourceObject.ReturnValues)
            {
                if (!first)
                    sb.Append(", ");

                first = false;
                sb.Append(attribute.Name);
            }

            sb.Append("]");
            return sb.ToString();
        }

        TemplateToken ParseObject(SchemaObject currentSchemaObject)
        {

            // Move to the start element
            if (this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.StartElement)
                while (this._jsonDocument.Read() && this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.StartElement) ;

            var objectTemplateToken = new TemplateToken(TemplateTokenType.Object, currentSchemaObject, null, this._jsonDocument.KeyChainString, this._jsonDocument.CurrentToken.Position, -1, null);
            if (this._rootTemplateToken == null)
                this._rootTemplateToken = objectTemplateToken;

            var objectKeyChain = this._jsonDocument.KeyChainString;
            int startObjectPosition = this._jsonDocument.Position;
            HashSet<string> keysThisLevel = new HashSet<string>();
            try
            {
                while (this._jsonDocument.ReadToNextKey())
                {
                    TemplateToken keyTemplateToken = ParseKey(currentSchemaObject, objectTemplateToken, keysThisLevel);

                    // Move to the key separator
                    while (this._jsonDocument.Read() && this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.KeyValueSeperator) ;

                    if (keyTemplateToken.IntrinsicFunction != null)
                        ParseIntrinsicFunction(keyTemplateToken);
                    else
                        ParseValue(keyTemplateToken);
                }

                if (this._jsonDocument.CurrentToken != null && this._jsonDocument.CurrentToken.Type == JsonDocument.JsonTokenType.EndElement)
                {
                    objectTemplateToken.Length = this._jsonDocument.CurrentToken.Position - objectTemplateToken.Postion;
                }
            }
            finally
            {
                // The caret is within this object but not inside a key or value
                if (!this._caretPositionFound && startObjectPosition <= this._caretPosition && this._caretPosition <= this._jsonDocument.Position)
                {
                    this._caretPositionFound = true;
                    IntellienseTokensInsideObject(currentSchemaObject, keysThisLevel, objectKeyChain);
                }
            }

            return objectTemplateToken;
        }

        TemplateToken ParseKey(SchemaObject objectSchema, TemplateToken objectTemplateToken, HashSet<string> keysAlreadyProcessForObject)
        {
            SchemaObject keySchema = null;
            IntrinsicFunction initrinsicFunction = null;
            if (objectSchema != null)
            {
                // If we are in free form json mode then carry that schema along
                if (objectSchema.SchemaType == SchemaType.Json || objectSchema.SchemaType == SchemaType.Policy ||
                    objectSchema.ArraySchemaType == SchemaType.Json || objectSchema.ArraySchemaType == SchemaType.Policy)
                {
                    keySchema = objectSchema;
                }
                else
                {
                    keySchema = objectSchema.GetPropertySchema(this._jsonDocument.CurrentToken.Text);
                }
            }

            if(this._jsonDocument.CurrentToken.Text != null)
                initrinsicFunction = this._schema.GetIntrinsicFunction(this._jsonDocument.CurrentToken.Text);

            TemplateTokenType type;
            if (keysAlreadyProcessForObject.Contains(this._jsonDocument.CurrentToken.Text))
                type = TemplateTokenType.DuplicateKey;
            else if (objectSchema != null && objectSchema.SchemaType == SchemaType.NamedArray)
                type = TemplateTokenType.ValidKey;
            else if (keySchema != null)
                type = TemplateTokenType.ValidKey;
            else if (this._schema.GetIntrinsicFunction(this._jsonDocument.CurrentToken.Text) != null)
                type = TemplateTokenType.IntrinsicFunction;
            else if (objectSchema != null && (objectSchema.SchemaType == SchemaType.Json || objectSchema.SchemaType == SchemaType.Policy || objectSchema.ArraySchemaType == SchemaType.Json || objectSchema.ArraySchemaType == SchemaType.Policy))
                type = TemplateTokenType.ValidKey;
            else
                type = TemplateTokenType.InvalidKey;

            if (objectSchema != null && objectSchema.SchemaType == SchemaType.NamedArray)
            {
                if (!string.IsNullOrWhiteSpace(objectSchema.SchemaLookupProperty))
                {
                    var schemaLookupName = this._jsonDocument.PeekChildAttribueValue(objectSchema.SchemaLookupProperty);
                    keySchema = objectSchema.GetChildSchema(schemaLookupName);
                }
                else
                {
                    keySchema = objectSchema.GetDefaultChildSchemaObject();
                }
            }

            TemplateToken keyTemplateToken = new TemplateToken(type, keySchema, initrinsicFunction, this._jsonDocument.KeyChainString, (int)this._jsonDocument.CurrentToken.Position, this._jsonDocument.CurrentToken.Length, this._jsonDocument.CurrentToken.Text);
            this._highlightedTemplateTokens.Add(keyTemplateToken);
            objectTemplateToken.AddChildTemplateToken(keyTemplateToken);

            // Caret is inside the key so compute valid intellisense for keys of this object.
            if (!this._caretPositionFound && keyTemplateToken.Postion <= this._caretPosition && this._caretPosition <= this._jsonDocument.Position)
            {
                this._caretPositionFound = true;
                IntellisenseTokensInsideKey(objectSchema, keysAlreadyProcessForObject, false);
                this._intellisenseStartingPostion = this._jsonDocument.CurrentToken.Position;
                this._intellisenseEndingPostion = this._jsonDocument.CurrentToken.Position + this._jsonDocument.CurrentToken.Length;
            }

            if (this._jsonDocument.KeyChain.Count > 2 && string.Equals(this._jsonDocument.KeyChain[2], "Conditions"))
            {
                this._allDefinedConditions.Add(this._jsonDocument.KeyChain[1]);
            }

            keysAlreadyProcessForObject.Add(this._jsonDocument.CurrentToken.Text);
            return keyTemplateToken;
        }

        void ParseValue(TemplateToken keyTemplateToken)
        {
            SchemaObject keySchema = keyTemplateToken.Schema;
            // Move to either 
            while (this._jsonDocument.Read() &&
                this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.Text &&
                this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.Number &&
                this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.Boolean &&
                this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.Null &&
                this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.StartElement &&
                this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.StartArray) ;

            TemplateToken valueTemplateToken = null;
            if (this._jsonDocument.CurrentToken.Type == JsonDocument.JsonTokenType.StartElement)
            {
                valueTemplateToken = ParseObject(keySchema);
                keyTemplateToken.AddChildTemplateToken(valueTemplateToken);
            }
            if (this._jsonDocument.CurrentToken.Type == JsonDocument.JsonTokenType.StartArray)
            {
                valueTemplateToken = ParseArray(keySchema);
                keyTemplateToken.AddChildTemplateToken(valueTemplateToken);
            }
            else if (this._jsonDocument.IsCurrentScalerValue)
            {
                valueTemplateToken = null;
                if (keySchema != null && !keySchema.IsAllowedValue(this._jsonDocument.CurrentToken.Text))
                    valueTemplateToken = new TemplateToken(TemplateTokenType.NotAllowedValue, keySchema, null, this._jsonDocument.KeyChainString, this._jsonDocument.CurrentToken.Position, this._jsonDocument.CurrentToken.Length, this._jsonDocument.CurrentToken.Text);
                else
                    valueTemplateToken = new TemplateToken(TemplateTokenType.ScalerValue, keySchema, null, this._jsonDocument.KeyChainString, this._jsonDocument.CurrentToken.Position, this._jsonDocument.CurrentToken.Length, this._jsonDocument.CurrentToken.Text);

                if (valueTemplateToken != null)
                {
                    this._highlightedTemplateTokens.Add(valueTemplateToken);
                    keyTemplateToken.AddChildTemplateToken(valueTemplateToken);

                    if (keySchema != null && keySchema.SchemaType == SchemaType.Resource)
                        this._resourceTokensToPostValidate.Add(valueTemplateToken);
                    // Caret is inside the value token.  Compute valid intellisense for this value.
                    if (!this._caretPositionFound && valueTemplateToken.Postion <= this._caretPosition && this._caretPosition <= this._jsonDocument.Position)
                    {
                        this._caretPositionFound = true;
                        if (keyTemplateToken != null)
                        {
                            IntellisenseTokensInsideValue(keyTemplateToken.Schema);
                            this._intellisenseStartingPostion = this._jsonDocument.CurrentToken.Position;
                            this._intellisenseEndingPostion = this._jsonDocument.CurrentToken.Position + this._jsonDocument.CurrentToken.Length;
                        }
                    }


                    // Check to see this object can be used as a references.
                    if (keyTemplateToken != null && string.Equals(keyTemplateToken.Value, "Type"))
                    {
                        if (this._jsonDocument.KeyChain.Count == 3)
                        {
                            if ((string.Equals(this._jsonDocument.KeyChain[2], "Parameters") || string.Equals(this._jsonDocument.KeyChain[2], "Resources")) && !string.IsNullOrWhiteSpace(this._jsonDocument.KeyChain[1]))
                            {
                                List<string> refs;
                                if (!this._referencesByType.TryGetValue(valueTemplateToken.Value, out refs))
                                {
                                    refs = new List<string>();
                                    this._referencesByType[valueTemplateToken.Value] = refs;
                                }

                                refs.Add(this._jsonDocument.KeyChain[1]);
                            }
                        }
                    }

                }
            }

            if (valueTemplateToken != null && keyTemplateToken.Schema != null && keyTemplateToken.Schema.SchemaType == SchemaType.ConditionDeclaration)
            {
                this._conditionsToPostValidate.Add(valueTemplateToken);
            }
        }

        TemplateToken ParseArray(SchemaObject currentSchemaObject)
        {
            int arrayStart = this._jsonDocument.Position;
            var arrayTemplateToken = new TemplateToken(TemplateTokenType.Array, currentSchemaObject, null, this._jsonDocument.KeyChainString, this._jsonDocument.Position, -1, null);
            while (this._jsonDocument.Read() && this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.EndArray)
            {
                TemplateToken childToken = null;
                if (this._jsonDocument.IsCurrentScalerValue)
                {
                    childToken = new TemplateToken(TemplateTokenType.ScalerValue, currentSchemaObject, null, this._jsonDocument.KeyChainString, (int)this._jsonDocument.CurrentToken.Position, this._jsonDocument.CurrentToken.Length, this._jsonDocument.CurrentToken.Text);
                    this._highlightedTemplateTokens.Add(childToken);

                    if (isCaretInside(childToken, this._caretPosition))
                    {
                        this._caretPositionFound = true;
                        IntellisenseTokensInsideValue(currentSchemaObject);
                        this._intellisensePositionType = IntellisensePositionType.Value;
                        this._intellisenseStartingPostion = childToken.Postion;
                        this._intellisenseEndingPostion = childToken.Postion + childToken.Length;
                    }
                }
                else if (this._jsonDocument.CurrentToken.Type == JsonDocument.JsonTokenType.StartElement)
                {
                    childToken = ParseObject(currentSchemaObject);
                }
                else if (this._jsonDocument.CurrentToken.Type == JsonDocument.JsonTokenType.StartArray)
                {
                    childToken = ParseArray(null);
                }

                if (childToken != null)
                {
                    arrayTemplateToken.AddChildTemplateToken(childToken);
                }
            }

            // Caret was inside the array but not inside an object so no intellisense can be displayed
            if (!this._caretPositionFound && arrayStart <= this._caretPosition && this._caretPosition <= this._jsonDocument.Position)
            {
                this._caretPositionFound = true;
                this._intellisensePositionType = IntellisensePositionType.Value;
                this._intellisenseKeyChain = this._jsonDocument.KeyChain.ToArray();


                if (currentSchemaObject != null)
                {
                    this._intellisenseSchemaType = currentSchemaObject.ArraySchemaType;
                    this._intellisenseResourceRefType = currentSchemaObject.ResourceRefType;

                    if (currentSchemaObject.ArraySchemaType == SchemaType.String)
                        this.AddAllIntrinsicFunctionsToIntellisense();

                    AddAllowedValuesToIntellisense(currentSchemaObject);
                }
                else
                {
                    this.AddAllIntrinsicFunctionsToIntellisense();
                }
            }

            return arrayTemplateToken;
        }

        void AddAllowedValuesToIntellisense(SchemaObject schema)
        {
            if (schema.AllowedValuesCount > 0)
            {
                foreach (var value in schema.AllowedValues)
                {
                    if (string.Equals(value.Value, WILD_CARD, StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    string codeSnippet;
                    if (schema.ArraySchemaType == SchemaType.Number)
                        codeSnippet = string.Format("{0}", value.Value);
                    else
                        codeSnippet = string.Format("\"{0}\"", value.Value);
                    this._intellisenseToken.Add(new IntellisenseToken(null, IntellisenseTokenType.AllowedValue, value.DisplayLabel, codeSnippet, null));
                }
            }
        }

        void ParseIntrinsicFunction(TemplateToken keyTemplateToken)
        {
            switch (keyTemplateToken.IntrinsicFunction.Name)
            {
                case "Ref":
                    ParseRefFunction(keyTemplateToken);
                    break;
                case "Fn::GetAtt":
                    ParseFnGetAttFunction(keyTemplateToken);
                    break;
                case "Fn::FindInMap":
                    ParseFnFindInMapFunction(keyTemplateToken);
                    break;
                case "Fn::If":
                    ParseFnIfFunction(keyTemplateToken);
                    break;
                // Not going to do anything special for functions that fall here and just treat them as json strings.
                default:
                    ParseValue(keyTemplateToken);
                    break;

            }
        }

        void ParseRefFunction(TemplateToken keyTemplateToken)
        {
            while (this._jsonDocument.Read() && this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.Text) ;

            var refTemplateToken = new TemplateToken(TemplateTokenType.ScalerValue, keyTemplateToken.Schema, null, this._jsonDocument.KeyChainString, this._jsonDocument.CurrentToken.Position, this._jsonDocument.CurrentToken.Length, this._jsonDocument.CurrentToken.Text);
            keyTemplateToken.AddChildTemplateToken(refTemplateToken);
            this._highlightedTemplateTokens.Add(refTemplateToken);
            this._refTokensToPostValidate.Add(refTemplateToken);

            CheckCaretInsideToken(refTemplateToken, IntellisensePositionType.RefValue);
        }

        void ParseFnIfFunction(TemplateToken keyTemplateToken)
        {
            while (this._jsonDocument.Read() && this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.StartArray) ;

            int arrayStart = this._jsonDocument.Position;
            TemplateToken conditionTemplateToken = null;
            TemplateToken trueTemplateToken = null;
            TemplateToken falseTemplateToken = null;

            try
            {
                while (this._jsonDocument.Read() && this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.Text && this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.EndArray) ;

                if (this._jsonDocument.CurrentToken.Type == JsonDocument.JsonTokenType.EndArray)
                {
                    conditionTemplateToken = new TemplateToken(TemplateTokenType.ScalerValue, keyTemplateToken.Schema, null, this._jsonDocument.KeyChainString, arrayStart, this._jsonDocument.Position - arrayStart - 1, null);
                    keyTemplateToken.AddChildTemplateToken(conditionTemplateToken);
                    CheckCaretInsideToken(conditionTemplateToken, IntellisensePositionType.FnIfCondition);
                    return;
                }

                conditionTemplateToken = new TemplateToken(TemplateTokenType.ScalerValue, keyTemplateToken.Schema, null, this._jsonDocument.KeyChainString, this._jsonDocument.CurrentToken.Position, this._jsonDocument.CurrentToken.Length, this._jsonDocument.CurrentToken.Text);
                keyTemplateToken.AddChildTemplateToken(conditionTemplateToken);
                this._highlightedTemplateTokens.Add(conditionTemplateToken);
                CheckCaretInsideToken(conditionTemplateToken, IntellisensePositionType.FnIfCondition);

                while (this._jsonDocument.Read() && 
                    this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.Text && 
                    this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.StartElement &&
                    this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.StartArray && 
                    this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.EndArray) ;

                if (this._jsonDocument.CurrentToken.Type == JsonDocument.JsonTokenType.EndArray)
                {
                    return;
                }

                if (this._jsonDocument.CurrentToken.Type == JsonDocument.JsonTokenType.Text)
                {
                    trueTemplateToken = new TemplateToken(TemplateTokenType.ScalerValue, keyTemplateToken.Schema, null, this._jsonDocument.KeyChainString, this._jsonDocument.CurrentToken.Position, this._jsonDocument.CurrentToken.Length, this._jsonDocument.CurrentToken.Text);
                }
                else if (this._jsonDocument.CurrentToken.Type == JsonDocument.JsonTokenType.StartElement)
                {
                    trueTemplateToken = ParseObject(keyTemplateToken.Schema ?? this._schema.DefaultJSONSchema);
                }
                else if (this._jsonDocument.CurrentToken.Type == JsonDocument.JsonTokenType.StartArray)
                {
                    trueTemplateToken = ParseArray(keyTemplateToken.Schema ?? this._schema.DefaultJSONSchema);
                }

                if (trueTemplateToken != null)
                {
                    keyTemplateToken.AddChildTemplateToken(trueTemplateToken);
                    this._highlightedTemplateTokens.Add(trueTemplateToken);
                    CheckCaretInsideToken(trueTemplateToken, IntellisensePositionType.FnIfTrue);
                }

                while (this._jsonDocument.Read() &&
                    this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.Text &&
                    this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.StartElement &&
                    this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.StartArray &&
                    this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.EndArray) ;

                if (this._jsonDocument.CurrentToken.Type == JsonDocument.JsonTokenType.EndArray)
                {
                    return;
                }

                if (this._jsonDocument.CurrentToken.Type == JsonDocument.JsonTokenType.Text)
                {
                    falseTemplateToken = new TemplateToken(TemplateTokenType.ScalerValue, keyTemplateToken.Schema, null, this._jsonDocument.KeyChainString, this._jsonDocument.CurrentToken.Position, this._jsonDocument.CurrentToken.Length, this._jsonDocument.CurrentToken.Text);
                }
                else if (this._jsonDocument.CurrentToken.Type == JsonDocument.JsonTokenType.StartElement)
                {
                    falseTemplateToken = ParseObject(keyTemplateToken.Schema ?? this._schema.DefaultJSONSchema);
                }
                else if (this._jsonDocument.CurrentToken.Type == JsonDocument.JsonTokenType.StartArray)
                {
                    falseTemplateToken = ParseArray(keyTemplateToken.Schema ?? this._schema.DefaultJSONSchema);
                }

                if (falseTemplateToken != null)
                {
                    keyTemplateToken.AddChildTemplateToken(falseTemplateToken);
                    this._highlightedTemplateTokens.Add(falseTemplateToken);
                    CheckCaretInsideToken(falseTemplateToken, IntellisensePositionType.FnIfFalse);
                }

                while (this._jsonDocument.Read() && this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.EndArray) ;
            }
            finally
            {
                if (conditionTemplateToken != null || trueTemplateToken != null || falseTemplateToken != null)
                {
                    var tuple = new Tuple<TemplateToken, TemplateToken, TemplateToken>(conditionTemplateToken, trueTemplateToken, falseTemplateToken);
                    this._fnIfTokensToPostValidate.Add(tuple);
                }

                if (conditionTemplateToken == null)
                    keyTemplateToken.Type = TemplateTokenType.UnknownConditionType;
                else if (trueTemplateToken == null)
                    keyTemplateToken.Type = TemplateTokenType.UnknownConditionTrue;
                else if (falseTemplateToken == null)
                    keyTemplateToken.Type = TemplateTokenType.UnknownConditionFalse;
            }
        }

        void ParseFnGetAttFunction(TemplateToken keyTemplateToken)
        {
            while (this._jsonDocument.Read() && this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.StartArray) ;

            int arrayStart = this._jsonDocument.Position;
            TemplateToken resourceTemplateToken = null;
            TemplateToken attribteTemplateToken = null;
            try
            {
                while (this._jsonDocument.Read() && this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.Text && this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.EndArray) ;

                if (this._jsonDocument.CurrentToken.Type == JsonDocument.JsonTokenType.EndArray)
                {
                    resourceTemplateToken = new TemplateToken(TemplateTokenType.ScalerValue, keyTemplateToken.Schema, null, this._jsonDocument.KeyChainString, arrayStart, this._jsonDocument.Position - arrayStart - 1, null);
                    keyTemplateToken.AddChildTemplateToken(resourceTemplateToken);
                    CheckCaretInsideToken(resourceTemplateToken, IntellisensePositionType.FnGetAttResource);
                    return;
                }

                resourceTemplateToken = new TemplateToken(TemplateTokenType.ScalerValue, keyTemplateToken.Schema, null, this._jsonDocument.KeyChainString, this._jsonDocument.CurrentToken.Position, this._jsonDocument.CurrentToken.Length, this._jsonDocument.CurrentToken.Text);
                keyTemplateToken.AddChildTemplateToken(resourceTemplateToken);
                this._highlightedTemplateTokens.Add(resourceTemplateToken);

                CheckCaretInsideToken(resourceTemplateToken, IntellisensePositionType.FnGetAttResource);

                while (this._jsonDocument.Read() && this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.Text && this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.EndArray) ;

                if (this._jsonDocument.CurrentToken.Type == JsonDocument.JsonTokenType.EndArray)
                {
                    return;
                }

                attribteTemplateToken = new TemplateToken(TemplateTokenType.ScalerValue, keyTemplateToken.Schema, null, this._jsonDocument.KeyChainString, this._jsonDocument.CurrentToken.Position, this._jsonDocument.CurrentToken.Length, this._jsonDocument.CurrentToken.Text);
                keyTemplateToken.AddChildTemplateToken(attribteTemplateToken);
                this._highlightedTemplateTokens.Add(attribteTemplateToken);

                CheckCaretInsideToken(attribteTemplateToken, IntellisensePositionType.FnGetAttAttribute);

                while (this._jsonDocument.Read() && this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.EndArray) ;
            }
            finally
            {
                if (resourceTemplateToken != null || attribteTemplateToken != null)
                {
                    var tuple = new Tuple<TemplateToken, TemplateToken>(resourceTemplateToken, attribteTemplateToken);
                    this._fnGetAttTokensToPostValidate.Add(tuple);
                }
            }
        }

        void ParseFnFindInMapFunction(TemplateToken keyTemplateToken)
        {
            while (this._jsonDocument.Read() && this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.StartArray) ;

            int arrayStart = this._jsonDocument.Position;
            TemplateToken mapNameTemplateToken = null;
            TemplateToken mapKeyTemplateToken = null;
            TemplateToken valueTemplateToken = null;

            try
            {
                while (this._jsonDocument.Read() && this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.Text && this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.StartElement && this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.EndArray) ;

                if (this._jsonDocument.CurrentToken.Type == JsonDocument.JsonTokenType.EndArray)
                {
                    mapNameTemplateToken = new TemplateToken(TemplateTokenType.ScalerValue, keyTemplateToken.Schema, null, this._jsonDocument.KeyChainString, arrayStart, this._jsonDocument.Position - arrayStart - 1, null);
                    keyTemplateToken.AddChildTemplateToken(mapNameTemplateToken);
                    CheckCaretInsideToken(mapNameTemplateToken, IntellisensePositionType.FnFindInMapMapName);
                    return;
                }

                if (this._jsonDocument.CurrentToken.Type == JsonDocument.JsonTokenType.StartElement)
                {
                    mapNameTemplateToken = ParseObject(null);
                }
                else
                {
                    mapNameTemplateToken = new TemplateToken(TemplateTokenType.ScalerValue, keyTemplateToken.Schema, null, this._jsonDocument.KeyChainString, this._jsonDocument.CurrentToken.Position, this._jsonDocument.CurrentToken.Length, this._jsonDocument.CurrentToken.Text);
                }

                keyTemplateToken.AddChildTemplateToken(mapNameTemplateToken);
                this._highlightedTemplateTokens.Add(mapNameTemplateToken);

                CheckCaretInsideToken(mapNameTemplateToken, IntellisensePositionType.FnFindInMapMapName);

                while (this._jsonDocument.Read() && this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.Text && this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.StartElement && this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.EndArray) ;

                if (this._jsonDocument.CurrentToken.Type == JsonDocument.JsonTokenType.EndArray)
                {
                    return;
                }

                if (this._jsonDocument.CurrentToken.Type == JsonDocument.JsonTokenType.StartElement)
                {
                    mapKeyTemplateToken = ParseObject(null);
                }
                else
                {
                    mapKeyTemplateToken = new TemplateToken(TemplateTokenType.ScalerValue, keyTemplateToken.Schema, null, this._jsonDocument.KeyChainString, this._jsonDocument.CurrentToken.Position, this._jsonDocument.CurrentToken.Length, this._jsonDocument.CurrentToken.Text);
                }
                
                keyTemplateToken.AddChildTemplateToken(mapKeyTemplateToken);
                this._highlightedTemplateTokens.Add(mapKeyTemplateToken);

                CheckCaretInsideToken(mapKeyTemplateToken, IntellisensePositionType.FnFindInMapKey);

                while (this._jsonDocument.Read() && this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.Text && this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.StartElement && this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.EndArray) ;

                if (this._jsonDocument.CurrentToken.Type == JsonDocument.JsonTokenType.EndArray)
                {
                    return;
                }

                if (this._jsonDocument.CurrentToken.Type == JsonDocument.JsonTokenType.StartElement)
                {
                    valueTemplateToken = ParseObject(null);
                }
                else
                {
                    valueTemplateToken = new TemplateToken(TemplateTokenType.ScalerValue, keyTemplateToken.Schema, null, this._jsonDocument.KeyChainString, this._jsonDocument.CurrentToken.Position, this._jsonDocument.CurrentToken.Length, this._jsonDocument.CurrentToken.Text);
                }

                keyTemplateToken.AddChildTemplateToken(valueTemplateToken);
                this._highlightedTemplateTokens.Add(valueTemplateToken);

                CheckCaretInsideToken(valueTemplateToken, IntellisensePositionType.FnFindInMapValue);

                while (this._jsonDocument.Read() && this._jsonDocument.CurrentToken.Type != JsonDocument.JsonTokenType.EndArray) ;
            }
            finally
            {
                if (mapNameTemplateToken != null || mapKeyTemplateToken != null || valueTemplateToken != null)
                {
                    var tuple = new Tuple<TemplateToken, TemplateToken, TemplateToken>(mapNameTemplateToken, mapKeyTemplateToken, valueTemplateToken);
                    this._fnFindInMapTokensToPostValidate.Add(tuple);
                }
            }
        }


        void CheckCaretInsideToken(TemplateToken templateToken, IntellisensePositionType positionType)
        {
            if (!this._caretPositionFound && templateToken.Postion <= this._caretPosition && this._caretPosition <= this._jsonDocument.Position)
            {
                this._caretPositionFound = true;
                this._intellisensePositionType = positionType;
                this._intellisenseStartingPostion = templateToken.Postion;
                this._intellisenseEndingPostion = templateToken.Postion + templateToken.Length;
            }
        }

        void IntellisenseTokensInsideValue(SchemaObject keySchema)
        {
            if (keySchema == null)
                return;

            this._disableReferencesForIntellisense = keySchema.DisableReferences;

            if (keySchema != null && keySchema.SchemaType == SchemaType.ConditionDeclaration)
                this._intellisensePositionType = IntellisensePositionType.Condition;
            else
                this._intellisensePositionType = IntellisensePositionType.Value;
            if (keySchema != null)
            {
                this._intellisenseSchemaType = keySchema.SchemaType;
                this._intellisenseResourceRefType = keySchema.ResourceRefType;
                this._intellisenseKeyChain = this._jsonDocument.KeyChain.ToArray();

                if (keySchema.AllowedValuesCount > 0)
                {
                    AddAllowedValuesToIntellisense(keySchema);
                }
                else if (keySchema.SchemaType == SchemaType.Boolean)
                {
                    this._intellisenseToken.Add(new IntellisenseToken(null, IntellisenseTokenType.AllowedValue, "false", "false", null));
                    this._intellisenseToken.Add(new IntellisenseToken(null, IntellisenseTokenType.AllowedValue, "true", "true", null));
                }
                
                if (!keySchema.DisableFunctions && keySchema.SchemaType == SchemaType.String && !this._jsonDocument.KeyChainString.StartsWith("/Parameters/") && !this._jsonDocument.KeyChainString.StartsWith("/Mappings/"))
                {
                    AddAllIntrinsicFunctionsToIntellisense();
                }
            }
        }

        void AddAllIntrinsicFunctionsToIntellisense()
        {
            foreach (var func in this._schema.IntrinsicFunctions)
            {
                if (func.Name.Equals("Ref") || func.Name.Equals("Fn::Base64"))
                    continue;

                this._intellisenseToken.Add(new IntellisenseToken(null, IntellisenseTokenType.IntrinsicFunction, func.Name, func.Skeleton, func.Description));
            }
        }

        void IntellisenseTokensInsideKey(SchemaObject objectSchema, HashSet<string> alreadyUsedKeys, bool includeValueSkeleton)
        {
            if (objectSchema == null)
                return;

            this._intellisensePositionType = IntellisensePositionType.Value;
            if (objectSchema != null)
            {
                this._intellisensePositionType = IntellisensePositionType.Key;
                this._intellisenseKeyChain = this._jsonDocument.KeyChain.ToArray();

                foreach (var property in objectSchema.PropertySchemaNames)
                {
                    if (alreadyUsedKeys.Contains(property))
                        continue;

                    var objectKeySchema = objectSchema.GetPropertySchema(property);
                    string codeSnippet = string.Format("\"{0}\"", property);
                    if (includeValueSkeleton)
                    {
                        if (objectKeySchema.SchemaType == SchemaType.String)
                            codeSnippet += " : \"\"";
                        else if (objectKeySchema.SchemaType == SchemaType.Object)
                            codeSnippet += " : {}";
                        else if (objectKeySchema.SchemaType == SchemaType.Array)
                            codeSnippet += " : []";
                        else if (objectKeySchema.SchemaType == SchemaType.NamedArray)
                            codeSnippet += " : {}";
                        else if (objectKeySchema.SchemaType == SchemaType.Json || objectKeySchema.SchemaType == SchemaType.Policy)
                            codeSnippet += " : {}";
                        else if (objectKeySchema.SchemaType == SchemaType.Resource)
                            codeSnippet += " : \"\"";
                        else if (objectKeySchema.SchemaType == SchemaType.ConditionDefinition)
                            codeSnippet += " : {\"\" : []}";
                        else if (objectKeySchema.SchemaType == SchemaType.ConditionDeclaration)
                            codeSnippet += " : \"\"";
                        else
                            codeSnippet += " : ";
                    }

                    this._intellisenseToken.Add(new IntellisenseToken(objectKeySchema, IntellisenseTokenType.ObjectKey, property, codeSnippet, objectKeySchema.Description));
                }
            }
        }

        void IntellisenseTokensInsideConditionsContainer(SchemaObject objectSchema)
        {
            this._intellisenseToken.Add(new IntellisenseToken(objectSchema, IntellisenseTokenType.NamedArrayElement, "New Condition", "\"\" : {\"\" : []}", null));
        }

        void IntellisenseTokensInsideNamedArray(SchemaObject objectSchema)
        {
            if (objectSchema == null)
                return;

            Action<JsonWriter, SchemaObject, bool> recursiveCall = null;
            recursiveCall = (writer, s, firstLevel) =>
            {
                foreach (var property in s.PropertySchemaNames.OrderBy(x => x))
                {
                    var propertySchema = s.GetPropertySchema(property);
                    if ((firstLevel && property == objectSchema.SchemaLookupProperty) || !propertySchema.Required)
                        continue;

                    writer.WritePropertyName(property);
                    if (propertySchema.SchemaType == SchemaType.Json || propertySchema.SchemaType == SchemaType.Policy)
                    {
                        writer.WriteObjectStart();
                        writer.WriteObjectEnd();
                    }
                    else if (propertySchema.SchemaType == SchemaType.Array)
                    {
                        writer.WriteArrayStart();
                        writer.WriteArrayEnd();
                    }
                    else if (propertySchema.SchemaType == SchemaType.Reference)
                    {
                        writer.WriteObjectStart();
                        writer.WritePropertyName("Ref");
                        writer.Write("");
                        writer.WriteObjectEnd();
                    }
                    else if (propertySchema.SchemaType == SchemaType.Object)
                    {
                        writer.WriteObjectStart();
                        recursiveCall(writer, propertySchema, false);
                        writer.WriteObjectEnd();
                    }
                    else
                    {
                        writer.Write("");
                    }
                }
            };

            // Figure out how far the caret is currently indented and indent all the lines in this json document.
            string indentReplacementString = null;
            int previousNewLine = this._jsonDocument.OriginalDocument.LastIndexOf('\n', this._caretPosition);
            if (previousNewLine != -1)
                indentReplacementString = this._jsonDocument.OriginalDocument.Substring(previousNewLine, this._caretPosition - previousNewLine);

            foreach (var childSchemaName in objectSchema.ChildSchemaNames)
            {
                var childSchema = objectSchema.GetChildSchema(childSchemaName);
                string jsonObject;

                lock (BASIC_RESOURCES_TEMPLATES)
                {
                    // If not cached then generate a skeleton version of this object with all of it's required fields
                    if (!BASIC_RESOURCES_TEMPLATES.TryGetValue(childSchemaName, out jsonObject))
                    {
                        JsonWriter writer = new JsonWriter();
                        writer.PrettyPrint = true;
                        writer.WriteObjectStart();

                        writer.WritePropertyName(objectSchema.SchemaLookupProperty);
                        writer.Write(childSchemaName);


                        recursiveCall(writer, childSchema, true);

                        writer.WriteObjectEnd();

                        jsonObject = writer.ToString().Trim();
                        BASIC_RESOURCES_TEMPLATES[childSchemaName] = jsonObject;
                    }
                }

                if (indentReplacementString != null)
                    jsonObject = jsonObject.Replace("\n", indentReplacementString);

                string codeSnippet = "\"\" : " + jsonObject;

                // Convert display name from AWS::<Service>::<Resource> to <Service> <Resource> to make it 
                // easier to navigate the list of resources in the intellisense drop down box
                string displayName = childSchemaName;
                if (displayName.StartsWith("AWS::"))
                {
                    displayName = displayName.Substring(5).Replace("::", " ");
                }

                this._intellisenseToken.Add(new IntellisenseToken(childSchema, IntellisenseTokenType.NamedArrayElement, displayName, codeSnippet, childSchema.Description));
            }

            // If this is a named array that can only have one type like parameters and outputs then add that schema to the intellisense
            if (objectSchema.GetDefaultChildSchemaObject() != null)
            {
                var defaultSchema = objectSchema.GetDefaultChildSchemaObject();
                JsonWriter writer = new JsonWriter();
                writer.PrettyPrint = true;
                writer.WriteObjectStart();
                recursiveCall(writer, objectSchema.GetDefaultChildSchemaObject(), true);
                writer.WriteObjectEnd();
                string jsonObject = writer.ToString().Trim();

                if (indentReplacementString != null)
                    jsonObject = jsonObject.Replace("\n", indentReplacementString);

                string codeSnippet = "\"\" : " + jsonObject;
                this._intellisenseToken.Add(new IntellisenseToken(defaultSchema, IntellisenseTokenType.NamedArrayElement, "New Object", codeSnippet, null));
            }
        }

        void IntellienseTokensInsideObject(SchemaObject schemaObject, HashSet<string> alreadyUsedKeys, string objectKeyChain)
        {
            if(schemaObject == null)
                return;

            if (schemaObject.SchemaType == SchemaType.Object || 
                schemaObject.ArraySchemaType == SchemaType.Object || 
                schemaObject.SchemaType == SchemaType.Json || 
                schemaObject.SchemaType == SchemaType.Policy ||
                schemaObject.SchemaType == SchemaType.ConditionDefinition)
            {
                char? prevChar;
                int? foundPosition;
                this._jsonDocument.GetPreviousNonWhiteChar(this._caretPosition, out prevChar, out foundPosition);
                if (!prevChar.HasValue)
                    return;

                // Check to see if we are just after start of an object or a property
                if (prevChar.Value == ',' || prevChar.Value == '{')
                {
                    IntellisenseTokensInsideKey(schemaObject, alreadyUsedKeys, true);
                }
                else if (prevChar == ':')
                {
                    string keyName = this._jsonDocument.GetPreviousQuotedString(foundPosition.Value);
                    if (keyName == null)
                        return;

                    var keySchema = schemaObject.GetPropertySchema(keyName);
                    if (keySchema == null)
                    {
                        if ((schemaObject.SchemaType == SchemaType.Json || schemaObject.SchemaType == SchemaType.Policy) && !objectKeyChain.StartsWith("/Parameters/") && !objectKeyChain.StartsWith("/Mappings/"))
                        {
                            AddAllIntrinsicFunctionsToIntellisense();
                            this._intellisensePositionType = IntellisensePositionType.Value;
                            this._intellisenseSchemaType = schemaObject.SchemaType;
                            this._intellisenseKeyChain = this._jsonDocument.KeyChain.ToArray();
                            return;
                        }

                        return;
                    }

                    IntellisenseTokensInsideValue(keySchema);
                }
            }
            else if (schemaObject.SchemaType == SchemaType.NamedArray)
            {
                char? prevChar;
                int? foundPosition;
                this._jsonDocument.GetPreviousNonWhiteChar(this._caretPosition, out prevChar, out foundPosition);
                if (!prevChar.HasValue)
                    return;

                if (prevChar.Value == ',' || prevChar.Value == '{')
                {
                    if (string.Equals(objectKeyChain, "/Conditions", StringComparison.Ordinal))
                    {
                        IntellisenseTokensInsideConditionsContainer(schemaObject);
                    }
                    else
                    {
                        IntellisenseTokensInsideNamedArray(schemaObject);
                    }
                }
            }
        }

        private bool IsRefType(string x)
        {
            if (x.StartsWith("AWS::") && !_schema.AWSCustomParameterTypes.Contains(x))
                return true;

            return false;
        }
    }
}
