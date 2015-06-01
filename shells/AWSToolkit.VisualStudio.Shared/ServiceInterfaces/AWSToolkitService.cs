using System;
using System.Runtime.InteropServices;

namespace Amazon.AWSToolkit.VisualStudio.Shared.ServiceInterfaces
{
    /// <summary>
    /// Interface onto the inner AWSToolkit singleton instance
    /// </summary>
    [Guid("1401BED5-BF44-4f33-A835-04002D66781B")]
    [ComVisible(true)]
    public interface IAWSToolkitService
    {
        /// <summary>
        /// Returns requested interface on a plugin loaded by the toolkit
        /// </summary>
        /// <param name="pluginServiceType">The type of the interface exposed by the plugin</param>
        /// <returns>Plugin interface instance or null if plugin not loaded</returns>
        object QueryAWSToolkitPluginService(Type pluginServiceType);

        /// <summary>
        /// Outputs the specified message to the 'Amazon Web Services' output window tab.
        /// The tab is created if necessary. If already created, the visibility state
        /// of the tab remains unchanged.
        /// </summary>
        /// <param name="message"></param>
        void OutputToConsole(string message);

        /// <summary>
        /// Outputs the specified message to the 'Amazon Web Services' output window tab,
        /// forcing the tab visible (and creating it if necessary).
        /// </summary>
        /// <param name="message"></param>
        /// <param name="forceVisible"></param>
        void OutputToConsole(string message, bool forceVisible);

        /// <summary>
        /// Adds the specified message to the toolkit shell's logfile
        /// </summary>
        /// <param name="category">Type of message; one of 'debug', 'warn', 'error' or 'info'</param>
        /// <param name="message"></param>
        void AddToLog(string category, string message);
    }

    /// <summary>
    /// Marker interface exposing the core AWSToolkit across VS2010 plugins
    /// </summary>
    [Guid("B95300F6-10E0-4f28-8EDF-00F087D6EEBE")]
    public interface SAWSToolkitService
    {
    }
}
