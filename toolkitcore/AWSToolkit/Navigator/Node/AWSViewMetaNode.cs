using Amazon.AWSToolkit.Account;

namespace Amazon.AWSToolkit.Navigator.Node
{
    public class AWSViewMetaNode : AbstractMetaNode
    {
        public AWSViewMetaNode()
        {
            this.Children.Add(new AccountViewMetaNode());
        }
    }
}
