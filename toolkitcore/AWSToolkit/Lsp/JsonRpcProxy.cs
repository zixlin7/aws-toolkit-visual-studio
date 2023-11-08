using System;
using System.Reflection;

namespace Amazon.AWSToolkit.Lsp
{
    /// <summary>
    /// Helps resolve JSON-RPC calls made through strongly typed requests.
    /// See: https://github.com/microsoft/vs-streamjsonrpc/blob/main/doc/sendrequest.md
    /// </summary>
    public static class JsonRpcProxy
    {
        /// <summary>
        /// Given the function name from a proxy interface, returns the corresponding JSON-RPC message name.
        /// This is used with JsonRpc.Attach to create a proxy for making calls with strongly typed code.
        /// See JsonRpcProxyOptions.MethodNameTransform
        /// https://learn.microsoft.com/en-us/dotnet/api/streamjsonrpc.jsonrpcproxyoptions.methodnametransform?view=streamjsonrpc-2.9#streamjsonrpc-jsonrpcproxyoptions-methodnametransform
        /// </summary>
        /// <typeparam name="TProxy">The Interface to transform a function name to a message name</typeparam>
        /// <param name="functionName">The function name to transform to a message name</param>
        /// <returns>Message name corresponding to the given function name. If there is no mapping, the function name is returned.</returns>
        public static string MethodNameTransform<TProxy>(string functionName)
        {
            return GetMessageNameOrDefault<TProxy>(functionName, functionName);
        }

        /// <summary>
        /// Looks up the message name for a given proxy function. Returns default if there is an error, or no mapping was found.
        /// </summary>
        public static string GetMessageNameOrDefault<TProxy>(string functionName, string defaultMessageName)
        {
            try
            {
                var messageName = GetMessageName<TProxy>(functionName);
                return string.IsNullOrWhiteSpace(messageName) ? defaultMessageName : messageName;
            }
            catch (Exception)
            {
                return defaultMessageName;
            }
        }

        /// Looks up the message name for a given proxy function. Returns null if there is an error, or no mapping was found.
        public static string GetMessageName<TProxy>(string functionName)
        {
            try
            {
                var classType = typeof(TProxy);
                var method = classType.GetMethod(functionName);
                var mappingAttrib = method.GetCustomAttribute<JsonRpcMessageMappingAttribute>();
                return mappingAttrib.MessageName;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
