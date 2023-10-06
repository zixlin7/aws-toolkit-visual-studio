namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials.Models
{
    /// <summary>
    /// Constant definitions for Jwt header/field names
    /// Some names are standard, see https://datatracker.ietf.org/doc/html/rfc7519#page-9
    /// </summary>
    public static class JwtFieldNames
    {
        public const string Data = "data";
        public const string NotBefore = "nbf";
        public const string ExpiresOn = "exp";
    }
}
