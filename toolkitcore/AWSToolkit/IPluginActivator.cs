using System;

namespace Amazon.AWSToolkit
{
    public interface IPluginActivator
    {
        string PluginName { get; }
        void RegisterMetaNodes();
        object QueryPluginService(Type serviceType);
    }
}
