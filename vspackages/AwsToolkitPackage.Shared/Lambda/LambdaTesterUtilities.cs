using System;
using Amazon.AWSToolkit.Lambda;
using Amazon.AWSToolkit.VisualStudio.Utilities.DTE;
using EnvDTE;
using log4net;
using Microsoft.VisualStudio.Threading;

using Task = System.Threading.Tasks.Task;

namespace Amazon.AWSToolkit.VisualStudio.Lambda
{
    public static class LambdaTesterUtilities
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(LambdaTesterUtilities));

        public static async Task  EnsureLambdaTesterConfiguredAsync(Solution solution, IAWSLambda lambdaPlugin, JoinableTaskFactory joinableTaskFactory)
        {
            LOGGER.Debug("Configuring Solution with Lambda Tester");
            await joinableTaskFactory.SwitchToMainThreadAsync();
            foreach (Project project in solution.Projects)
            {
                await EnsureLambdaTesterConfiguredAsync(project, lambdaPlugin, joinableTaskFactory).ConfigureAwait(false);
            }

            LOGGER.Debug("Finished configuring Solution with Lambda Tester");
        }

        public static async Task EnsureLambdaTesterConfiguredAsync(Project project, IAWSLambda lambdaPlugin, JoinableTaskFactory joinableTaskFactory)
        {
            await joinableTaskFactory.SwitchToMainThreadAsync();
            if (lambdaPlugin == null)
            {
                throw new ArgumentNullException(nameof(lambdaPlugin));
            }

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
                        await EnsureLambdaTesterConfiguredAsync(childItem.SubProject, lambdaPlugin, joinableTaskFactory).ConfigureAwait(false);
                    }
                }
                else
                {
                    var projectFileName = project.SafeGetFileName();
                    if (String.IsNullOrEmpty(projectFileName))
                    {
                        throw new Exception("Unable to determine project FileName");
                    }

                    await lambdaPlugin.EnsureLambdaTesterConfiguredAsync(projectFileName).ConfigureAwait(false);
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
