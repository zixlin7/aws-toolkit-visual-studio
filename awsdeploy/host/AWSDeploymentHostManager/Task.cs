using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ThirdParty.Json.LitJson;

namespace AWSDeploymentHostManager
{
    public abstract class Task
    {
        protected const string
            JSON_KEY_OP        = "operation",
            JSON_KEY_RESPONSE  = "response";

        protected const string
            TASK_RESPONSE_OK      = "ok",
            TASK_RESPONSE_DEFER   = "deferred",
            TASK_RESPONSE_UNKNOWN = "unknown",
            TASK_RESPONSE_FAIL    = "failed";  //JSON, I am disappoint

        public const string
            JSON_TIMESTAMP_FORMAT = "yyyy-MM-ddTHH:mm:ssZ",
            JSON_KEY_TIMESTAMP    = "timestamp";

        protected IDictionary<string, string> parameters = new Dictionary<string, string>();

        public void SetParameter(string key, string value)
        {
            parameters.Add(key, value);
        }

        public abstract string Operation { get; }
        public abstract string Execute();

        // The response should have operation: and response: keys, but other values can be merged in
        // as well. Tasks can call this with a string or a JsonData
        protected string GenerateResponse(object response)
        {
            JsonData responseJson;

            if (response is string)
            {
                responseJson = new JsonData();

                responseJson[JSON_KEY_OP] = this.Operation;
                responseJson[JSON_KEY_RESPONSE] = (string)response;
            }
            else if (response is JsonData)
            {
                responseJson = (JsonData)response;

                if (null == responseJson[JSON_KEY_OP])
                {
                    responseJson[JSON_KEY_OP] = this.Operation;
                }

                if (null == responseJson[JSON_KEY_RESPONSE])
                {
                    // assume the response is okay if the task didn't explicitly say otherwise
                    responseJson[JSON_KEY_RESPONSE] = TASK_RESPONSE_OK;
                }
            }
            else
            {
                responseJson = new JsonData();

                responseJson[JSON_KEY_OP] = this.Operation;
                responseJson[JSON_KEY_RESPONSE] = TASK_RESPONSE_FAIL;
            }

            return JsonMapper.ToJson(responseJson);
        }

    }
}
