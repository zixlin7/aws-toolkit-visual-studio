using Amazon.AWSToolkit.CodeCommit.Interface;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.VisualStudio.TeamExplorer.CodeCommit.Model;
using log4net;

namespace Amazon.AWSToolkit.VisualStudio.TeamExplorer.CodeCommit.Controllers
{
    /// <summary>
    /// Creates a new CodeCommit repository.
    /// </summary>
    internal class CreateRepositoryController : BaseCodeCommitController
    {
        public CreateRepositoryController()
        {
            Logger = LogManager.GetLogger(typeof(CreateRepositoryController));
        }

        public override void Execute()
        {
            CodeCommitPlugin = ToolkitFactory.Instance.QueryPluginService(typeof(IAWSCodeCommit)) as IAWSCodeCommit;
            if (CodeCommitPlugin == null)
            {
                Logger.Error("Called to create a repository but CodeCommit plugin not loaded, cannot display repository details page");
                return;
            }

            var repository = CodeCommitPlugin.PromptForRepositoryToCreate(Account, Region, GetLocalClonePathFromGitProvider());
            if (repository == null)
                return;

            var gitCredentials = ObtainGitCredentials();
            if (gitCredentials == null)
                return;

            // delegate the actual clone operation via an intermediary; this allows us to use either
            // Team Explorer or CodeCommit to do the clone operation depending on the host shell.
            var gitServices = ToolkitFactory
                                  .Instance
                                  .ShellProvider
                                  .QueryShellProviderService<IAWSToolkitGitServices>() ?? ToolkitFactory
                                  .Instance
                                  .QueryPluginService(typeof(IAWSToolkitGitServices)) as IAWSToolkitGitServices;
            gitServices?.Clone(repository.RepositoryUrl, repository.LocalFolder, gitCredentials);
        }
    }
}
