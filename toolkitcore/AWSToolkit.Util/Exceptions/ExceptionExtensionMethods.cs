using System;
using System.Text.RegularExpressions;

using Amazon.Runtime;

namespace Amazon.AWSToolkit.Exceptions
{
    public static class ExceptionExtensionMethods
    {
        public static string GetServiceName(this Exception ex)
        {
            var serviceException = ex as AmazonServiceException;
            // check if exception is an AmazonServiceException and not an Unmarshalling exception
            if (serviceException == null || serviceException is AmazonUnmarshallingException)
            {
                return null;
            }

            var serviceName = GetServiceName(serviceException.GetType().Name);

            // check one level down for custom modeled service exceptions like AccessDeniedException : AmazonECSException
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                var baseExceptionName = ex.GetType().BaseType?.Name;
                return GetServiceName(baseExceptionName);
            }

            return serviceName;
        }

        private static string GetServiceName(string exceptionName)
        {
            if (exceptionName == null)
            {
                return null;
            }

            var regex = new Regex("Amazon(.*?)Exception");
            var serviceName = regex.Match(exceptionName).Groups[1].Value;
            return string.IsNullOrWhiteSpace(serviceName) ? null : serviceName;
        }
    }
}
