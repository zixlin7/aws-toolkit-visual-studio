using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Build.Utilities;

using BuildCommon;

namespace BuildTasks
{
    public static class Utilities
    {
        public const string LAST_RELEASE_BRANCH = "last-release";
        public const string GITHUB_TOKEN_ID = "GitAccessCredentials";
        public const string SdkVersionsFilePath = @"generator\ServiceModels\_sdk-versions.json";
        public const string SdkChangeLog = @"SDK.CHANGELOG.md";
        public const string SdkEndpointFile = @"sdk\src\Core\endpoints.json";

        public const string KeySeparator = "/";
        public const string PathSeparator = "\\";

        public static void ReplaceFolderContents(string source, string target)
        {
            MakeSureDirectoryExist(source);
            MakeSureDirectoryExist(target);

            foreach (var file in Directory.GetFiles(target))
            {
                Console.WriteLine("Deleting {0}", file);
                File.Delete(file);
            }

            foreach (var file in Directory.GetFiles(source))
            {
                var destinationFile = Path.Combine(target, Path.GetFileName(file));
                Console.WriteLine("Copying {0} to {1}", file, destinationFile);
                File.Copy(file, destinationFile);
            }
        }

        public static void MakeSureDirectoryExist(string path)
        {
            if (Directory.Exists(path))
                return;

            Directory.CreateDirectory(path);
        }

        public static string GetRelativePath(string longerPath, string shorterPath)
        {
            int index = longerPath.IndexOf(shorterPath, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                throw new InvalidOperationException(string.Format("Shorter path not found in longer path. longerPath = {0}, shorterPath = {1}", longerPath, shorterPath));

            shorterPath = shorterPath.Trim('\\');
            string path = longerPath.Substring(shorterPath.Length + 1);
            return path;
        }

        public static HashSet<string> Split(string content, string delimiter, IEqualityComparer<string> comparer, Func<string, string> preProcess = null)
        {
            if (string.IsNullOrEmpty(content))
                return new HashSet<string>(comparer);

            var items = content
                .Split(new[] { delimiter }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Select(l => preProcess == null ? l : preProcess(l))
                .Where(l => !string.IsNullOrEmpty(l))
                .ToList();

            return new HashSet<string>(items, comparer);
        }

    }
}
