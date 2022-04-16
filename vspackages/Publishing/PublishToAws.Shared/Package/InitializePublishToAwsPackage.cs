using System;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.PluginServices.Publishing;
using Amazon.AWSToolkit.Shared;

namespace Amazon.AWSToolkit.Publish.Package
{
    /// <summary>
    /// VS Service used to initialize the Publish to AWS Package from the main
    /// Toolkit Package.
    /// </summary>
    public class InitializePublishToAwsPackage : SInitializePublishToAwsPackage, IInitializePublishToAwsPackage
    {
        public event EventHandler<EventArgs> Initialize;

        private bool _isInitialized = false;

        internal ToolkitContext ToolkitContext { get; private set; }
        internal IAWSToolkitShellProvider ShellProvider { get; private set; }

        public InitializePublishToAwsPackage()
        {

        }

        public Task InitializePackageAsync(ToolkitContext toolkitContext, IAWSToolkitShellProvider shellProvider)
        {
            if (_isInitialized)
            {
                throw new Exception("Already initialized");
            }

            ToolkitContext = toolkitContext;
            ShellProvider = shellProvider;

            _isInitialized = true;
            RaiseInitialize();

            return Task.CompletedTask;
        }

        private void RaiseInitialize()
        {
            Initialize?.Invoke(this, EventArgs.Empty);
        }
    }
}
