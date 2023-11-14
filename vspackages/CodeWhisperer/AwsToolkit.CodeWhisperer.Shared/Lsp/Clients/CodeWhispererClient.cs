using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.InlineCompletions;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Install;
using Amazon.AwsToolkit.CodeWhisperer.Settings;
using Amazon.AwsToolkit.CodeWhisperer.Telemetry;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.CommonUI.Notifications;

using Community.VisualStudio.Toolkit;

using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients
{
    // Suppress warnings caused by MEF properties
#pragma warning disable CS0649 // Field 'Foo' is never assigned to, and will always have its default value null
#pragma warning disable IDE0044 // Add readonly modifier

    public interface ICodeWhispererLspClient : IToolkitLspClient
    {
        IInlineCompletions CreateInlineCompletions();
    }

    /// <summary>
    /// MEF component picked up by Visual Studio to define and orchestrate the CodeWhisperer language server.
    /// </summary>
    [Export(typeof(ICodeWhispererLspClient))]
    [Export(typeof(ILanguageClient))]
    [ContentType(ContentTypes.Code)]
    public class CodeWhispererClient : ToolkitLspClient, ICodeWhispererLspClient
    {
        [Import]
        private ICodeWhispererSettingsRepository _settingsRepository;

        [ImportingConstructor]
        public CodeWhispererClient() : base(initializeServerWithCredentials: true)
        {
        }

        public override string Name => "Amazon CodeWhisperer Language Client";

        public override string LanguageServerIdentifier => "CodeWhisperer";

        // TODO IDE-11602
        // Emitting as an anonymous type during early development, but consider options such as proxies, generated code, or hand-managed
        // POCOs to keep the InitializationOptions in sync with AwsInitializationOptions.
        // See https://github.com/aws/aws-language-servers/blob/main/core/aws-lsp-core/src/initialization/awsInitializationOptions.ts
        public override object InitializationOptions => new
        {
            credentials = new
            {
                providesBearerToken = true
            }
        };

        protected override string GetServerPath()
        {
            return !string.IsNullOrWhiteSpace(_serverPath) ? _serverPath : throw new Exception("Error finding CodeWhisperer Language Server location");
        }

        protected override async Task<LspInstallResult> InstallServerAsync(ITaskStatusNotifier taskNotifier, CancellationToken token = default)
        {
            taskNotifier.ProgressText = "Installing CodeWhisperer Language Server...";
            taskNotifier.CanCancel = true;
            var installer = new CodeWhispererInstaller(_serviceProvider, _settingsRepository, _toolkitContext);
            return await installer.ExecuteAsync(taskNotifier, token);
        }

        protected override async Task<ITaskStatusNotifier> CreateTaskStatusNotifierAsync()
        {
            var taskStatus = await _toolkitContext.ToolkitHost.CreateTaskStatusNotifier();
            taskStatus.Title = "AWS Toolkit is setting up CodeWhisperer features";
            taskStatus.ProgressText = "Loading...";
            taskStatus.CanCancel = true;
            return taskStatus;
        }

        protected override string GetServerWorkingDir()
        {
            return Path.GetDirectoryName(GetServerPath());
        }

        public IInlineCompletions CreateInlineCompletions()
        {
            return new InlineCompletions.InlineCompletions(_rpc);
        }
    }
#pragma warning restore IDE0044 // Add readonly modifier
#pragma warning restore CS0649 // Field 'Foo' is never assigned to, and will always have its default value null
}
