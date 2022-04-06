
using Amazon.AWSToolkit.Clients;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;

using log4net;

namespace Amazon.AWSToolkit.Credentials.Core
{
    public class AwsConnectionSettings
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AwsConnectionSettings));

        private readonly ICredentialIdentifier _credentialIdentifier;

        private readonly ToolkitRegion _region;

        private string _accountId;

        public ICredentialIdentifier CredentialIdentifier { get => _credentialIdentifier; }

        public ToolkitRegion Region { get => _region; }

        public AwsConnectionSettings(ICredentialIdentifier credentialIdentifier, ToolkitRegion region)
        {
            _credentialIdentifier = credentialIdentifier;
            _region = region;
        }

        public string GetAccountId(IAwsServiceClientManager awsServiceClientManager)
        {
            if (_accountId == null)
            {
                _accountId = awsServiceClientManager.GetAccountId(this);
            }

            return _accountId;
        }
    }
}
