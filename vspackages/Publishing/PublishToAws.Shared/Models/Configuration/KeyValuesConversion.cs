using System.Collections.Generic;
using System.Linq;

using Amazon.AWSToolkit.Models;

using Newtonsoft.Json;

namespace Amazon.AWSToolkit.Publish.Models.Configuration
{
    public static class KeyValuesConversion
    {
        public static ICollection<KeyValue> FromJson(string json)
        {
            var dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(json ?? string.Empty) ??
                             new Dictionary<string, string>();

            return dictionary
                .Select(keyValue => new KeyValue() { Key = keyValue.Key, Value = keyValue.Value })
                .ToList();
        }

        public static string ToJson(IEnumerable<KeyValue> keyValues)
        {
            return JsonConvert.SerializeObject(keyValues
                .ToDictionary(keyValue => keyValue.Key, keyValue => keyValue.Value));
        }
    }
}
