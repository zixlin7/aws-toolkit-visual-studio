using System;
using System.Threading.Tasks;
using System.Windows;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.Notifications;
using Amazon.AWSToolkit.CommonUI.Notifications.Progress;
using Amazon.AWSToolkit.CommonUI.ToolWindow;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Util;

using Project = Amazon.AWSToolkit.Solutions.Project;

namespace Amazon.AWSToolkit.Tests.Common.Context
{
    /// <summary>
    /// NoOp implementation of the ToolkitShellProvider to make creation of test-doubles easier
    /// </summary>
    public class NoOpToolkitShellProvider : IAWSToolkitShellProvider
    {
        public IToolkitHostInfo HostInfo { get; }

        public void OpenShellWindow(ShellWindows window)
        {

        }

        public void OpenInEditor(IAWSToolkitControl editorControl)
        {

        }

        public Task OpenInEditorAsync(IAWSToolkitControl editorControl)
        {
            return null;
        }

        public void OpenInEditor(string fileName)
        {

        }

        public IntPtr GetParentWindowHandle()
        {
            return IntPtr.Zero;
        }

        public bool ShowModal(IAWSToolkitControl hostedControl)
        {
            return false;
        }

        public bool ShowModalFrameless(IAWSToolkitControl hostedControl)
        {
            return false;
        }

        public virtual void ShowError(string message)
        {

        }

        public virtual void ShowError(string title, string message)
        {

        }

        public void ShowErrorWithLinks(string title, string message)
        {

        }

        public void ShowMessage(string title, string message)
        {

        }

        public bool Confirm(string title, string message)
        {
            return false;
        }

        public void UpdateStatus(string status)
        {

        }

        public void ExecuteOnUIThread(Action action)
        {

        }

        public void ExecuteOnUIThread(Func<Task> asyncFunc)
        {
        }

        public T ExecuteOnUIThread<T>(Func<Task<T>> asyncFunc)
        {
            return default(T);
        }

        public void BeginExecuteOnUIThread(Action action)
        {

        }

        public void OutputToHostConsole(string message)
        {

        }

        public virtual void OutputToHostConsole(string message, bool forceVisible)
        {

        }

        public void AddToLog(string category, string message)
        {

        }

        public T QueryShellProviderService<T>() where T : class
        {
            return null;
        }

        public object QueryAWSToolkitPluginService(Type pluginServiceType)
        {
            return null;
        }

        public object QueryAWSToolkitPluginService(string pluginServiceType)
        {
            return null;
        }

        public virtual void OpenInBrowser(string url, bool preferInternalBrowser)
        {

        }

        public virtual Project GetSelectedProject()
        {
            return null;
        }

        public void CloseEditor(IAWSToolkitControl editorControl)
        {

        }

        public void CloseEditor(string fileName)
        {

        }

        public bool Confirm(string title, string message, MessageBoxButton buttons)
        {
            return false;
        }

        public bool ShowModal(Window window)
        {
            return false;
        }

        public virtual bool ShowInModalDialogWindow(IAWSToolkitControl hostedControl, MessageBoxButton buttons)
        {
            return false;
        }

        public bool ShowModal(IAWSToolkitControl hostedControl, MessageBoxButton buttons)
        {
            return false;
        }

        public Task<IProgressDialog> CreateProgressDialog()
        {
            return null;
        }

        public Task<ITaskStatusNotifier> CreateTaskStatusNotifier()
        {
            return null;
        }

        public IDialogFactory GetDialogFactory()
        {
            throw new NotImplementedException();
        }

        public IToolWindowFactory GetToolWindowFactory()
        {
            throw new NotImplementedException();
        }
    }
}
