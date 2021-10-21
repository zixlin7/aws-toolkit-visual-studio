using System;
using Amazon.AWSToolkit.CodeCommit.Interface;
using log4net;

namespace Amazon.AWSToolkit.CodeCommitTeamExplorer.CodeCommit.Controllers
{
    /// <summary>
    /// Sequences the process of cloning a repository from CodeCommit inside Team Explorer, 
    /// ensuring the user has valid service-specific credentials.
    /// </summary>
    internal class CloneRepositoryController  : BaseCodeCommitController
    {
        public CloneRepositoryController(IServiceProvider serviceProvider)
        {
            Logger = LogManager.GetLogger(typeof(CloneRepositoryController));
            TeamExplorerServiceProvider = serviceProvider;
        }

        public IServiceProvider TeamExplorerServiceProvider { get; }

        /// <summary>
        /// Interactively clones a repository. The user is first asked to select the repo
        /// to clone and the local folder location. Once this is established we determine if
        /// service specific credentials are available to be set into the OS credential store
        /// for Team Explorer/git to work with:
        /// 1. If credentials are available locally for the selected user a/c, we use them silently
        /// 2. If no credentials are found locally, we determine if a set has been created in IAM
        /// 2a. If no credentials exist in IAM, we attempt to create some and prompt the user to
        ///     save the downloaded credentials if successful.
        /// 2b. If the attempt to create fails, we direct the user to supply credentials manually.
        /// </summary>
        /// <returns></returns>
        public override void Execute()
        {
            CodeCommitPlugin = ToolkitFactory.Instance.QueryPluginService(typeof(IAWSCodeCommit)) as IAWSCodeCommit;
            if (CodeCommitPlugin == null)
            {
                Logger.Error("Called to clone repository but CodeCommit plugin not loaded, cannot display repository list selector");
                return;
            }

            var selectedRepository = CodeCommitPlugin.PromptForRepositoryToClone(Account, Region, GetLocalClonePathFromGitProvider());
            if (selectedRepository == null)
                return;

            var gitCredentials = ObtainGitCredentials();
            if (gitCredentials == null)
                return;

            // delegate the actual clone operation via an intermediary; this allows us to use either
            // Team Explorer or CodeCommit to do the clone operation depending on the host shell.
            GitUtilities.CloneAsync(gitCredentials, selectedRepository.RepositoryUrl, selectedRepository.LocalFolder);
        }
    }
}
