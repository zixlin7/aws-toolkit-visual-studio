using System.Net;

namespace Amazon.AWSToolkit.Util
{
    public static class HttpStatusCodeExtensionMethods
    {
        public static bool Is4xx(this HttpStatusCode statusCode)
        {
            var code = (int) statusCode;
            return code >= 400 && code <= 499;
        }

        public static bool Is5xx(this HttpStatusCode statusCode)
        {
            var code = (int)statusCode;
            return code >= 500 && code <= 599;
        }
    }
}