using System;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Regions;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using log4net;

namespace Amazon.AWSToolkit.Credentials.Utils
{
    public static class AccountViewModelExtensionMethods
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AccountViewModelExtensionMethods));

        public static string GetAccountId(this AccountViewModel account, ToolkitRegion region)
        {
            try
            {
                var sts = account.CreateServiceClient<AmazonSecurityTokenServiceClient>(region);

                var response = sts.GetCallerIdentity(new GetCallerIdentityRequest());
                return response.Account;
            }
            catch (Exception e)
            {
                Logger.Error("Unable to determine account id", e);
                return null;
            }
        }
    }
}
