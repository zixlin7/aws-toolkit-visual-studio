using System;
using System.Threading.Tasks;

namespace AwsToolkit.VsSdk.Common.Settings
{
    /// <summary>
    /// Repository to retrieve and store Lsp related settings
    /// </summary>
    public interface ILspSettingsRepository : IDisposable
    {
        /// <summary>
        /// Load the current LSP settings
        /// </summary>
        /// <returns></returns>
        Task<ILspSettings> GetLspSettingsAsync();
    }
}
