﻿/***************************************************************************

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
using EnvDTE;
using VSLangProj;

namespace Microsoft.VisualStudio.Project.Automation
{
	/// <summary>
	/// Represents an automation friendly version of a language-specific project.
	/// </summary>
	[SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "OAVS")]
	[ComVisible(true)]
	public class OAVSProject : VSProject
	{
		#region fields
		private ProjectNode project;
		private OAVSProjectEvents events;
		#endregion

		#region ctors
		public OAVSProject(ProjectNode project)
		{
			this.project = project;
		}
		#endregion

		#region VSProject Members

		public virtual ProjectItem AddWebReference(string bstrUrl)
		{
			throw new NotImplementedException();
		}

		public virtual BuildManager BuildManager => new OABuildManager(this.project);

        public virtual void CopyProject(string bstrDestFolder, string bstrDestUNCPath, prjCopyProjectOption copyProjectOption, string bstrUsername, string bstrPassword)
		{
			throw new NotImplementedException();
		}

		public virtual ProjectItem CreateWebReferencesFolder()
		{
			throw new NotImplementedException();
		}

		public virtual DTE DTE => (EnvDTE.DTE)this.project.Site.GetService(typeof(EnvDTE.DTE));

        public virtual VSProjectEvents Events
		{
			get
			{
				if(events == null)
					events = new OAVSProjectEvents(this);
				return events;
			}
		}

		public virtual void Exec(prjExecCommand command, int bSuppressUI, object varIn, out object pVarOut)
		{
			throw new NotImplementedException(); ;
		}

		public virtual void GenerateKeyPairFiles(string strPublicPrivateFile, string strPublicOnlyFile)
		{
			throw new NotImplementedException(); ;
		}

		public virtual string GetUniqueFilename(object pDispatch, string bstrRoot, string bstrDesiredExt)
		{
			throw new NotImplementedException(); ;
		}

		public virtual Imports Imports => throw new NotImplementedException();

        public virtual EnvDTE.Project Project => this.project.GetAutomationObject() as EnvDTE.Project;

        public virtual References References
		{
			get
			{
				ReferenceContainerNode references = project.GetReferenceContainer() as ReferenceContainerNode;
				if(null == references)
				{
					return null;
				}
				return references.Object as References;
			}
		}

		public virtual void Refresh()
		{
			throw new NotImplementedException();
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
		public virtual string TemplatePath => throw new NotImplementedException();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
		public virtual ProjectItem WebReferencesFolder => throw new NotImplementedException();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
		public virtual bool WorkOffline
		{
			get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

		#endregion
	}

	/// <summary>
	/// Provides access to language-specific project events
	/// </summary>
	[SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "OAVS")]
	[ComVisible(true)]
	public class OAVSProjectEvents : VSProjectEvents
	{
		#region fields
		private OAVSProject vsProject;
		#endregion

		#region ctors
		public OAVSProjectEvents(OAVSProject vsProject)
		{
			this.vsProject = vsProject;
		}
		#endregion

		#region VSProjectEvents Members

		public virtual BuildManagerEvents BuildManagerEvents => vsProject.BuildManager as BuildManagerEvents;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
		public virtual ImportsEvents ImportsEvents => throw new NotImplementedException();

        public virtual ReferencesEvents ReferencesEvents => vsProject.References as ReferencesEvents;

        #endregion
	}

}
