/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using VSLangProj;

namespace Microsoft.VisualStudio.Project.Automation
{
	/// <summary>
	/// Represents a project reference of the solution
	/// </summary>
	[SuppressMessage("Microsoft.Interoperability", "CA1405:ComVisibleTypeBaseTypesShouldBeComVisible")]
	[ComVisible(true)]
	public class OAProjectReference : OAReferenceBase<ProjectReferenceNode>
	{
		public OAProjectReference(ProjectReferenceNode projectReference) :
			base(projectReference)
		{
		}

		#region Reference override
		public override string Culture => string.Empty;

        public override string Name => BaseReferenceNode.ReferencedProjectName;

        public override string Identity => BaseReferenceNode.Caption;

        public override string Path => BaseReferenceNode.ReferencedProjectOutputPath;

        public override EnvDTE.Project SourceProject
		{
			get
			{
				if(Guid.Empty == BaseReferenceNode.ReferencedProjectGuid)
				{
					return null;
				}
				IVsHierarchy hierarchy = VsShellUtilities.GetHierarchy(BaseReferenceNode.ProjectMgr.Site, BaseReferenceNode.ReferencedProjectGuid);
				if(null == hierarchy)
				{
					return null;
				}
				object extObject;
				if(Microsoft.VisualStudio.ErrorHandler.Succeeded(
						hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out extObject)))
				{
					return extObject as EnvDTE.Project;
				}
				return null;
			}
		}

        // TODO: Write the code that finds out the type of the output of the source project.
        public override prjReferenceType Type => prjReferenceType.prjReferenceTypeAssembly;

        public override string Version => string.Empty;

        #endregion
	}
}
