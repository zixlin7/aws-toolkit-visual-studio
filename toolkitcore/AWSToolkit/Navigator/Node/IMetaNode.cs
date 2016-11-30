using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
