using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.VisualStudio.Shared.ServiceInterfaces
{
    public interface INetCoreProjectSupport
    {
        bool IsNetCoreWebProject(IntPtr hierarchyPtr);
    }
}
