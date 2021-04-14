using System;
using Amazon.AWSToolkit.Context;

namespace Amazon.AWSToolkit
{
    public interface IPluginActivator
    {
        string PluginName { get; }
        void Initialize(ToolkitContext toolkitContext);
        void RegisterMetaNodes();
        object QueryPluginService(Type serviceType);
    }
}
