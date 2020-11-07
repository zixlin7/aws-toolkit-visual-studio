using System;
using System.Windows;
using System.Windows.Interop;

using Amazon.Runtime.Internal.Settings;

namespace Amazon.AWSToolkit.CommonUI
{
    public static class DirectoryBrowserDlgHelper
    {
        public static string ChooseDirectory(string description)
        {
            var parentHandle = ToolkitFactory.Instance.ShellProvider.GetParentWindowHandle();
            var windowWrapper = new WindowWrapper(parentHandle);

            return ChooseDirectory(windowWrapper, description);
        }

        public static string ChooseDirectory(UIElement element, string description)
        {
            HwndSource source = PresentationSource.FromVisual(element) as HwndSource;
            var windowWrapper = new WindowWrapper(source?.Handle ?? IntPtr.Zero);

            return ChooseDirectory(windowWrapper, description);
        }

        private static string ChooseDirectory(System.Windows.Forms.IWin32Window owner, string description)
        {
            var settings = PersistenceManager.Instance.GetSettings(ToolkitSettingsConstants.RecentUsages);
            var os = settings["DirectoryBrowserDlgHelper"];
            
            var dlg = new System.Windows.Forms.FolderBrowserDialog();
            dlg.SelectedPath = os["LastDirectory"];

            dlg.Description = description;
            System.Windows.Forms.DialogResult result = dlg.ShowDialog(owner);
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                os["LastDirectory"] = dlg.SelectedPath;
                PersistenceManager.Instance.SaveSettings(ToolkitSettingsConstants.RecentUsages, settings);

                return dlg.SelectedPath;
            }

            return string.Empty;
        }


        class WindowWrapper : System.Windows.Forms.IWin32Window
        {
            IntPtr _handle;
            public WindowWrapper(IntPtr handle)
            {
                this._handle = handle;
            }

            IntPtr System.Windows.Forms.IWin32Window.Handle => this._handle;
        }
    }
}
