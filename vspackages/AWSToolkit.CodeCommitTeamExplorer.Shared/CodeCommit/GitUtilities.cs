using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.AWSToolkit.CodeCommit.Interface;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Util;
using Amazon.AWSToolkit.CodeCommitTeamExplorer.CredentialManagement;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;

using log4net;
using Microsoft.VisualStudio.TeamFoundation.Git.Extensibility;

namespace Amazon.AWSToolkit.CodeCommitTeamExplorer.CodeCommit
{
    public class GitUtilities
    {
        private static readonly ILog LOGGER = LogManager.GetLogger(typeof(GitUtilities));

        public static async Task CloneAsync(ServiceSpecificCredentials credentials,
                                     string repositoryUrl,
                                     string destinationFolder,
                                     string operation)
        {
            var success = false;
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

                // note that in 2017, this service is also directly obtainable from HostPackage.GetVSShellService
                var gitExt = ToolkitFactory.Instance.ShellProvider.QueryShellProviderService<IGitActionsExt>();
                var progress = new Progress<Microsoft.VisualStudio.Shell.ServiceProgressData>();

                await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    progress.ProgressChanged += (s, e) =>
                    {
                        Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
                        ToolkitFactory.Instance.ShellProvider.UpdateStatus(e.ProgressText);
                    };
                    await gitExt.CloneAsync(repositoryUrl,
                                            destinationFolder,
                                            true,
                                            default(CancellationToken),
                                            progress).ConfigureAwait(false);
                });

                success = true;
            }
            catch (Exception e)
            {
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

                if (operation == "clone")
                {
                    RecordCodeCommitCloneRepoMetric(success, operation);
                }
                
                await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    TeamExplorerConnection.ActiveConnection.RefreshRepositories();
                });
            }
        }

        public static async Task CreateAsync(INewCodeCommitRepositoryInfo newRepositoryInfo,
                                      bool autoCloneNewRepository,
                                      AWSToolkitGitCallbackDefinitions.PostCloneContentPopulationCallback contentPopulationCallback)
        {
            var success = false;
            var phase = "create";
            try
            {
                // delegate the repo creation to the CodeCommit plugin, then we'll take over for the
                // clone operation so that the new repo is recognized inside Team Explorer                
                var codeCommitPlugin = ToolkitFactory.Instance.ShellProvider.QueryAWSToolkitPluginService(typeof(IAWSCodeCommit)) as IAWSCodeCommit;

                var codeCommitGitServices = codeCommitPlugin.CodeCommitGitServices;
                await codeCommitGitServices.CreateAsync(newRepositoryInfo, false, null);

                var repository = codeCommitPlugin.GetRepository(newRepositoryInfo.Name,
                                                                newRepositoryInfo.OwnerAccount,
                                                                newRepositoryInfo.Region);

                var svcCredentials
                    = newRepositoryInfo.OwnerAccount.GetCredentialsForService(ServiceSpecificCredentialStore
                        .CodeCommitServiceName);

                phase = "clone";
                await CloneAsync(svcCredentials, repository.RepositoryUrl, newRepositoryInfo.LocalFolder, "create");

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

                phase = "initialCommit";
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
                success = true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Failed to create repository", e);
                var msg = string.Format("Error creating repository {0}: {1}.", newRepositoryInfo.Name, e.Message);
                ToolkitFactory.Instance.ShellProvider.ShowError("Repository Creation Failed", msg);
            }
            finally
            {
                RecordCodeCommitCreateRepoMetric(success, phase);

                await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    TeamExplorerConnection.ActiveConnection.RefreshRepositories();
                });
            }
        }

        public static void RecordCodeCommitCreateRepoMetric(bool success, string reason)
        {
            var payload = new CodecommitCreateRepo
            {
                AwsAccount = GetAccountId(),
                AwsRegion = GetRegionId(),
                Result = success ? Result.Succeeded : Result.Failed
            };

            if (!success)
            {
                payload.Reason = reason;
            }

            ToolkitFactory.Instance.TelemetryLogger.RecordCodecommitCreateRepo(payload);
        }

        public static void RecordCodeCommitCloneRepoMetric(bool success, string reason)
        {
            var payload = new CodecommitCloneRepo
            {
                AwsAccount = GetAccountId(),
                AwsRegion = GetRegionId(),
                Result = success ? Result.Succeeded : Result.Failed
            };

            if (!success)
            {
                payload.Reason = reason;
            }

            ToolkitFactory.Instance.TelemetryLogger.RecordCodecommitCloneRepo(payload);
        }

        public static void RecordCodeCommitSetCredentialsMetric(bool success)
        {
            ToolkitFactory.Instance.TelemetryLogger.RecordCodecommitSetCredentials(new CodecommitSetCredentials
            {
                AwsAccount = GetAccountId(),
                AwsRegion = GetRegionId(),
                Result = success ? Result.Succeeded : Result.Failed
            });
        }

        private static string GetAccountId()
        {
            return TeamExplorerConnection.ActiveConnection?.AccountId ?? MetadataValue.NotSet;
        }

        private static string GetRegionId()
        {
            return TeamExplorerConnection.ActiveConnection?.Account.Region?.Id ?? MetadataValue.NotSet;
        }
    }
}
