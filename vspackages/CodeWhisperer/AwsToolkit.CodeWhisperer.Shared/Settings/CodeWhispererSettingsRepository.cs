using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

using AwsToolkit.VsSdk.Common.Settings;
using AwsToolkit.VsSdk.Common.Settings.CodeWhisperer;

namespace Amazon.AwsToolkit.CodeWhisperer.Settings
{
    /// <summary>
    /// Repository to retrieve and store CodeWhisperer related settings.
    /// Gives MEF components some separation from the actual CodeWhispererSettings I/O.
    ///
    /// Settings are backed by Visual Studio's own settings system.
    /// </summary>
    [Export(typeof(ICodeWhispererSettingsRepository))]
    internal class CodeWhispererSettingsRepository : ICodeWhispererSettingsRepository
    {
        [ImportingConstructor]
        public CodeWhispererSettingsRepository()
        {
            CodeWhispererSettings.Saved += CodeWhispererSettingsOnSaved;
        }

        /// <summary>
        /// Event signalling that the CodeWhisperer settings have been saved.
        /// This could be caused through programmatic access, or when a user
        /// changes settings in the Visual Studio Settings dialog.
        /// This event does not guarantee that a setting has changed since
        /// the last time it was saved.
        /// </summary>
        public event EventHandler<CodeWhispererSettingsSavedEventArgs> SettingsSaved;

        public Task<CodeWhispererSettings> GetAsync()
        {
            return CodeWhispererSettings.GetLiveInstanceAsync();
        }

        public async Task<ILspSettings> GetLspSettingsAsync()
        {
            ILspSettings settings = await CodeWhispererSettings.GetLiveInstanceAsync();
            return settings;
        }

        public void Save(CodeWhispererSettings settings)
        {
            settings.Save();
        }

        public void Dispose()
        {
            CodeWhispererSettings.Saved -= CodeWhispererSettingsOnSaved;
        }

        private void CodeWhispererSettingsOnSaved(CodeWhispererSettings settings)
        {
            SettingsSaved?.Invoke(this, new CodeWhispererSettingsSavedEventArgs()
            {
                Settings = settings,
            });
        }
    }
}
