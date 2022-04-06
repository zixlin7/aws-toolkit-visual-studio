using System.Collections.Generic;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;
using Amazon.EC2;

namespace Amazon.AWSToolkit.EC2
{
    public interface IAWSEC2
    {
        bool IsVpcOnly(IAmazonEC2 ec2Client);

        bool IsVpcOnly(AccountViewModel account, ToolkitRegion region);

        void ConnectToInstance(AwsConnectionSettings connectionSettings, string settingsUniqueKey, IList<string> instanceIds);

        void ConnectToInstance(AwsConnectionSettings connectionSettings, string settingsUniqueKey, string instanceId);
    }
}
