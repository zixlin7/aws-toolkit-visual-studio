using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ThirdParty.Json.LitJson;

namespace Amazon.AWSToolkit.CloudFormation.Parser
{
    public class ParserUtil
    {
        public static string PrettyFormat(string jsonDocument)
        {
            object obj = null;

            try
            {
                obj = JsonMapper.ToObject(jsonDocument);
            }
            catch (Exception e)
            {
                throw new Exception("Error parsing template due to json syntax error: " + e.Message);
            }

            try
            {
                var sb = new StringBuilder();
                var writer = new JsonWriter(sb) { PrettyPrint = true };
                JsonMapper.ToJson(obj, writer);
                var formattedDocument = sb.ToString();

                return formattedDocument;
            }
            catch (Exception e)
            {
                throw new Exception("Error formatting template: " + e.Message);
            }
        }
    }
}
