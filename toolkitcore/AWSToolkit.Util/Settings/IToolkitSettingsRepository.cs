namespace Amazon.AWSToolkit.Settings
{
    /// <summary>
    /// Persistence interface for getting configured ToolkitSettings.
    /// </summary>
    public interface IToolkitSettingsRepository
    {
        string GetLastSelectedCredentialId();
        string GetLastSelectedRegion();
    }
}
