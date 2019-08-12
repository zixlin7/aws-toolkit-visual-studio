using Amazon.CognitoIdentity;
using Amazon.Runtime.Internal.Settings;

namespace Amazon.AWSToolkit.MobileAnalytics
{
    public class AnalyticsCognitoAWSCredentials : CognitoAWSCredentials
    {

        public AnalyticsCognitoAWSCredentials(string cognitoIdentityPoolID, RegionEndpoint regionEndpoint)
            : base(cognitoIdentityPoolID, regionEndpoint) { }

        public override void CacheIdentityId(string identityId)
        {
            PersistenceManager.Instance.SetSetting(ToolkitSettingsConstants.AnalyticsCognitoIdentityId, identityId);
        }

        public override void ClearIdentityCache()
        {
            PersistenceManager.Instance.SetSetting(ToolkitSettingsConstants.AnalyticsCognitoIdentityId, "");
        }

        public override string GetCachedIdentityId()
        {
            return PersistenceManager.Instance.GetSetting(ToolkitSettingsConstants.AnalyticsCognitoIdentityId);
        }
    }
}
