using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Runtime.InteropServices;

namespace Amazon.AWSToolkit.Shared
{
    public abstract class ShellProviderServiceGuids
    {
        public const string IAWSToolkitShellProviderIdentifier = "b1c1863d-5a0c-492c-9a43-5d01a087d7cd";
        public const string SAWSToolkitShellProviderIdentifier = "48d7723b-08f7-4c33-8e92-fe5c3c427ccc";
    }

    public enum ShellWindows
    {
        Explorer,
        Output
    }

    /// <summary>
    /// Interface to be implemented by the shell that is hosting the toolkit.
    /// </summary>
    [Guid(ShellProviderServiceGuids.IAWSToolkitShellProviderIdentifier)]
    [ComVisible(true)]
    public interface IAWSToolkitShellProvider
    {
        /// <summary>
        /// 
        /// </summary>
        string ShellName { get; }

        /// <summary>
        /// 
        /// </summary>
        string ShellVersion { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="window"></param>
        void OpenShellWindow(ShellWindows window);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="editorControl"></param>
        void OpenInEditor(IAWSToolkitControl editorControl);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        void OpenInEditor(string fileName);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostedControl"></param>
        /// <returns></returns>
        bool ShowModal(IAWSToolkitControl hostedControl);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostedControl"></param>
        /// <param name="buttons"></param>
        /// <returns></returns>
        bool ShowModal(IAWSToolkitControl hostedControl, MessageBoxButton buttons);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostedControl"></param>
        /// <returns></returns>
        bool ShowModalFrameless(IAWSToolkitControl hostedControl);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="window"></param>
        /// <param name="metricId"></param>
        /// <returns></returns>
        bool ShowModal(Window window, string metricId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        void ShowError(string message);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        void ShowError(string title, string message);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        void ShowErrorWithLinks(string title, string message);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        void ShowMessage(string title, string message);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        bool Confirm(string title, string message);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <param name="buttons"></param>
        /// <returns></returns>
        bool Confirm(string title, string message, MessageBoxButton buttons);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="status"></param>
        void UpdateStatus(string status);

        /// <summary>
        /// 
        /// </summary>
        Dispatcher ShellDispatcher {get;}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        void OutputToHostConsole(string message);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="forceVisible"></param>
        void OutputToHostConsole(string message, bool forceVisible);

        /// <summary>
        /// Adds the specified message to the toolkit shell's logfile
        /// </summary>
        /// <param name="category">Type of message; one of 'debug', 'warn', 'error' or 'info'</param>
        /// <param name="message"></param>
        void AddToLog(string category, string message);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T QueryShellProverService<T>() where T : class;

        /// <summary>
        /// Returns requested interface on a plugin loaded by the toolkit
        /// </summary>
        /// <param name="pluginServiceType">The type of the interface exposed by the plugin</param>
        /// <returns>Plugin interface instance or null if plugin not loaded</returns>
        object QueryAWSToolkitPluginService(Type pluginServiceType);

        /// <summary>
        /// Returns requested interface on a plugin loaded by the toolkit
        /// </summary>
        /// <param name="pluginServiceType">The name of the interface type exposed by the plugin</param>
        /// <returns>Plugin interface instance or null if plugin not loaded</returns>
        object QueryAWSToolkitPluginService(string pluginServiceType);

    }

    /// <summary>
    /// Marker interface exposing the core AWSToolkit across the rest of our toolkit packages
    /// </summary>
    [Guid(ShellProviderServiceGuids.SAWSToolkitShellProviderIdentifier)]
    public interface SAWSToolkitShellProvider
    {
    }
}
