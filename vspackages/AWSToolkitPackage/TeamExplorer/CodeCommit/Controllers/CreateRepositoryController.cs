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

            var newRepoInfo = CodeCommitPlugin.PromptForRepositoryToCreate(Account, Region, GetLocalClonePathFromGitProvider());
            if (newRepoInfo == null)
                return;

            var gitCredentials = ObtainGitCredentials();
            if (gitCredentials == null)
                return;

            // Create the repo at the service first, then clone locally so that Team Explorer becomes
            // aware of it. We will then call an optional delegate so that external tooling/wizards
            // can populate the repo with initial content that we will then commit and push.
            var gitServices = ToolkitFactory
                                  .Instance
                                  .ShellProvider
                                  .QueryShellProviderService<IAWSToolkitGitServices>() ?? ToolkitFactory
                                  .Instance
                                  .QueryPluginService(typeof(IAWSToolkitGitServices)) as IAWSToolkitGitServices;
            gitServices.CreateAsync(newRepoInfo, true, null);
        }
    }
}
