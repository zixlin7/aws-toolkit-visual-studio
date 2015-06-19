using Amazon.CognitoIdentity;
using Amazon.Runtime.Internal.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.MobileAnalytics
{
    class AnalyticsCognitoAWSCredentials : CognitoAWSCredentials
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
