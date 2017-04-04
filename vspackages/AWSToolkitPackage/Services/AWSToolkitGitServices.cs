using System;
using System.Threading;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Shared;
using log4net;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TeamFoundation.Git.Extensibility;

namespace Amazon.AWSToolkit.VisualStudio.Services
{
    internal class AWSToolkitGitServices : IAWSToolkitGitServices
    {
        private readonly ILog LOGGER = LogManager.GetLogger(typeof(AWSToolkitGitServices));
        readonly IVsStatusbar _statusBar;

        public AWSToolkitGitServices(AWSToolkitPackage hostPackage)
        {
            HostPackage = hostPackage;
            _statusBar = hostPackage.GetVSShellService(typeof(IVsStatusbar)) as IVsStatusbar;
        }

        private AWSToolkitPackage HostPackage { get; set; }

        public async void Clone(string repositoryUrl, string destinationFolder, AccountViewModel account)
        {
            var recurseSubmodules = true;

            try
            {
#if VS2017_OR_LATER
                var gitExt = HostPackage.GetVSShellService(typeof(IGitActionsExt)) as IGitActionsExt;
                var progress = new Progress<Microsoft.VisualStudio.Shell.ServiceProgressData>();

                await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    progress.ProgressChanged += (s, e) => _statusBar.SetText(e.ProgressText);
                    await gitExt.CloneAsync(repositoryUrl, destinationFolder, recurseSubmodules, default(CancellationToken), progress);
                });
#else
            var gitExt = HostPackage.GetVSShellService(typeof(IGitRepositoriesExt)) as IGitRepositoriesExt;
            gitExt.Clone(repositoryUrl, destinationFolder, recurseSubmodules ? CloneOptions.RecurseSubmodule : CloneOptions.None);

            // The operation will have completed when CanClone goes false and then true again.
            await gitExt.WhenAnyValue(x => x.CanClone).Where(x => !x).Take(1);
            await gitExt.WhenAnyValue(x => x.CanClone).Where(x => x).Take(1);
#endif
            }
            catch (Exception e)
            {
                // todo: need to push this to UI message box too
                var msg = string.Format("Failed to clone repository {0} using Team Explorer. Exception message {1}.",
                    repositoryUrl,
                    e.Message);
                LOGGER.Error(msg, e);
            }
        }
    }
}
