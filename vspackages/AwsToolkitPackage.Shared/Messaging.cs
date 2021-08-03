using System;
using System.Windows;
using System.Diagnostics;

using Microsoft.WindowsAPICodePack.Dialogs;

namespace Amazon.AWSToolkit.VisualStudio
{
    /// <summary>
    /// Utility helpers for displaying messages (shared across VS versions) to the end-user.
    /// These should be called under the control of the correct dispatcher.
    /// </summary>
    public static class Messaging
    {
        /// <summary>
        /// Display message stating that msdeploy.exe is not installed, together with a link
        /// to where the user can find it.
        /// </summary>
        /// <returns>True to retry</returns>
        public static bool DisplayMSDeployRequiredMessage(Window parentWindow)
        {
            if (TaskDialog.IsPlatformSupported)
            {
                TaskDialog td = new TaskDialog();
                td.HyperlinksEnabled = true;
                td.Caption = "WebDeploy Tool Not Found";
                td.InstructionText = "The web deployment tool 'msdeploy.exe' is required for deployment.";
                td.Text = "This tool could not be located on your machine. Please download and install from <a href=\"http://www.iis.net/download/WebDeploy\">www.iis.net/download/WebDeploy</a> and try again.";
                td.Icon = TaskDialogStandardIcon.Error;
                td.StandardButtons = TaskDialogStandardButtons.Retry | TaskDialogStandardButtons.Cancel;
                td.HyperlinkClick += new EventHandler<TaskDialogHyperlinkClickedEventArgs>(td_HyperlinkClick);
                return td.Show() == TaskDialogResult.Retry;
            }
            else
            {
                string msg = string.Format(Resources.msgMSDeployNotFound, "http://www.iis.net/download/WebDeploy");
                MessageBox.Show(parentWindow, msg, Resources.msgtitleMSDeployNotFound, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        static void td_HyperlinkClick(object sender, TaskDialogHyperlinkClickedEventArgs e)
        {
            Process.Start(e.LinkText);
        }

        public static void ShowErrorWithLinks(Window parentWindow, string title, string message)
        {
            if (TaskDialog.IsPlatformSupported)
            {
                TaskDialog td = new TaskDialog();
                td.HyperlinksEnabled = true;
                td.Caption = title;
                td.Text = message;
                td.Icon = TaskDialogStandardIcon.Error;
                td.StandardButtons = TaskDialogStandardButtons.Ok;
                td.HyperlinkClick += new EventHandler<TaskDialogHyperlinkClickedEventArgs>(td_HyperlinkClick);
                td.Show();
            }
            else
            {
                MessageBox.Show(parentWindow, message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
