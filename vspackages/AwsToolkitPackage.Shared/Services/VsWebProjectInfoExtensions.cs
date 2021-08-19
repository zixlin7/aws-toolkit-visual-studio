using Amazon.AWSToolkit.Solutions;
using Amazon.AwsToolkit.VsSdk.Common;

namespace Amazon.AWSToolkit.VisualStudio.Services
{
    public static class VsWebProjectInfoExtensions
    {
        public static Project AsProject(this VSWebProjectInfo projectInfo)
        {
            return new Project(GetProjectTypeFrom(projectInfo));
        }

        private static ProjectType GetProjectTypeFrom(VSWebProjectInfo projectInfo)
        {
            switch (projectInfo.VsProjectType)
            {
                case VSWebProjectInfo.VsWebProjectType.CoreCLRWebProject:
                    return ProjectType.NetCore;

                case VSWebProjectInfo.VsWebProjectType.WebApplicationProject:
                    return ProjectType.NetFramework;

                default:
                    return ProjectType.Unknown;
            }
        }
    }
}
