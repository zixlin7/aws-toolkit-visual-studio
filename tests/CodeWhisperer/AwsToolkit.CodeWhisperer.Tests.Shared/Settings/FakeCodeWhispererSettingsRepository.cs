using System;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Settings;

using AwsToolkit.VsSdk.Common.Settings;
using AwsToolkit.VsSdk.Common.Settings.CodeWhisperer;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Settings
{
    /// <summary>
    /// Fake settings repo that stores settings in memory.
    /// </summary>
    public class FakeCodeWhispererSettingsRepository : ICodeWhispererSettingsRepository
    {
        public CodeWhispererSettings Settings = new CodeWhispererSettings();

        public event EventHandler<CodeWhispererSettingsSavedEventArgs> SettingsSaved;

        public virtual Task<CodeWhispererSettings> GetAsync()
        {
            return Task.FromResult(Settings);
        }

        public virtual void Save(CodeWhispererSettings settings)
        {
            Settings = settings;
        }

        public void Dispose()
        {
        }

        public void RaiseSettingsSaved()
        {
            SettingsSaved?.Invoke(this, new CodeWhispererSettingsSavedEventArgs()
            {
                Settings = Settings,
            });
        }

        public Task<ILspSettings> GetLspSettingsAsync()
        {
            return Task.FromResult((ILspSettings)Settings);
        }
    }
}
