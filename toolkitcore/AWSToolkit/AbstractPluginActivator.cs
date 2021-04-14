using System;
using Amazon.AWSToolkit.Context;

namespace Amazon.AWSToolkit
{
    public abstract class AbstractPluginActivator : IPluginActivator
    {
        protected ToolkitContext ToolkitContext;

        public void Initialize(ToolkitContext toolkitContext)
        {
            ToolkitContext = toolkitContext;
        }

        public abstract string PluginName { get; }
        public virtual void RegisterMetaNodes() { }
        public virtual object QueryPluginService(Type serviceType) { return null; }
    }
}
