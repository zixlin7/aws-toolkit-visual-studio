using System;
using System.Runtime.Versioning;

using Amazon.AWSToolkit.Solutions;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.Tests.Common.Context
{
    public class ProjectFixture
    {
        public static Project Create()
        {
            var targetFramework = new FrameworkName(FrameworkNameHelper.DotNetCoreIdentifier, new Version(5, 0));
            return new Project("SampleProject", GetNet5ProjectFilePath(), targetFramework);
        }

        public static string GetNet5ProjectFilePath() => @"Context\AspNet.csproj";
    }
}
