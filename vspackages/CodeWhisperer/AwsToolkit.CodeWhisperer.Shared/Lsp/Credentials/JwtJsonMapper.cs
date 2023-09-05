using Jose;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials
{
    /// <summary>
    /// Defines JSON serialization behavior that is used with JWT encoding.
    /// We want payload property names to use camelCase by default, rather than .NET's PascalCase.
    /// Otherwise recipients (language servers) will not locate the fields they are looking for.
    /// </summary>
    internal class JwtJsonMapper : IJsonMapper
    {
        private readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
        };

        public string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj, _jsonSerializerSettings);
        }

        public T Parse<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, _jsonSerializerSettings);
        }
    }
}
