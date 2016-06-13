using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Amazon.AWSToolkit.VisualStudio.Shared.ServiceInterfaces;

namespace Amazon.AWSToolkit.VisualStudio
{
    public class NetCoreProjectSupport : Package, INetCoreProjectSupport
    {
        private const string _dotNetCoreWebCapability = "DotNetCoreWeb";

        public bool IsNetCoreWebProject(IntPtr hierarchyPtr)
        {
            if (hierarchyPtr == null)
                throw new ArgumentNullException("hierarchyPtr");

            var projHier = Marshal.GetTypedObjectForIUnknown(hierarchyPtr, typeof(IVsHierarchy)) as IVsHierarchy;
            return projHier != null && projHier.IsCapabilityMatch(_dotNetCoreWebCapability);
        }
    }
}
