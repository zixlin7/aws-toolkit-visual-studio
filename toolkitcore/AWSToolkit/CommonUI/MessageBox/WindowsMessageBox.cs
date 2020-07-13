using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace Amazon.AWSToolkit.CommonUI.MessageBox
{
    /// <summary>
    /// Convenience wrapper around the Win32 MessageBox function.
    /// The WPF MessageBox requires a Window owner, however the VS IDE only provides a
    /// handle, so we have to drop to the core MessageBox function.
    ///
    /// This also helps keep the message box parented to the IDE in situations where
    /// a thread pool or background thread attempts to display a message box.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-messagebox"/>
    public static class WindowsMessageBox
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int MessageBox(HandleRef hWnd, string text, string caption, uint type);

        /// <summary>
        /// Shows the windows system message box
        /// </summary>
        /// <seealso cref="https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-messagebox"/>
        /// <param name="ownerWindowHwnd">The Modal Message Box owner</param>
        /// <param name="title">Message title</param>
        /// <param name="message">Message text</param>
        /// <param name="buttons">Which buttons to show in the message box</param>
        /// <param name="image">Icon to display</param>
        /// <param name="selectedButton">The initially selected button</param>
        /// <returns>Message Box result</returns>
        public static MessageBoxResult Show(IntPtr ownerWindowHwnd,
            string title, string message,
            MessageBoxButton buttons, MessageBoxImage image,
            MessageBoxResult selectedButton)
        {
            var handleRef = new HandleRef(null, ownerWindowHwnd);

            uint type = (uint) buttons | (uint) image | (uint) GetDefaultButton(selectedButton, buttons);

            return (MessageBoxResult) MessageBox(handleRef, message, title, type);
        }

        /// <summary>
        /// Determines what the initially selected button should be in a Message Box
        /// </summary>
        /// <example>
        /// Resolves [Yes] in the [Yes|No] button set to the first button
        /// </example>
        /// <param name="defaultButton">Button that should be made default (expressed as a result)</param>
        /// <param name="buttonSet">The button set to be shown</param>
        /// <returns>Initially selected button</returns>
        private static MessageBoxDefaultButton GetDefaultButton(MessageBoxResult defaultButton, MessageBoxButton buttonSet)
        {
            if (defaultButton == MessageBoxResult.None)
            {
                return MessageBoxDefaultButton.DefaultButton;
            }

            switch (buttonSet)
            {
                case MessageBoxButton.OK:
                    return MessageBoxDefaultButton.Button1;
                case MessageBoxButton.OKCancel:
                    return defaultButton == MessageBoxResult.Cancel ? MessageBoxDefaultButton.Button2 : MessageBoxDefaultButton.Button1;
                case MessageBoxButton.YesNoCancel:
                    switch (defaultButton)
                    {
                        case MessageBoxResult.No:
                            return MessageBoxDefaultButton.Button2;
                        case MessageBoxResult.Cancel:
                            return MessageBoxDefaultButton.Button3;
                        case MessageBoxResult.Yes:
                        default:
                            return MessageBoxDefaultButton.Button1;
                    }
                case MessageBoxButton.YesNo:
                    return defaultButton == MessageBoxResult.No ? MessageBoxDefaultButton.Button2 : MessageBoxDefaultButton.Button1;
                default:
                    return MessageBoxDefaultButton.DefaultButton;
            }
        }
    }
}