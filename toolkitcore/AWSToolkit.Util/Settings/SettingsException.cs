using System;

namespace Amazon.AWSToolkit.Settings
{
    /// <summary>
    /// An exception that is thrown when a error occurred interacting with Toolkit managed Settings.
    /// </summary>
    public class SettingsException : Exception
    {
        public SettingsException(string message, Exception inner) : base(message, inner)
        {

        }
    }
}
