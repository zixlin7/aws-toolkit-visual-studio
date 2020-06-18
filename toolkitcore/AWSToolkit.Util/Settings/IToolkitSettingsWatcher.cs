using System;

namespace Amazon.AWSToolkit.Settings
{
    public interface IToolkitSettingsWatcher
    {
        event EventHandler SettingsChanged;
    }
}