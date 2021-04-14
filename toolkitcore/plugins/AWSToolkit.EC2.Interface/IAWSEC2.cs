using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Regions;
using Amazon.EC2;

namespace Amazon.AWSToolkit.EC2
{
    public interface IAWSEC2
    {
        bool IsVpcOnly(IAmazonEC2 ec2Client);

        bool IsVpcOnly(AccountViewModel account, ToolkitRegion region);
    }
}
