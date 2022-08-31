using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CommonUI.Dialogs;

using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace AwsToolkit.VsSdk.Common.CommonUI
{
    /// <summary>
    /// Wrapper around the Visual Studio folder browser dialog
    /// </summary>
    public class VsFolderBrowserDialog : IFolderBrowserDialog
    {
        private const int MaxPathLength = 2048;

        private readonly JoinableTaskFactory _joinableTaskFactory;

        public VsFolderBrowserDialog(JoinableTaskFactory joinableTaskFactory)
        {
            _joinableTaskFactory = joinableTaskFactory;
        }

        /// <summary>
        /// Gets the path to the folder selected in the dialog.
        /// Set this prior to calling <see cref="ShowModal"/> in order to specify the initial folder shown in the dialog.
        /// </summary>
        public string FolderPath { get; set; }

        /// <summary>
        /// The dialog's title
        /// </summary>
        public string Title { get; set; }

        public bool ShowModal()
        {
            return _joinableTaskFactory.Run(ShowModalAsync);
        }

        public async Task<bool> ShowModalAsync()
        {
            await _joinableTaskFactory.SwitchToMainThreadAsync();

            var uiShell = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell;
            Assumes.Present(uiShell);

            ErrorHandler.ThrowOnFailure(uiShell.GetDialogOwnerHwnd(out IntPtr dialogOwner));

            var browseInfo = new VSBROWSEINFOW[1];
            browseInfo[0].pwzDirName = IntPtr.Zero;

            try
            {
                browseInfo[0].pwzInitialDir = FolderPath;
                browseInfo[0].lStructSize = (uint) Marshal.SizeOf(typeof(VSBROWSEINFOW));
                browseInfo[0].pwzDlgTitle = Title;
                browseInfo[0].dwFlags = 0;
                browseInfo[0].nMaxDirName = MaxPathLength;
                browseInfo[0].pwzDirName = Marshal.AllocCoTaskMem(MaxPathLength * 2);
                browseInfo[0].hwndOwner = dialogOwner;

                if (ErrorHandler.Succeeded(uiShell.GetDirectoryViaBrowseDlg(browseInfo)))
                {
                    FolderPath = Marshal.PtrToStringAuto(browseInfo[0].pwzDirName);
                    return true;
                }
            }
            finally
            {
                if (browseInfo[0].pwzDirName != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(browseInfo[0].pwzDirName);
                }
            }

            return false;
        }
    }
}
