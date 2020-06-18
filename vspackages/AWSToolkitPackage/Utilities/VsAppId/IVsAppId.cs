using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;

namespace Amazon.AWSToolkit.VisualStudio.Utilities.VsAppId
{
    /// <summary>
    /// Recommended interface for querying VS version information.
    /// Additional Reference: https://github.com/dotnet/project-system/blob/master/src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Interop/IVsAppId.cs
    /// </summary>
    [Guid("1EAA526A-0898-11d3-B868-00C04F79F802")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsAppId
    {
        [PreserveSig]
        int SetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider pSP);

        [PreserveSig]
        int GetProperty(int propid, [MarshalAs(UnmanagedType.Struct)] out object pvar);

        [PreserveSig]
        int SetProperty(int propid, [MarshalAs(UnmanagedType.Struct)] object var);

        [PreserveSig]
        int GetGuidProperty(int propid, out Guid guid);

        [PreserveSig]
        int SetGuidProperty(int propid, ref Guid rguid);

        [PreserveSig]
        int Initialize();
    }

    internal static class VSAPropID
    {
        public static readonly int VSAPROPID_ProductDisplayVersion = -8641;
        public static readonly int VSAPROPID_AppShortBrandName = -8603;
    }

    internal static class IVsAppIdExtensionMethods
    {
        internal static bool TryGetProperty(this IVsAppId vsAppId, int propid, out object property)
        {
            property = null;

            if (vsAppId == null) {
                return false;
            }

            var hr = vsAppId.GetProperty(propid, out property);
            return hr == VSConstants.S_OK;
        }
    }
}