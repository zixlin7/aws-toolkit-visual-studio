using System;
using System.Windows;
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
        /// Returns an IDE handle that can be used as a parent to extension modal dialogs
        /// </summary>
        IntPtr GetParentWindowHandle();

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
        /// <param name="action"></param>
        void ExecuteOnUIThread(Action action);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        void BeginExecuteOnUIThread(Action action);

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
        /// Requests a service implemented in our hosting shell.
        /// </summary>
        /// <typeparam name="T">The interface of the requested service</typeparam>
        /// <returns>The service or null if unknown</returns>
        T QueryShellProviderService<T>() where T : class;

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

        /// <summary>
        /// Opens a web browser view onto the specified url.
        /// </summary>
        /// <param name="url">The url to open</param>
        /// <param name="preferInternalBrowser">
        /// If the host shell supports internal browsing and preferInternalBrowser is set true
        /// then the url will be opened using the host shell's built-in browser, falling back
        /// to the system default browser (in a separate process) if necessary.
        /// </param>
        void OpenInBrowser(string url, bool preferInternalBrowser);

        /// <summary>
        /// Uses the unique id of the supplied control to find an opened editor window
        /// to be closed.
        /// </summary>
        /// <param name="editorControl"></param>
        void CloseEditor(IAWSToolkitControl editorControl);

        /// <summary>
        /// Uses the supplied filename to find an opened editor window to be closed.
        /// </summary>
        /// <param name="fileName"></param>
        void CloseEditor(string fileName);
    }

    /// <summary>
    /// Marker interface exposing the core AWSToolkit across the rest of our toolkit packages
    /// </summary>
    [Guid(ShellProviderServiceGuids.SAWSToolkitShellProviderIdentifier)]
    public interface SAWSToolkitShellProvider
    {
    }
}
