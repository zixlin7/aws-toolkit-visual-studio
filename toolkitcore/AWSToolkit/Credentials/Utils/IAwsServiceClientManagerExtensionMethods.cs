using System;

using Amazon.AWSToolkit.Clients;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;

using log4net;

namespace Amazon.AWSToolkit.Credentials.Utils
{
    public static class IAwsServiceClientManagerExtensionMethods
    {
        public static readonly ILog Logger = LogManager.GetLogger(typeof(IAwsServiceClientManagerExtensionMethods));

        public static string GetAccountId(this IAwsServiceClientManager @this, AwsConnectionSettings connectionSettings)
        {
            try
            {
                using (var sts = @this.CreateServiceClient<AmazonSecurityTokenServiceClient>(connectionSettings.CredentialIdentifier, connectionSettings.Region))
                {
                    var response = sts.GetCallerIdentity(new GetCallerIdentityRequest());
                    return response.Account;
                }
            }
            catch (Exception e)
            {
                Logger.Error("Unable to determine AWS account ID.", e);
                return null;
            }
        }
    }
}
