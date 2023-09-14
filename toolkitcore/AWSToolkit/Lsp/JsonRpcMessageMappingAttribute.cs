using System;

namespace Amazon.AWSToolkit.Lsp
{
    /// <summary>
    /// Associates the function name of a JSON-RPC notification/request to the message name it is a proxy for.
    /// <see cref="JsonRpcProxy"/> contains utility methods leveraging this attribute.
    /// See: https://github.com/microsoft/vs-streamjsonrpc/blob/main/doc/sendrequest.md
    /// </summary>
    public class JsonRpcMessageMappingAttribute : Attribute
    {
        public JsonRpcMessageMappingAttribute(string messageName)
        {
            MessageName = messageName;
        }

        public string MessageName { get; private set; }
    }
}
