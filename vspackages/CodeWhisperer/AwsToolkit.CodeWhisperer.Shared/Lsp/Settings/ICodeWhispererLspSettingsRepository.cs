using System.Threading.Tasks;

using AwsToolkit.VsSdk.Common.Settings.CodeWhisperer;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Settings
{
    /// <summary>
    /// Repository to retrieve and store CodeWhisperer language server related settings
    /// </summary>
    public interface ICodeWhispererLspSettingsRepository
    {
        /// <summary>
        /// Load the current CodeWhisperer settings
        /// </summary>
        /// <returns></returns>
        Task<CodeWhispererSettings> GetAsync();

        /// <summary>
        /// Update the current CodeWhisperer settings with the given values
        /// </summary>
        void Save(CodeWhispererSettings settings);
    }
}
