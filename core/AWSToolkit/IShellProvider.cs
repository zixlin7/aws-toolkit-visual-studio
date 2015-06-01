using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit
{
    public enum ShellWindows
    {
        Explorer,
        Output
    }

    public interface IShellProvider
    {
        string ShellName { get; }

        void OpenShellWindow(ShellWindows window);

        void OpenInEditor(IAWSControl editorControl);
        void OpenInEditor(string fileName);

        bool ShowModal(IAWSControl hostedControl);
        bool ShowModal(IAWSControl hostedControl, MessageBoxButton buttons);
        bool ShowModalFrameless(IAWSControl hostedControl);

        bool ShowModal(Window window);

        void ShowError(string message);
        void ShowError(string title, string message);
        void ShowErrorWithLinks(string title, string message);

        void ShowMessage(string title, string message);

        bool Confirm(string title, string message);
        bool Confirm(string title, string message, MessageBoxButton buttons);

        void UpdateStatus(string status);

        Dispatcher ShellDispatcher {get;}

        void OutputToHostConsole(string message);
        void OutputToHostConsole(string message, bool forceVisible);

        T QueryShellProverService<T>() where T : class;
    }
}
