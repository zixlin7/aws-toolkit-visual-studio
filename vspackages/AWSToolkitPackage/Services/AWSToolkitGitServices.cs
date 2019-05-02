using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CodeCommit.Interface;
using Amazon.AWSToolkit.CodeCommit.Interface.Model;
using Amazon.AWSToolkit.CodeCommit.Model;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Util;
using Amazon.AWSToolkit.CodeCommitTeamExplorer.CredentialManagement;
using log4net;
using Microsoft.VisualStudio.Shell.Interop;

using Amazon.AWSToolkit.MobileAnalytics;
using System.ComponentModel;

#if VS2017_OR_LATER
using Microsoft.VisualStudio.TeamFoundation.Git.Extensibility;
#endif

#if VS2015_OR_LATER
using Microsoft.TeamFoundation.Git.Controls.Extensibility;
#endif

namespace Amazon.AWSToolkit.VisualStudio.Services
{
    internal class AWSToolkitGitServices : IAWSToolkitGitServices
    {
        private readonly ILog LOGGER = LogManager.GetLogger(typeof(AWSToolkitGitServices));
        IVsStatusbar _statusBar;

        public AWSToolkitGitServices(AWSToolkitPackage hostPackage)
        {
            HostPackage = hostPackage;
            hostPackage.JoinableTaskFactory.Run(async () =>
            {
                await hostPackage.JoinableTaskFactory.SwitchToMainThreadAsync();
                this._statusBar = hostPackage.GetVSShellService(typeof(IVsStatusbar)) as IVsStatusbar;
            });            
        }

        private AWSToolkitPackage HostPackage { get; }

        public IServiceProvider ServiceProvider { get; set; }

        public async Task CloneAsync(ServiceSpecificCredentials credentials, 
                                     string repositoryUrl, 
                                     string destinationFolder)
        {
            var recurseSubmodules = true;
            GitCredentials gitCredentials = null;

            try
            {
                var repoUrl = repositoryUrl.TrimEnd('/');

                // Push the service specific credentials to the Windows credential store. Team Explorer
                // seems to want only the domain host - specifying the full repo path yields a 'repo doesn't
                // exist' exception.
                var uri = new Uri(repoUrl);
                var gitCredentialKey = string.Format("git:{0}://{1}", uri.Scheme, uri.DnsSafeHost);
                gitCredentials = new GitCredentials(credentials.Username, credentials.Password, gitCredentialKey);

                TeamExplorerConnection.ActiveConnection.RegisterCredentials(gitCredentials);

#if VS2017_OR_LATER
                // note that in 2017, this service is also directly obtainable from HostPackage.GetVSShellService
                var gitExt = ServiceProvider.GetService(typeof(IGitActionsExt)) as IGitActionsExt;
                var progress = new Progress<Microsoft.VisualStudio.Shell.ServiceProgressData>();

                await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await this.HostPackage.JoinableTaskFactory.SwitchToMainThreadAsync();
                    progress.ProgressChanged += (s, e) =>
                    {
                        Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
                        _statusBar.SetText(e.ProgressText);
                    };
                    await gitExt.CloneAsync(repositoryUrl, 
                                            destinationFolder, 
                                            recurseSubmodules,
                                            default(CancellationToken), 
                                            progress).ConfigureAwait(false);
                });
#elif VS2015
                var gitExt = ServiceProvider.GetService(typeof(IGitRepositoriesExt)) as IGitRepositoriesExt;
                if (gitExt == null)
                    throw new Exception("Unable to obtain IGitRepositoriesExt interface from Team Explorer; unable to clone.");

                // The operation will have completed when CanClone goes false and then true again.
                var gotFalseSignal = false;
                var gotTrueSignal = false;
                PropertyChangedEventHandler tracker = (o, e) =>
                {
                    if (!string.Equals(e.PropertyName, "CanClone"))
                        return;

                    if (!gotFalseSignal && !gitExt.CanClone)
                        gotFalseSignal = true;

                    if (gotFalseSignal && !gotTrueSignal && gitExt.CanClone)
                        gotTrueSignal = true;
                };

                gitExt.PropertyChanged += tracker;
                gitExt.Clone(repositoryUrl, destinationFolder, recurseSubmodules ? CloneOptions.RecurseSubmodule : CloneOptions.None);

                await Task.Run(() =>
                {
                    while (!gotFalseSignal || !gotTrueSignal)
                    {
                        Thread.Sleep(100);
                    }
                });
#endif

