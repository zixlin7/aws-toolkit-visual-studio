using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using LitJson;

using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

namespace BuildTasks
{
    public abstract class BaseUpdateVersionTask : BuildTaskBase
    {
        /// <summary>
        /// The root folder of the repository holding the component to be version stamped.
        /// </summary>
        public string RepositoryRoot { get; set; }

        /// <summary>
        /// The subfolder locations, ;-delimited, to search for AssemblyInfo.cs files. If not
        /// specified all folders beneath RepositoryRoot are scanned.
        /// </summary>
        public string AssemblyInfoPaths { get; set; }

        /// <summary>
        /// The subfolder locations, ;-delimited, to exclude when seaching for assembly infos.
        /// </summary>
        public string ExcludePaths { get; set; }

        /// <summary>
        /// The version number to be stamped into the component
        /// </summary>
        public string VersionNumber {get; set; }

        public override bool Execute()
        {
            CheckWaitForDebugger();

            if (string.IsNullOrEmpty(this.VersionNumber))
            {
                this.Log.LogMessage("Skipping version stamping");
                return true;
            }

            return PerformExecute();
        }

        protected virtual void PatchFile(string targetFile, string contentPrefix, string contentSuffix)
        {
            if (!VerifyFileExists(targetFile)) return;

            var content = File.ReadAllText(targetFile);
            content = replaceVersion(content, contentPrefix, contentSuffix);

            File.WriteAllText(targetFile, content);
        }

        protected abstract bool PerformExecute();

        public void UpdateAssemblyInfoFiles()
        {
            var folderSet = new List<string>();
            if (string.IsNullOrEmpty(AssemblyInfoPaths))
                folderSet.Add(RepositoryRoot);
            else
            {
                var subPaths = AssemblyInfoPaths.Split(';');
                folderSet.AddRange(subPaths.Select(sp => Path.Combine(RepositoryRoot, sp)));
            }

            var excludeSet = new List<string>();
            if(!string.IsNullOrEmpty(ExcludePaths))
            {
                var subPaths = ExcludePaths.Split(';');
                excludeSet.AddRange(subPaths.Select(sp => Path.Combine(RepositoryRoot, sp)));
            }

            foreach (var f in folderSet)
            {
                var files = Directory.GetFiles(f, "AssemblyInfo.cs", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    if (excludeSet.FirstOrDefault(x => file.StartsWith(x)) == null)
                    {
                        PatchAssemblyInfoFile(file);
                    }
                }
            }
        }

        void PatchAssemblyInfoFile(string relativePath)
        {
            string file;
            if (File.Exists(relativePath))
                file = relativePath;
            else
                file = RepositoryRoot + relativePath + "/Properties/AssemblyInfo.cs";

            if (!VerifyFileExists(file)) return;
            string content = File.ReadAllText(file);

            content = replaceVersion(content, "\r\n[assembly: AssemblyVersion(\"", "\")]");
            content = replaceVersion(content, "[assembly: AssemblyFileVersion(\"", "\")]");

            File.WriteAllText(file, content);
        }

        protected bool VerifyFileExists(string file)
        {
            if (!File.Exists(file))
            {
                Console.WriteLine("...file [" + file + "] does not exist, skipping version update");
                return false;
            }
            return true;
        }

        protected string replaceVersion(string content, string beforeToken, string afterToken)
        {
            return replaceVersion(content, beforeToken, afterToken, VersionNumber);
        }

        protected string replaceVersion(string content, string beforeToken, string afterToken, string versionString)
        {
            string newVersion = content;
            int start = newVersion.IndexOf(beforeToken);
            if (start == -1)
                return newVersion;

            start += beforeToken.Length;
            while (start > 0)
            {
                int end = newVersion.IndexOf(afterToken, start);
                newVersion = newVersion.Substring(0, start) + versionString + newVersion.Substring(end);
                if (start + beforeToken.Length >= content.Length)
                    break;

                start = content.IndexOf(beforeToken, start + beforeToken.Length);
                if (start == -1)
                    break;

                start += beforeToken.Length;
            }
            return newVersion;
        }
    }

    public class UpdatePowerShellVersionTask : BaseUpdateVersionTask
    {
        protected override bool PerformExecute()
        {
            this.Log.LogMessage("Updating PowerShell AssemblyInfo files for version {0}", this.VersionNumber);
            base.UpdateAssemblyInfoFiles();

            var modulePath = Path.Combine(RepositoryRoot, @"modules\AWSPowerShell");
            var moduleFiles = Directory.GetFiles(modulePath, "*.psd1");
            foreach (var moduleFile in moduleFiles)
            {
                this.Log.LogMessage("...updating module {0} for version {1}", moduleFile, this.VersionNumber);
                PatchFile(moduleFile, "ModuleVersion = '", "'");
            }

            return true;
        }
    }

    /// <summary>
    /// General purpose update task for assemblyinfo files
    /// </summary>
    public class UpdateAssemblyInfoVersionTask : BaseUpdateVersionTask
    {
        protected override bool PerformExecute()
        {
            this.Log.LogMessage("Updating AssemblyInfo files for version {0}", this.VersionNumber);
            base.UpdateAssemblyInfoFiles();

            return true;
        }
    }

