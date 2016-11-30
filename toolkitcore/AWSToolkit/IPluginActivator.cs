using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit
{
    public interface IPluginActivator
    {
        string PluginName { get; }
        void RegisterMetaNodes();
        object QueryPluginService(Type serviceType);
    }
}
