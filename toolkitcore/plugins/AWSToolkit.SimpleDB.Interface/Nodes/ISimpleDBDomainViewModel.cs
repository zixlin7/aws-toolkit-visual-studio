using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.SimpleDB.Nodes
{
    public interface ISimpleDBDomainViewModel : IViewModel
    {
        string Domain { get; }
    }
}
