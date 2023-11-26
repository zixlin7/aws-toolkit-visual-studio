using System;

using Amazon.AWSToolkit.VisualStudio.Utilities.DTE;

using AwsToolkit.VsSdk.Common.LambdaTester;

using EnvDTE;

using log4net;

using Microsoft.VisualStudio.Threading;

using Task = System.Threading.Tasks.Task;

namespace Amazon.AWSToolkit.VisualStudio.Lambda
{
    public static class LambdaTesterUtilities
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(LambdaTesterUtilities));

        public static async Task  EnsureLambdaTesterConfiguredAsync(Solution solution, JoinableTaskFactory joinableTaskFactory)
        {
            LOGGER.Debug("Configuring Solution with Lambda Tester");
            await joinableTaskFactory.SwitchToMainThreadAsync();
            foreach (Project project in solution.Projects)
            {
                await EnsureLambdaTesterConfiguredAsync(project, joinableTaskFactory).ConfigureAwait(false);
            }

            LOGGER.Debug("Finished configuring Solution with Lambda Tester");
        }

        public static async Task EnsureLambdaTesterConfiguredAsync(Project project, JoinableTaskFactory joinableTaskFactory)
        {
            await joinableTaskFactory.SwitchToMainThreadAsync();

            if (project == null)
            {
                return;
            }

            try
            {
                var kind = project.Kind;

                // If we got an event that a project folder was opened then we have
                // to manually look for the projects under the folder because VS won't send 
                // the child events.
                if (string.Equals(kind, GuidList.VSProjectTypeProjectFolder))
                {
                    foreach (ProjectItem childItem in project.ProjectItems)
                    {
                        await EnsureLambdaTesterConfiguredAsync(childItem.SubProject, joinableTaskFactory).ConfigureAwait(false);
                    }
                }
                else
                {
                    var projectFileName = project.SafeGetFileName();
                    if (String.IsNullOrEmpty(projectFileName))
                    {
                        throw new Exception("Unable to determine project FileName");
                    }

                    await LambdaTesterInstaller.InstallAsync(projectFileName).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                LOGGER.Error(
                    "Error configuring Lambda Tester on project. " +
                    $"FileName: {project.SafeGetFileName()}, " +
                    $"FullName: {project.SafeGetFullName()}, " +
                    $"UniqueName: {project.SafeGetUniqueName()}",
                    e
                );
            }
        }
    }
}
