using System;
using System.Threading.Tasks;

using AwsToolkit.VsSdk.Common.Settings.CodeWhisperer;

namespace Amazon.AwsToolkit.CodeWhisperer.Settings
{
    /// <summary>
    /// Repository to retrieve and store CodeWhisperer related settings
    /// </summary>
    public interface ICodeWhispererSettingsRepository : IDisposable
    {
        event EventHandler<CodeWhispererSettingsSavedEventArgs> SettingsSaved;

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
