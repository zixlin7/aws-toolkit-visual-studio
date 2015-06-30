using System;
using System.Collections.Generic;
using System.Text;

namespace Amazon.AWSToolkit
{
    public class ToolkitSettingsConstants
    {
        public const string UserPreferences = "UserPreferences";

        public const string MiscSettings = Amazon.Runtime.Internal.Settings.SettingsConstants.MiscSettings;

        public const string RegisteredProfiles = Amazon.Runtime.Internal.Settings.SettingsConstants.RegisteredProfiles;
        public const string RecentUsages = "RecentUsages";

        public const string DisplayNameField = Amazon.Runtime.Internal.Settings.SettingsConstants.DisplayNameField;
        public const string AccessKeyField = Amazon.Runtime.Internal.Settings.SettingsConstants.AccessKeyField;
        public const string SecretKeyField = Amazon.Runtime.Internal.Settings.SettingsConstants.SecretKeyField;
        public const string AccountNumberField = Amazon.Runtime.Internal.Settings.SettingsConstants.AccountNumberField;
        public const string Restrictions = Amazon.Runtime.Internal.Settings.SettingsConstants.Restrictions;

        public const string SecretKeyRepository = Amazon.Runtime.Internal.Settings.SettingsConstants.SecretKeyRepository;

        public const string LastAcountSelectedKey = "LastAcountSelectedKey";

        public const string VersionCheck = "VersionCheck";
        public const string LastVersionDoNotRemindMe = "LastVersionDoNotRemindMe";

        public const string HostedFilesLocation = "HostedFilesLocation";

        public const string EC2ConnectSettings = "EC2ConnectSettings";

        public const string EC2InstanceUseKeyPair = "EC2RDPUseKeyPair";
        public const string EC2InstanceMapDrives = "EC2RDPMapDrives";

        public const string EC2InstanceSaveCredentials = "EC2InstanceSaveCredentials";
        public const string EC2InstanceUserName = Amazon.Runtime.Internal.Settings.SettingsConstants.EC2InstanceUserName;
        public const string EC2InstancePassword = Amazon.Runtime.Internal.Settings.SettingsConstants.EC2InstancePassword;

        public const string AnalyticsPermission = "AnalyticsPermission";
        public const string AnalyticsCognitoIdentityId = "AnalyticsCognitoIdentityId";
        public const string AnalyticsCustomerGuid = "AnalyticsCustomerGuid";
        public const string AnalyticsMostReventlyUsedCognitoIdentityPool = "AnalyticsMostReventlyUsedCognitoIdentityPool";
    }
}
