using System;
using System.Collections.Generic;

using Amazon.Runtime;

namespace Amazon.AWSToolkit.Credentials.Core
{
    public sealed class ToolkitSsoTokenManagerOptions
    {
        public string ClientName { get; }
        public string ClientType { get; }
        public Action<SsoVerificationArguments> SsoVerificationCallback { get; }
        public IEnumerable<string> Scopes { get; }

        public ToolkitSsoTokenManagerOptions(string clientName, string clientType,
            Action<SsoVerificationArguments> ssoVerificationCallback, IEnumerable<string> scopes)
        {
            ClientName = clientName;
            ClientType = clientType;
            SsoVerificationCallback = ssoVerificationCallback;
            Scopes = scopes;
        }
    }
}
