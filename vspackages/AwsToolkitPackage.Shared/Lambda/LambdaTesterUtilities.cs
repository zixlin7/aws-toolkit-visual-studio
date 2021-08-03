using System;
using Amazon.AWSToolkit.Lambda;
using Amazon.AWSToolkit.VisualStudio.Utilities.DTE;
using EnvDTE;
using log4net;
using Microsoft.VisualStudio.Shell;

namespace Amazon.AWSToolkit.VisualStudio.Lambda
{
    public static class LambdaTesterUtilities
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(LambdaTesterUtilities));

        public static void EnsureLambdaTesterConfigured(Solution solution, IAWSLambda lambdaPlugin)
        {
            LOGGER.Debug("Configuring Solution with Lambda Tester");
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (Project project in solution.Projects)
            {
                EnsureLambdaTesterConfigured(project, lambdaPlugin);
            }

            LOGGER.Debug("Finished configuring Solution with Lambda Tester");
        }

        public static void EnsureLambdaTesterConfigured(Project project, IAWSLambda lambdaPlugin)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

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
                        EnsureLambdaTesterConfigured(childItem.SubProject, lambdaPlugin);
                    }
                }
                else
                {
                    var projectFileName = project.SafeGetFileName();
                    if (String.IsNullOrEmpty(projectFileName))
                    {
                        throw new Exception("Unable to determine project FileName");
                    }

                    lambdaPlugin.EnsureLambdaTesterConfigured(projectFileName);
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