using System;
using Amazon.AWSToolkit.CodeCommit.Interface;
using Amazon.AWSToolkit.Tasks;

using log4net;

namespace Amazon.AWSToolkit.CodeCommitTeamExplorer.CodeCommit.Controllers
{
    /// <summary>
    /// Creates a new CodeCommit repository.
    /// </summary>
    internal class CreateRepositoryController : BaseCodeCommitController
    {
        public CreateRepositoryController(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            Logger = LogManager.GetLogger(typeof(CreateRepositoryController));
        }

        public override void Execute()
        {
            var operation = "codeCommitPlugin";
            try
            {
                CodeCommitPlugin = ToolkitFactory.Instance.QueryPluginService(typeof(IAWSCodeCommit)) as IAWSCodeCommit;
                if (CodeCommitPlugin == null)
                {
                    Logger.Error(
                        "Called to create a repository but CodeCommit plugin not loaded, cannot display repository details page");
                    return;
                }

                operation = "newRepoInfo";
                var newRepoInfo =
                    CodeCommitPlugin.PromptForRepositoryToCreate(Account, Region, GetLocalClonePathFromGitProvider());
                if (newRepoInfo == null)
                {
                    return;
                }

                operation = "gitCredentials";
                var gitCredentials = ObtainGitCredentials();
                if (gitCredentials == null)
                {
                    return;
                }

                // Create the repo at the service first, then clone locally so that Team Explorer becomes
                // aware of it. We will then call an optional delegate so that external tooling/wizards
                // can populate the repo with initial content that we will then commit and push.
                operation = "create";
                GitUtilities.CreateAsync(newRepoInfo).LogExceptionAndForget();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to create repository", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError("Repository Creation Failed", $"Error creating repository: {ex.Message}.");
            }
            finally
            {
                // Delegated create operation emits the create metric
                // Emit a false metric only if any of the previous required operations have failures
                if (!string.Equals(operation, "create"))
                {
                    GitUtilities.RecordCodeCommitCreateRepoMetric(false, operation);
                }
            }
        }
    }
}
