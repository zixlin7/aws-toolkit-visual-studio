using System.ComponentModel.Composition;
using System.Threading.Tasks;

using AwsToolkit.VsSdk.Common.Settings.CodeWhisperer;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Settings
{
    /// <summary>
    /// Repository to retrieve and store CodeWhisperer language server related settings.
    /// Gives MEF components some separation from the actual CodeWhispererSettings I/O.
    ///
    /// Settings are backed by Visual Studio's own settings system.
    /// </summary>
    [Export(typeof(ICodeWhispererLspSettingsRepository))]
    public class CodeWhispererLspSettingsRepository : ICodeWhispererLspSettingsRepository
    {
        [ImportingConstructor]
        public CodeWhispererLspSettingsRepository()
        {
        }

        public Task<CodeWhispererSettings> GetAsync()
        {
            return CodeWhispererSettings.GetLiveInstanceAsync();
        }

        public void Save(CodeWhispererSettings settings)
        {
            settings.Save();
        }
    }
}
