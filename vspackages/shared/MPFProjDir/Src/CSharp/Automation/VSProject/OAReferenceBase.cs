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
using VSLangProj;

namespace Microsoft.VisualStudio.Project.Automation
{
	/// <summary>
	/// Represents the automation equivalent of ReferenceNode
	/// </summary>
	/// <typeparam name="RefType"></typeparam>
	[SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "T")]
	[ComVisible(true)]
	public abstract class OAReferenceBase<RefType> : Reference
		where RefType : ReferenceNode
	{
		#region fields
		private RefType referenceNode;
		#endregion

		#region ctors
		protected OAReferenceBase(RefType referenceNode)
		{
			this.referenceNode = referenceNode;
		}
		#endregion

		#region properties
		protected RefType BaseReferenceNode => referenceNode;

        #endregion

		#region Reference Members
		public virtual int BuildNumber => 0;

        public virtual References Collection => BaseReferenceNode.Parent.Object as References;

        public virtual EnvDTE.Project ContainingProject => BaseReferenceNode.ProjectMgr.GetAutomationObject() as EnvDTE.Project;

        public virtual bool CopyLocal
		{
			get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

		public virtual string Culture => throw new NotImplementedException();

        public virtual EnvDTE.DTE DTE => BaseReferenceNode.ProjectMgr.Site.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;

        public virtual string Description => this.Name;

        public virtual string ExtenderCATID => throw new NotImplementedException();

        public virtual object ExtenderNames => throw new NotImplementedException();

        public virtual string Identity => throw new NotImplementedException();

        public virtual int MajorVersion => 0;

        public virtual int MinorVersion => 0;

        public virtual string Name => throw new NotImplementedException();

        public virtual string Path => BaseReferenceNode.Url;

        public virtual string PublicKeyToken => throw new NotImplementedException();

        public virtual void Remove()
		{
			BaseReferenceNode.Remove(false);
		}

		public virtual int RevisionNumber => 0;

        public virtual EnvDTE.Project SourceProject => null;

        public virtual bool StrongName => false;

        public virtual prjReferenceType Type => throw new NotImplementedException();

        public virtual string Version => new Version().ToString();

        public virtual object get_Extender(string ExtenderName)
		{
			throw new NotImplementedException();
		}
		#endregion
	}
}
