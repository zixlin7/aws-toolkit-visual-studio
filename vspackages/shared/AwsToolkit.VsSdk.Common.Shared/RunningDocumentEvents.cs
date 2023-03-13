using System;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Amazon.AwsToolkit.VsSdk.Common
{
    /// <summary>
    /// References: https://github.com/VsixCommunity/Community.VisualStudio.Toolkit/blob/master/src/toolkit/Community.VisualStudio.Toolkit.Shared/Documents/DocumentEvents.cs
    /// Events related to the editor documents.
    /// </summary>
    public class RunningDocumentEvents : IVsRunningDocTableEvents, IDisposable
    {
        private readonly RunningDocumentTable _rdt;
        private readonly uint _cookie;

        public RunningDocumentEvents()
        {
            _rdt = new RunningDocumentTable();
            _cookie = _rdt.Advise(this);
        }

        /// <summary>
        /// Fires after the document was opened in the editor.
        /// </summary>
        /// <remarks>
        /// The event is called for documents in the document well but also
        /// for project files and may also be called for solution files.<br/>
        /// </remarks>
        public event Action<string> Opened;

        int IVsRunningDocTableEvents.OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType,
            uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            // Please note that this event is called multiple times when a document
            // is opened for editing.
            // This code tries to only call the Open Event once
            if (dwEditLocksRemaining == 1 && dwReadLocksRemaining == 0)
            {
                if (Opened != null)
                {
                    string file = _rdt.GetDocumentInfo(docCookie).Moniker;
                    Opened.Invoke(file);
                }
            }

            return VSConstants.S_OK;
        }

        int IVsRunningDocTableEvents.OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType,
            uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        int IVsRunningDocTableEvents.OnAfterSave(uint docCookie)
        {
            return VSConstants.S_OK;
        }

        int IVsRunningDocTableEvents.OnAfterAttributeChange(uint docCookie, uint grfAttribs)
        {
            return VSConstants.S_OK;
        }

        int IVsRunningDocTableEvents.OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        int IVsRunningDocTableEvents.OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        public void Dispose()
        {
            _rdt.Unadvise(_cookie);
        }
    }
}
