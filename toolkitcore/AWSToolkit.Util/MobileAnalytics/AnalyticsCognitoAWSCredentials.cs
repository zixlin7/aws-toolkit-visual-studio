using Amazon.AWSToolkit.Settings;
using Amazon.CognitoIdentity;

namespace Amazon.AWSToolkit.MobileAnalytics
{
    public class AnalyticsCognitoAWSCredentials : CognitoAWSCredentials
    {

        public AnalyticsCognitoAWSCredentials(string cognitoIdentityPoolID, RegionEndpoint regionEndpoint)
            : base(cognitoIdentityPoolID, regionEndpoint) { }

        public override void CacheIdentityId(string identityId)
        {
            ToolkitSettings.Instance.MobileAnalytics.CognitoIdentityId = identityId;
        }

        public override void ClearIdentityCache()
        {
            ToolkitSettings.Instance.MobileAnalytics.CognitoIdentityId = "";
        }

        public override string GetCachedIdentityId()
        {
            return ToolkitSettings.Instance.MobileAnalytics.CognitoIdentityId;
        }
    }
}
