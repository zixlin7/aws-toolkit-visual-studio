using System;

namespace Amazon.AWSToolkit.Credentials.Utils
{
    public class CredentialProviderNotFoundException : Exception
    {
        public CredentialProviderNotFoundException(string message)
            : base(message)
        {
        }

        public CredentialProviderNotFoundException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
