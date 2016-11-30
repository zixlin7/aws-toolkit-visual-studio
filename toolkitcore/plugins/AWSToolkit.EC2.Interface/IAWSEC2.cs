using Amazon.AWSToolkit.Account;
using Amazon.EC2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.EC2
{
    public interface IAWSEC2
    {
        bool IsVpcOnly(IAmazonEC2 ec2Client);

        bool IsVpcOnly(AccountViewModel account, RegionEndpoint region);
    }
}
