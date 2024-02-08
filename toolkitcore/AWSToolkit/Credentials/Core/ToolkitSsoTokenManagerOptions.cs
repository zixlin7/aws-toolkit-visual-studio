using System;
using System.Collections.Generic;

using Amazon.Runtime;

namespace Amazon.AWSToolkit.Credentials.Core
{
    public sealed class ToolkitSsoTokenManagerOptions
    {
        public string ClientName { get; }
        public string ClientType { get; }
        public string CredentialName { get; }
        public bool IsBuilderId { get; }
        public Action<SsoVerificationArguments> SsoVerificationCallback { get; }
        public IEnumerable<string> Scopes { get; }

        public ToolkitSsoTokenManagerOptions(string clientName, string clientType, string credentialName,
            Action<SsoVerificationArguments> ssoVerificationCallback, IEnumerable<string> scopes, bool isBuilderId)
        {
            ClientName = clientName;
            ClientType = clientType;
            CredentialName = credentialName;
            SsoVerificationCallback = ssoVerificationCallback;
            Scopes = scopes;
            IsBuilderId = isBuilderId;
        }
    }
}
