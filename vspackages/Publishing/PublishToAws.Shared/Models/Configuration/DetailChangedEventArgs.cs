using System;

namespace Amazon.AWSToolkit.Publish.Models.Configuration
{
    /// <summary>
    /// Signals that a value changed within this detail or its children
    /// </summary>
    public class DetailChangedEventArgs : EventArgs
    {
        public ConfigurationDetail Detail { get; }

        public DetailChangedEventArgs(ConfigurationDetail detail)
        {
            Detail = detail;
        }
    }
}
