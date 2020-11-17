using System;
using System.Net;

namespace Amazon.AWSToolkit.CodeArtifact.CredentialProvider
{
    public interface ICodeArtifactAuthCredentials
    {
        NetworkCredential GetCredential(Uri uri, string authType);
    }
}
