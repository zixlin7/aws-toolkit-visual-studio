using Amazon.RDS;

namespace Amazon.AWSToolkit.RDS.Nodes
{
    public interface IRDSRootViewModel
    {
        IAmazonRDS RDSClient { get; }
    }
}