                ToolkitEvent evnt = new ToolkitEvent();
                evnt.AddProperty(AttributeKeys.CodeCommitCloneStatus, ToolkitEvent.COMMON_STATUS_SUCCESS);
                SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);
            }
            catch (Exception e)
            {
                ToolkitEvent evnt = new ToolkitEvent();
                evnt.AddProperty(AttributeKeys.CodeCommitCloneStatus, ToolkitEvent.COMMON_STATUS_FAILURE);
                SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);

                LOGGER.Error("Clone using Team Explorer failed with exception", e);

                var msg = string.Format("Failed to clone repository {0}. Error message: {1}.",
                                        repositoryUrl,
                                        e.Message);
                ToolkitFactory.Instance.ShellProvider.ShowError("Repository Clone Failed", msg);
            }
            finally
            {
                if (gitCredentials != null && gitCredentials.Exists())
                {
                    gitCredentials.Dispose();
                }

                await HostPackage.JoinableTaskFactory.RunAsync(async () =>
                {
                    await HostPackage.JoinableTaskFactory.SwitchToMainThreadAsync();
                    TeamExplorerConnection.ActiveConnection.RefreshRepositories();
                });
            }
        }

        public async Task CreateAsync(INewCodeCommitRepositoryInfo newRepositoryInfo, 
                                      bool autoCloneNewRepository, 
                                      AWSToolkitGitCallbackDefinitions.PostCloneContentPopulationCallback contentPopulationCallback)
        {
            try
            {
                // delegate the repo creation to the CodeCommit plugin, then we'll take over for the
                // clone operation so that the new repo is recognized inside Team Explorer
                var codeCommitPlugin =
                    HostPackage.ToolkitShellProviderService
                        .QueryAWSToolkitPluginService(typeof(IAWSCodeCommit)) as IAWSCodeCommit;

                var codeCommitGitServices = codeCommitPlugin.ToolkitGitServices;
                codeCommitGitServices.ServiceProvider = ServiceProvider; // just in case we choose to use it one day
                await codeCommitGitServices.CreateAsync(newRepositoryInfo, false, null);

                var repository = codeCommitPlugin.GetRepository(newRepositoryInfo.Name, 
                                                                newRepositoryInfo.OwnerAccount,
                                                                newRepositoryInfo.Region);

                var svcCredentials
                    = newRepositoryInfo.OwnerAccount.GetCredentialsForService(ServiceSpecificCredentialStore
                        .CodeCommitServiceName);

                await CloneAsync(svcCredentials, repository.RepositoryUrl, newRepositoryInfo.LocalFolder);

                repository.LocalFolder = newRepositoryInfo.LocalFolder;

                var initialCommitContent = new List<string>();

                switch (newRepositoryInfo.GitIgnore.GitIgnoreType)
                {
                    case GitIgnoreOption.OptionType.VSToolkitDefault:
                        {
                            var content = S3FileFetcher.Instance.GetFileContent("CodeCommit/vsdefault.gitignore.txt", S3FileFetcher.CacheMode.PerInstance);
                            var target = Path.Combine(newRepositoryInfo.LocalFolder, ".gitignore");
                            File.WriteAllText(target, content);
                            initialCommitContent.Add(target);
                        }
                        break;

                    case GitIgnoreOption.OptionType.Custom:
                        {
                            var target = Path.Combine(newRepositoryInfo.LocalFolder, ".gitignore");
                            File.Copy(newRepositoryInfo.GitIgnore.CustomFilename, target);
                            initialCommitContent.Add(target);
                        }
                        break;

                    case GitIgnoreOption.OptionType.None:
                        break;
                }

                // if content needs to be populated, make the callback
                if (contentPopulationCallback != null)
                {
                    var contentAdded = contentPopulationCallback(repository.LocalFolder);
                    if (contentAdded != null && contentAdded.Any())
                    {
                        foreach (var c in contentAdded)                                                         
                        {
                            initialCommitContent.Add(c);
                        }
                    }
                }

                if (initialCommitContent.Any())
                {
                    codeCommitPlugin.StageAndCommit(repository.LocalFolder, initialCommitContent, "Initial commit", svcCredentials.Username);
                    codeCommitPlugin.Push(repository.LocalFolder, svcCredentials);
                }
                
                ToolkitEvent successEvent = new ToolkitEvent();
                successEvent.AddProperty(AttributeKeys.CodeCommitCreateStatus, ToolkitEvent.COMMON_STATUS_SUCCESS);
                SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(successEvent);
            }
            catch (Exception e)
            {
                ToolkitEvent evnt = new ToolkitEvent();
                evnt.AddProperty(AttributeKeys.CodeCommitCreateStatus, ToolkitEvent.COMMON_STATUS_FAILURE);
                SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);

                LOGGER.Error("Failed to create repository", e);

                var msg = string.Format("Error creating repository {0}: {1}.", newRepositoryInfo.Name, e.Message);
                ToolkitFactory.Instance.ShellProvider.ShowError("Repository Creation Failed", msg);
            }
            finally
            {
                await HostPackage.JoinableTaskFactory.RunAsync(async () =>
                {
                    await HostPackage.JoinableTaskFactory.SwitchToMainThreadAsync();
                    TeamExplorerConnection.ActiveConnection.RefreshRepositories();
                });
            }
        }

    }
}
