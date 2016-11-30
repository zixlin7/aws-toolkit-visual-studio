using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;


using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Shared;

namespace Amazon.AWSToolkit.VisualStudio.HostedEditor
{
    [Guid(GuidList.HostedEditorFactoryGuidString)]
    public sealed class HostedEditorFactory : IVsEditorFactory, IDisposable
    {
        private AWSToolkitPackage editorPackage;
        private ServiceProvider vsServiceProvider;


        public HostedEditorFactory(AWSToolkitPackage package)
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering {0} constructor", this.ToString()));

            this.editorPackage = package;
        }

        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public int CreateEditorInstance(
                        uint grfCreateDoc,
                        string pszMkDocument,
                        string pszPhysicalView,
                        IVsHierarchy pvHier,
                        uint itemid,
                        System.IntPtr punkDocDataExisting,
                        out System.IntPtr ppunkDocView,
                        out System.IntPtr ppunkDocData,
                        out string pbstrEditorCaption,
                        out Guid pguidCmdUI,
                        out int pgrfCDW)
        {
            
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture,
              "Entering {0} CreateEditorInstance()", ToString()));

            // --- Initialize to null
            ppunkDocView = IntPtr.Zero;
            ppunkDocData = IntPtr.Zero;
            pguidCmdUI = GetType().GUID;
            pgrfCDW = 0;
            pbstrEditorCaption = null;

            // --- Validate inputs
            if ((grfCreateDoc & (VSConstants.CEF_OPENFILE | VSConstants.CEF_SILENT)) == 0)
            {
                return VSConstants.E_INVALIDARG;
            }
            if (punkDocDataExisting != IntPtr.Zero)
            {
                return VSConstants.VS_E_INCOMPATIBLEDOCDATA;
            }

            // --- Create the Document (editor)
            object newDocView;
            object newDocData;
            createViewAndData(itemid, out newDocView, out newDocData);
            ppunkDocView = Marshal.GetIUnknownForObject(newDocView);
            ppunkDocData = Marshal.GetIUnknownForObject(newDocData);
            pbstrEditorCaption = "";

            return VSConstants.S_OK;
        }

        private void createViewAndData(uint controlId, out object docView, out object docData)
        {
            IAWSToolkitControl control = this.editorPackage.PopControl(controlId);
            docView = new HostedEditorPane(control);
            docData = docView;
        }


        public void Dispose()
        {
            if (vsServiceProvider != null)
            {
                vsServiceProvider.Dispose();
            }
        }

        public int SetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider psp)
        {
            vsServiceProvider = new ServiceProvider(psp);
            return VSConstants.S_OK;
        }

        public object GetService(Type serviceType)
        {
            return vsServiceProvider.GetService(serviceType);
        }

        public int MapLogicalView(ref Guid rguidLogicalView, out string pbstrPhysicalView)
        {
            pbstrPhysicalView = null;    // initialize out parameter

            // we support only a single physical view
            if (VSConstants.LOGVIEWID_Primary == rguidLogicalView)
                return VSConstants.S_OK;        // primary view uses NULL as pbstrPhysicalView
            else
                return VSConstants.E_NOTIMPL;   // you must return E_NOTIMPL for any unrecognized rguidLogicalView values
        }

        public int Close()
        {
            return VSConstants.S_OK;
        }


    }
}
