namespace Amazon.AWSToolkit.Settings
{
    public class AppDataToolkitSettingsRepository : IToolkitSettingsRepository
    {
        public string GetLastSelectedCredentialId()
        {
            return ToolkitSettings.Instance.LastSelectedCredentialId;
        }

        public string GetLastSelectedRegion()
        {
            return ToolkitSettings.Instance.LastSelectedRegion;
        }
    }
}
