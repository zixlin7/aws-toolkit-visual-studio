using System.IO;

namespace Amazon.AWSToolkit.Tests.Integration.Publishing
{
    public class TestProjects
    {
        public static string GetASPNet5() => GetProjectPath("ASPNet5");

        private static string GetProjectPath(string projectName)
        {
            var testFolder = GetTestProjectFolder();
            return Path.Combine(testFolder, $@"{projectName}\{projectName}.csproj");
        }

        private static string GetTestProjectFolder()
        {
            return Path.Combine(GetTestsFolder(), "TestProjects");
        }

        private static string GetTestsFolder()
        {
            return GetParent(GetParent(GetParent(Directory.GetCurrentDirectory())));
        }

        private static string GetParent(string path)
        {
            return Directory.GetParent(path).FullName;
        }
    }
}