    public class UpdateVSToolkitVersionTask : BaseUpdateVersionTask
    {
        protected override bool PerformExecute()
        {
            this.Log.LogMessage("Updating VSToolkit AssemblyInfo files for version {0}", this.VersionNumber);
            base.UpdateAssemblyInfoFiles();

            this.Log.LogMessage("...updating Visual Studio package manifest for version {0}", this.VersionNumber);
            foreach (var manifestFile in Directory.GetFiles(this.RepositoryRoot, "source.extension.vsixmanifest", SearchOption.AllDirectories))
            {
                Console.WriteLine("Updating toolkit manifest: " + manifestFile);
                PatchFile(manifestFile, "<Version>", "</Version>");
                PatchFile(manifestFile, "Id=\"|%CurrentProject%;GetVsixGuid|\" Version=\"", "\"");
            }

            var assemblyVersionFile = Directory.GetFiles(Path.Combine(this.RepositoryRoot, "buildtools") , "AssemblyVersion.cs").FirstOrDefault();
            if(File.Exists(assemblyVersionFile))
            {
                Console.WriteLine(@"Updating buildtools\AssemblyVersion.cs: " + assemblyVersionFile);
                PatchFile(assemblyVersionFile, "AssemblyVersion(\"", "\")]");
                PatchFile(assemblyVersionFile, "AssemblyFileVersion(\"", "\")]");
                PatchFile(assemblyVersionFile, "AssemblyInformationalVersion(\"", "\")]");
            }

            return true;
        }
    }

    // MS Installer version only support 1 byte for major, 1 byte for minor, and 2 bytes for patch number.
    // https://msdn.microsoft.com/en-us/library/windows/desktop/aa370859(v=vs.85).aspx
    public class InstallerVersion
    {
        public byte Major { get; set; }
        public byte Minor { get; set; }
        public ushort Patch { get; set; }

        public InstallerVersion()
        {
            this.Major = 1;
            this.Major = 0;
            this.Patch = 0;
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}.{2}", Major, Minor, Patch);
        }
    }

    public class ReadInstallerVersionFileTask : BuildTaskBase
    {
        public string VersionFile { get; set; }

        [Output]
        public string FullVersion { get; set; }

        public override bool Execute()
        {
            InstallerVersion version;

            if (File.Exists(VersionFile))
            {
                version = JsonMapper.ToObject<InstallerVersion>(File.ReadAllText(VersionFile));
                FullVersion = version.ToString();

                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public class IncrementInstallerPatchNumberTask : BuildTaskBase
    {
        public string VersionFile { get; set; }

        public override bool Execute()
        {
            InstallerVersion version;

            version = JsonMapper.ToObject<InstallerVersion>(File.ReadAllText(VersionFile));
            version.Patch = (ushort)(version.Patch + 1);

            using (var stream = File.Open(VersionFile, FileMode.Truncate, FileAccess.Write))
            using (var writer = new StreamWriter(stream))
            {
                var jsonWriter = new LitJson.JsonWriter(writer);
                jsonWriter.PrettyPrint = true;
                LitJson.JsonMapper.ToJson(version, jsonWriter);
            }

            return true;
        }
    }

    public class UpdateInstallVersionsTask : BuildTaskBase
    {
        public string InstallerVersion { get; set; }
        public string SDKV1Version { get; set; }
        public string SDKV2Version { get; set; }
        public string SDKV3Version { get; set; }
        public string ToolkitVersion { get; set; }
        public string PowerShellVersion { get; set; }

        /// <summary>
        /// The root folder of the repository holding the components to be version stamped.
        /// </summary>
        public string RepositoryRoot { get; set; }

        public override bool Execute()
        {
            CheckWaitForDebugger();

            var productFile = Path.Combine(RepositoryRoot, @"Setup\Product.wxs");
            if (!File.Exists(productFile))
                throw new ArgumentException("Unable to find product file to version: {0}", productFile);

            var content = File.ReadAllText(productFile);

            content = ReplaceVersion(content, "Title=\"Version 1 (", ")\" AllowAdvertise", SDKV1Version);
            content = ReplaceVersion(content, "Title=\"Version 2 (", ")\" AllowAdvertise", SDKV2Version);
            content = ReplaceVersion(content, "Title=\"Version 3 (", ")\" AllowAdvertise", SDKV3Version);
            content = ReplaceVersion(content, "Title=\"AWS Tools for Windows PowerShell (", ")\" Level", PowerShellVersion);
            content = ReplaceVersion(content, "Title=\"AWS Toolkit for Visual Studio (", ")\" Level", ToolkitVersion);
            content = ReplaceVersion(content, "Language=\"1033\" Version=\"", "\" Manufacturer=", InstallerVersion);

            content = ReplaceVersion(content, "Property=\"OLD_VERSION_FOUND\" Minimum=\"1.0.0.0\" Maximum=\"", "\" IncludeMinimum=\"yes\"", InstallerVersion);
            content = ReplaceVersion(content, "Property=\"NEWER_VERSION_FOUND\" Minimum=\"", "\" IncludeMinimum=\"no\"", InstallerVersion);

            File.WriteAllText(productFile, content);

            return true;
        }

        static string ReplaceVersion(string content, string beforeToken, string afterToken, string versionString)
        {
            var newVersion = content;
            var start = newVersion.IndexOf(beforeToken) + beforeToken.Length;
            while (start > 0)
            {
                var end = newVersion.IndexOf(afterToken, start);
                newVersion = newVersion.Substring(0, start) + versionString + newVersion.Substring(end);
                if (start + beforeToken.Length >= content.Length)
                    break;

                start = content.IndexOf(beforeToken, start + beforeToken.Length);
                if (start == -1)
                    break;

                start += beforeToken.Length;
            }

            return newVersion;
        }
    }

}
