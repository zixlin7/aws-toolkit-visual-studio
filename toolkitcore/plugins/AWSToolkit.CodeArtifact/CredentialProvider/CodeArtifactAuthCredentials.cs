using System;
using System.Net;
using System.Security;

namespace Amazon.AWSToolkit.CodeArtifact.CredentialProvider
{
    public class CodeArtifactAuthCredentials : ICredentials, ICodeArtifactAuthCredentials
    {
        private readonly string _username;
        private readonly SecureString _token;

        public CodeArtifactAuthCredentials(string username, SecureString token)
        {
            _username = username;
            _token = token;
        }

        public NetworkCredential GetCredential(Uri uri, string authType)
        {
            return new NetworkCredential(_username, _token);
        }
    }
}
