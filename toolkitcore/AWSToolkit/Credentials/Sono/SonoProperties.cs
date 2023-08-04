namespace Amazon.AWSToolkit.Credentials.Sono
{
    internal static class SonoProperties
    {
        // When making Sono* more generic as part of CW integration for SSO token, make
        // ClientName and ClientType more generic as used by SSO as well.
        public const string ClientName = "AWS Toolkit for Visual Studio";
        public const string ClientType = "public";
        public const string DefaultSessionName = "aws-toolkit-visual-studio";

        public const string StartUrl = "https://d-9067642ac7.awsapps.com/start";
        public const string SsoAccountAccessScope = "sso:account:access";
        public const string CodeCatalystReadWriteScope = "codecatalyst:read_write";
        public static readonly string[] Scopes = { SsoAccountAccessScope, CodeCatalystReadWriteScope };

        public static readonly RegionEndpoint DefaultTokenProviderRegion = RegionEndpoint.USEast1;
        public static readonly RegionEndpoint DefaultOidcRegion = RegionEndpoint.USEast1;
    }
}
