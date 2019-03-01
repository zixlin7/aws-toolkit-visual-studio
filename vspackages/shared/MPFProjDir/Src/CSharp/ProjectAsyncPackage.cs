/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.Project
{
	/// <summary>
	/// Defines abstract package.
	/// </summary>
	[ComVisible(true)]
	[CLSCompliant(false)]
	public abstract class ProjectAsyncPackage : Microsoft.VisualStudio.Shell.AsyncPackage, IProjectPackage
    {
		#region fields
		/// <summary>
		/// This is the place to register all the solution listeners.
		/// </summary>
		private List<SolutionListener> solutionListeners = new List<SolutionListener>();
		#endregion

		#region properties
		/// <summary>
		/// Add your listener to this list. They should be added in the overridden Initialize befaore calling the base.
		/// </summary>
		public IList<SolutionListener> SolutionListeners
		{
			get
			{
				return this.solutionListeners;
			}
		}

		public abstract string ProductUserContext { get; }

		#endregion

		#region methods
		protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<Shell.ServiceProgressData> progress)
		{
			await base.InitializeAsync(cancellationToken, progress);

            // Subscribe to the solution events
            var package = this as Microsoft.VisualStudio.Shell.Package;
            await AddSolutionListnerAsync(new SolutionListenerForProjectReferenceUpdate((IAsyncServiceProvider)package));
            await AddSolutionListnerAsync(new SolutionListenerForProjectOpen((IAsyncServiceProvider)package));
            await AddSolutionListnerAsync(new SolutionListenerForBuildDependencyUpdate((IAsyncServiceProvider)package));
            await AddSolutionListnerAsync(new SolutionListenerForProjectEvents((IAsyncServiceProvider)package));

            foreach (SolutionListener solutionListener in this.solutionListeners)
			{
				solutionListener.Init();
			}
		}

        private async System.Threading.Tasks.Task AddSolutionListnerAsync(SolutionListener listener)
        {
            await listener.CollectServicesAsync();
            this.solutionListeners.Add(listener);
        }

		protected override void Dispose(bool disposing)
		{
			// Unadvise solution listeners.
			try
			{
				if(disposing)
				{
					foreach(SolutionListener solutionListener in this.solutionListeners)
					{
						solutionListener.Dispose();
					}

                    // Dispose the UIThread singleton.
                    UIThread.Instance.Dispose();                   
				}
			}
			finally
			{

				base.Dispose(disposing);
			}
		}
		#endregion
	}
}
