using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit
{
    public abstract class AbstractPluginActivator : IPluginActivator
    {
        public abstract string PluginName { get; }
        public virtual void RegisterMetaNodes() { }
        public virtual object QueryPluginService(Type serviceType) { return null; }
    }
}
