using System;
using System.Drawing;
using System.Diagnostics;
using System.ComponentModel.Design;
using System.Windows.Forms;
using System.Resources;
using System.Runtime.InteropServices;
using System.Collections.Generic;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;

using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;


namespace Amazon.AWSToolkit.VisualStudio.HostedEditor
{
    public class NavigatorVsUIHierarchy : IVsUIHierarchy
    {
        private static ServiceProvider _serviceProvider;

        public NavigatorVsUIHierarchy()
        {
        }

        public int AdviseHierarchyEvents(IVsHierarchyEvents pEventSink, out uint pdwCookie)
        {
            pdwCookie = 0;
            return VSConstants.S_OK;
        }

        public int Close()
        {
            return VSConstants.S_OK;
        }

        public int ExecCommand(uint itemid, ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            return VSConstants.S_OK;
        }


        public int GetCanonicalName(uint itemid, out string pbstrName)
        {
            pbstrName = itemid.ToString();
            if (pbstrName != null)
                return VSConstants.S_OK;

            return VSConstants.E_INVALIDARG;
        }

        public int GetGuidProperty(uint itemid, int propid, out Guid pguid)
        {
            string s = string.Format("GetGuidProperty for itemID({0})", itemid.ToString());
            Trace.WriteLine(s);
            pguid = Guid.Empty;
            return VSConstants.DISP_E_MEMBERNOTFOUND;
        }

        public int GetNestedHierarchy(uint itemid, ref Guid iidHierarchyNested, out IntPtr ppHierarchyNested, out uint pitemidNested)
        {
            ppHierarchyNested = IntPtr.Zero;
            pitemidNested = 0;
            return VSConstants.E_NOTIMPL;
        }


        public int GetProperty(uint itemid, int propid, out object pvar)
        {
            // GetProperty is called many many times for this particular property
            if (propid != (int)__VSHPROPID.VSHPROPID_ParentHierarchy)
            {
                string s = string.Format("GetProperty for itemId ({0}) called with propid = {1}", itemid.ToString(), propid.ToString());
                Trace.WriteLine(s);
            }

            pvar = null;
            switch (propid)
            {
                case (int)__VSHPROPID.VSHPROPID_Parent:
                    if (itemid == VSConstants.VSITEMID_ROOT)
                        pvar = VSConstants.VSITEMID_NIL;
                    else
                        pvar = VSConstants.VSITEMID_ROOT;
                    break;

                case (int)__VSHPROPID.VSHPROPID_FirstChild:
                    if (itemid == VSConstants.VSITEMID_ROOT)
                        pvar = 1;
                    else
                        pvar = VSConstants.VSITEMID_NIL;
                    break;

                case (int)__VSHPROPID.VSHPROPID_NextSibling:
                    pvar = VSConstants.VSITEMID_NIL;
                    break;

                case (int)__VSHPROPID.VSHPROPID_Expandable:
                    if (itemid == VSConstants.VSITEMID_ROOT)
                        pvar = true;
                    else
                        pvar = false;
                    break;



                //case (int)__VSHPROPID.VSHPROPID_Caption:
                //case (int)__VSHPROPID.VSHPROPID_SaveName:
                //    pvar = "MyCaption";
                //    break;

                case (int)__VSHPROPID.VSHPROPID_ShowOnlyItemCaption:
                    pvar = true;
                    break;

                case (int)__VSHPROPID.VSHPROPID_ParentHierarchy:
                    //if (itemid == childItem1._Id || itemid == childItem2._Id)
                    //    pvar = this as IVsHierarchy;
                    break;
            }

            if (pvar != null)
                return VSConstants.S_OK;

            return VSConstants.DISP_E_MEMBERNOTFOUND;
        }

        public int GetSite(out IOleServiceProvider ppSP)
        {
            ppSP = _serviceProvider.GetService(typeof(IOleServiceProvider)) as IOleServiceProvider;
            return VSConstants.S_OK;
        }

        public int ParseCanonicalName(string pszName, out uint pitemid)
        {
            pitemid = 0;
            return VSConstants.E_NOTIMPL;
        }

        public int QueryClose(out int pfCanClose)
        {
            pfCanClose = 1;
            return VSConstants.S_OK;
        }

        public int QueryStatusCommand(uint itemid, ref Guid pguidCmdGroup, uint cCmds, Microsoft.VisualStudio.OLE.Interop.OLECMD[] prgCmds, IntPtr pCmdText)
        {
            return (int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_UNKNOWNGROUP;
        }

        public int SetGuidProperty(uint itemid, int propid, ref Guid rguid)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int SetProperty(uint itemid, int propid, object var)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int SetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider psp)
        {
            _serviceProvider = new ServiceProvider(psp, true);
            return VSConstants.S_OK;
        }

        public int UnadviseHierarchyEvents(uint dwCookie)
        {
            return VSConstants.S_OK;
        }

        public int Unused0()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public int Unused1()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public int Unused2()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public int Unused3()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public int Unused4()
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }
}
