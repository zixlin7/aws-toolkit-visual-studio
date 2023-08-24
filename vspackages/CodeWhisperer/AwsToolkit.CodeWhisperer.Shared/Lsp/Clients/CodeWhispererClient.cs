using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Settings;

using Community.VisualStudio.Toolkit;

using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients
{
    // Suppress warnings caused by MEF properties
#pragma warning disable CS0649 // Field 'Foo' is never assigned to, and will always have its default value null
#pragma warning disable IDE0044 // Add readonly modifier

    /// <summary>
    /// MEF component picked up by Visual Studio to define and orchestrate the CodeWhisperer language server.
    /// </summary>
    [Export(typeof(ILanguageClient))]
    [ContentType(ContentTypes.CSharp)]
    public class CodeWhispererClient : ToolkitLspClient
    {
        [Import]
        private ICodeWhispererLspSettingsRepository _settingsRepository;

        [ImportingConstructor]
        public CodeWhispererClient() : base(initializeServerWithCredentials: true)
        {
        }

        public override string Name => "Amazon CodeWhisperer Language Client";

        protected override async Task<string> GetServerPathAsync()
        {
            return await GetLanguageServerPathAsync();
        }

        protected override async Task<string> GetServerWorkingDirAsync()
        {
            return Path.GetDirectoryName(await GetLanguageServerPathAsync());
        }

        private async Task<string> GetLanguageServerPathAsync()
        {
            // TODO : We will need to know where the Toolkit stores the language server.
            // For now, Toolkit developers will point at a binary using an env var.
            var settings = await _settingsRepository.GetAsync();
            return !string.IsNullOrWhiteSpace(settings.LanguageServerPath) ? settings.LanguageServerPath : throw new Exception($"Configure a CodeWhisperer language server location");
        }
    }
#pragma warning restore IDE0044 // Add readonly modifier
#pragma warning restore CS0649 // Field 'Foo' is never assigned to, and will always have its default value null
}
