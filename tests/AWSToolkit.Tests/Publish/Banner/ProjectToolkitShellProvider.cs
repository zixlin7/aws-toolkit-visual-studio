using Amazon.AWSToolkit.Solutions;
using Amazon.AWSToolkit.Tests.Common.Context;

namespace AWSToolkit.Tests.Publish.Banner
{
    public class ProjectToolkitShellProvider : NoOpToolkitShellProvider
    {
        private readonly Project _project;

        public ProjectToolkitShellProvider(Project project)
        {
            _project = project;
        }

        public override Project GetSelectedProject()
        {
            return _project;
        }
    }
}
