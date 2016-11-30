using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.CloudFront;
using Amazon.CloudFront.Model;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.CloudFront.Nodes
{
    public interface ICloudFrontBaseDistributionViewModel : IViewModel
    {
        IAmazonCloudFront CFClient { get; }

        string DistributionId { get; }
        string GetETag();

        string DomainName { get; }
        Aliases Aliases { get; }
    }
}
