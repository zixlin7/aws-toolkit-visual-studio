using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

using Amazon.AWSToolkit;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.Tests
{
    public class TestShellProvider : IShellProvider
    {
        public string ShellName 
        { 
            get {return "test shell"; }
        }

        public void OpenShellWindow(ShellWindows window)
        {
        }

        public void OpenInEditor(IAWSControl editorControl)
        {
        }

        public void OpenInEditor(string fileName)
        {
        }

        public bool ShowModal(IAWSControl hostedControl)
        {
            return true;
        }

        public bool ShowModal(IAWSControl hostedControl, MessageBoxButton buttons)
        {
            return true;
        }

        public bool ShowModalFrameless(IAWSControl hostedControl)
        {
            return true;
        }

        public bool ShowModal(Window window)
        {
            return true;
        }

        public void ShowError(string message)
        {
            Console.WriteLine("Error: " + message);
            return;
        }

        public void ShowError(string title, string message)
        {
            Console.WriteLine("Error: " + message);
        }

        public void ShowErrorWithLinks(string title, string message)
        {
            Console.WriteLine("Error: " + message);
        }

        public void ShowMessage(string title, string message)
        {
            Console.WriteLine("Message: " + message);
        }

        public void UpdateStatus(string status)
        {
        }

        public bool Confirm(string title, string message)
		{
			return Confirm(title, message, MessageBoxButton.YesNo);
		}
		
        public bool Confirm(string title, string message, MessageBoxButton buttons)
        {
            return true;
        }

        public Dispatcher ShellDispatcher 
        {
            get
            {
                return Dispatcher.CurrentDispatcher;
            }
        }

        public void OutputToHostConsole(string message)
        {
            Console.WriteLine("OutputToHostConsole: " + message);
        }

        public void OutputToHostConsole(string message, bool forceVisible)
        {
            Console.WriteLine("OutputToHostConsole: " + message);
        }

        public T QueryShellProviderService<T>() where T : class
        {
            return this as T;
        }

    }
}
