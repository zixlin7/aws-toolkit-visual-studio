using System;
using System.Windows;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.Notifications;
using Amazon.AWSToolkit.CommonUI.Notifications.Progress;
using Amazon.AWSToolkit.CommonUI.ToolWindow;
using Amazon.AWSToolkit.Solutions;
using Amazon.AWSToolkit.Util;
using Amazon.AWSToolkit.Telemetry.Model;
using System.ComponentModel.Design;

namespace Amazon.AWSToolkit.Shared
{
    public abstract class ShellProviderServiceGuids
    {
        public const string IAWSToolkitShellProviderIdentifier = "b1c1863d-5a0c-492c-9a43-5d01a087d7cd";
        public const string SAWSToolkitShellProviderIdentifier = "48d7723b-08f7-4c33-8e92-fe5c3c427ccc";
    }

    public enum ShellWindows
    {
        // Toolkit
        AwsExplorer,

        // Visual Studio
        AutoLocals,
        CallStack,
        ClassView,
        CommandWindow,
        DocumentOutline,
        DynamicHelp,
        FindReplace,
        FindResults1,
        FindResults2,
        FindSymbol,
        FindSymbolResults,
        LinkedWindowFrame,
        Locals,
        MacroExplorer,
        MainWindow,
        ObjectBrowser,
        Output,
        Properties,
        ResourceView,
        ServerExplorer,
        SolutionExplorer,
        TaskList,
        Thread,
        Toolbox,
        Watch,
        WebBrowser
    }

    /// <summary>
    /// Interface to be implemented by the shell that is hosting the toolkit.
    /// </summary>
    [Guid(ShellProviderServiceGuids.IAWSToolkitShellProviderIdentifier)]
    [ComVisible(true)]
    public interface IAWSToolkitShellProvider
    {
        /// <summary>
        /// Describes the Shell Host of the Toolkit
        /// </summary>
        IToolkitHostInfo HostInfo { get; }
        ProductEnvironment ProductEnvironment { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="window"></param>
        Task OpenShellWindowAsync(ShellWindows window);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="editorControl"></param>
        void OpenInEditor(IAWSToolkitControl editorControl);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="editorControl"></param>
        Task OpenInEditorAsync(IAWSToolkitControl editorControl);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        void OpenInEditor(string fileName);


        /// <summary>
        /// Opens the given file path in the Windows Explorer
        /// </summary>
        /// <param name="filePath"></param>
        void OpenInWindowsExplorer(string filePath);

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
        /// Hosts a control within a modal dialog.
        /// The modal dialog used is a VS SDK based DialogWindow, which accounts
        /// for modal state within Visual Studio properly.
        /// <seealso cref="https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.platformui.dialogwindow"/>
        /// </summary>
        /// <remarks>
        /// Hosted controls should migrate from <see cref="ShowModal"/> calls to
        /// <see cref="ShowInModalDialogWindow"/> as they are set up with the new Toolkit Theming.
        /// </remarks>
        /// <param name="hostedControl">Control to host in a DialogWindow</param>
        /// <param name="buttons">Buttons to show in the dialog</param>
        /// <returns>true if dialog was accepted (eg: Ok/Yes button), false if cancelled</returns>
        bool ShowInModalDialogWindow(IAWSToolkitControl hostedControl, MessageBoxButton buttons);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="window"></param>
        /// <returns></returns>
        bool ShowModal(Window window);

        /// <summary>
        /// Attempts to display the provided window as a modal to Visual Studio.
        /// An attempt is made to center the window.
        /// </summary>
        Task<bool> ShowModalAsync(Window window);

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
        /// Displays a confirmation message box with Yes/No buttons.
        /// </summary>
        /// <param name="title">Title of message box.</param>
        /// <param name="message">Message for body of message box.</param>
        /// <returns>True if Yes clicked, false otherwise.</returns>
        bool ConfirmWithLinks(string title, string message);

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
        /// Runs a chunk of code on a background thread (not the UI thread).
        /// Intended for use in code that doesn't have access to the JoinableTaskFactory.
        /// </summary>
        T ExecuteOnBackgroundThread<T>(Func<Task<T>> asyncFunc);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        void ExecuteOnUIThread(Action action);

        void ExecuteOnUIThread(Func<Task> asyncFunc);

        T ExecuteOnUIThread<T>(Func<Task<T>> asyncFunc);

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
        /// Attempts to show the output window pane specified by the name
        /// </summary>
        /// <param name="name">Name of output window pane to display.</param>
        /// <returns>True if pane was found and attempted to show, false otherwise.</returns>
        Task<bool> OpenOutputWindowPaneAsync(string name);

        /// <summary>
        /// Adds the specified message to the toolkit shell's logfile
        /// </summary>
        /// <param name="category">Type of message; one of 'debug', 'warn', 'error' or 'info'</param>
        /// <param name="message"></param>
        void AddToLog(string category, string message);

        /// <summary>
        /// Returns a VS CommandID by name.
        /// </summary>
        /// <param name="name">CommandName from vsct file.</param>
        /// <returns>CommandID if found by name, otherwise null.</returns>
        Task<CommandID> QueryCommandAsync(string name);

        /// <summary>
        /// Requests a service implemented in our hosting shell.
        /// </summary>
        /// <typeparam name="T">The interface of the requested service</typeparam>
        /// <returns>The service or null if unknown</returns>
        T QueryShellProviderService<T>() where T : class;

        /// <summary>
        /// Asynchronously requests a service implemented in our hosting shell.
        /// </summary>
        /// <typeparam name="T">The interface of the requested service</typeparam>
        /// <returns>The service or null if unknown</returns>
        Task<T> QueryShellProviderServiceAsync<T>() where T : class;

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
        /// Get the User's current selected Project in the IDE.
        /// </summary>
        /// <returns>The current selected project.</returns>
        Project GetSelectedProject();

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

        /// <summary>
        /// Create an object that abstracts the Visual Studio progress dialog.
        /// Used with long running tasks.
        ///
        /// Caller is responsible for disposing.
        /// </summary>
        Task<IProgressDialog> CreateProgressDialog();

        /// <summary>
        /// Create an object that abstracts the Visual Studio Task Status Center Service.
        /// Used with long running tasks.
        /// </summary>
        Task<ITaskStatusNotifier> CreateTaskStatusNotifier();

        /// <summary>
        /// Get the factory responsible for creating Toolkit dialogs
        /// </summary>
        IDialogFactory GetDialogFactory();

        /// <summary>
        /// Get the factory responsible for creating tool windows
        /// </summary>
        /// <returns></returns>
        IToolWindowFactory GetToolWindowFactory();
    }

    /// <summary>
    /// Marker interface exposing the core AWSToolkit across the rest of our toolkit packages
    /// </summary>
    [Guid(ShellProviderServiceGuids.SAWSToolkitShellProviderIdentifier)]
    public interface SAWSToolkitShellProvider
    {
    }
}
