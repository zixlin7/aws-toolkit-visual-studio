using System.Runtime.Versioning;

using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.Publish
{
    public static class PublishableProjectSpecification
    {
        public static bool IsSatisfiedBy(FrameworkName targetFramework)
        {
            return IsNotDotNetFramework(targetFramework);
        }

        private static bool IsNotDotNetFramework(FrameworkName targetFramework)
        {
            return !FrameworkNameHelper.IsDotNetFramework(targetFramework);
        }
    }
}
