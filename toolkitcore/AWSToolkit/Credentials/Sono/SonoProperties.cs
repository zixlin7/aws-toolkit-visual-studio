namespace Amazon.AWSToolkit.Credentials.Sono
{
    public static class SonoProperties
    {
        public const string ClientName = "AWS Toolkit for Visual Studio";
        public const string ClientType = "public";

        public const string StartUrl = "https://d-9067642ac7.awsapps.com/start";
        public const string SsoAccountAccessScope = "sso:account:access";

        public const string CodeCatalystReadWriteScope = "codecatalyst:read_write";
        public static readonly string[] CodeCatalystScopes = new[] { CodeCatalystReadWriteScope };

        public const string CodeWhispererCompletionsScope = "codewhisperer:completions";
        public const string CodeWhispererAnalysisScope = "codewhisperer:analysis";
        public static readonly string[] CodeWhispererScopes = new[] { CodeWhispererCompletionsScope, CodeWhispererAnalysisScope };

        public static readonly RegionEndpoint DefaultTokenProviderRegion = RegionEndpoint.USEast1;
        public static readonly RegionEndpoint DefaultOidcRegion = RegionEndpoint.USEast1;
    }
}
