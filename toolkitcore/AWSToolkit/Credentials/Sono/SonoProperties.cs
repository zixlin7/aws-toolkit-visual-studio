namespace Amazon.AWSToolkit.Credentials.Sono
{
    internal static class SonoProperties
    {
        public const string ClientName = "AwsToolkitForVisualStudio";
        public const string ClientType = "public";
        public const string DefaultSessionName = "aws-toolkit-visual-studio";

        public const string StartUrl = "https://d-9067642ac7.awsapps.com/start";
        public static readonly string[] Scopes = { "sso:account:access" };

        public static readonly RegionEndpoint DefaultTokenProviderRegion = RegionEndpoint.USEast1;
        public static readonly RegionEndpoint DefaultOidcRegion = RegionEndpoint.USEast1;
    }
}
