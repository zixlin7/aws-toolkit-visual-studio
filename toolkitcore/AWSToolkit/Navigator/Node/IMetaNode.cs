using System.Collections.Generic;

namespace Amazon.AWSToolkit.Navigator.Node
{
    public interface IMetaNode
    {
        bool SupportsRefresh { get; }
        bool SupportsEndPoint { get; }
        string EndPointSystemName { get; }

        T FindChild<T>() where T : IMetaNode;

        IList<ActionHandlerWrapper> Actions
        {
            get;
        }
    }
}
