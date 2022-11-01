using System;

using log4net;

namespace Amazon.AWSToolkit.CodeCatalyst
{
    internal class CodeCatalystPluginActivator : AbstractPluginActivator
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(CodeCatalystPluginActivator));

        public override string PluginName => "CodeCatalyst";

        private Lazy<IAWSCodeCatalyst> PluginService { get; }

        public CodeCatalystPluginActivator()
        {
            PluginService = new Lazy<IAWSCodeCatalyst>(() => new CodeCatalystPluginService(ToolkitContext));
        }

        public override object QueryPluginService(Type serviceType)
        {
            return serviceType == typeof(IAWSCodeCatalyst) ? PluginService.Value : null;
        }
    }
}
