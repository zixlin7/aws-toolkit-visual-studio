using Amazon.SimpleDB;

using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.SimpleDB.Nodes
{
    public interface ISimpleDBRootViewModel : IServiceRootViewModel
    {
        IAmazonSimpleDB SimpleDBClient { get; }
    }
}
