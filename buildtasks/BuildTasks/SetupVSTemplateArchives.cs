using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.Build.Utilities;

namespace BuildTasks
{
    public class SetupVSTemplateArchives : BuildTaskBase
    {
        /// <summary>
        /// The root folder containing one or more *.Template folders containing
        /// Visual Studio project templates to be discovered and archived.
        /// </summary>
        public string BaseTemplatesFolder { get; set; }

        /// <summary>
        /// The output folder that will contain the base project template archives.
        /// </summary>
        public string OutputFolder { get; set; }

        public override bool Execute()
        {
            CheckWaitForDebugger();

            // sanitize paths to make calculations easier
            OutputFolder = Path.GetFullPath(OutputFolder);
            BaseTemplatesFolder = Path.GetFullPath(BaseTemplatesFolder);

            if (!Directory.Exists(OutputFolder))
                Directory.CreateDirectory(OutputFolder);

            Log.LogMessage("Archiving project templates beneath {0} to output folder {1}", BaseTemplatesFolder, OutputFolder);
            var templateRoots = Directory.GetDirectories(BaseTemplatesFolder, "*.Templates", SearchOption.TopDirectoryOnly);
            foreach (var root in templateRoots)
            {
                ArchiveTemplates(root);
            }

            return true;
        }

        void ArchiveTemplates(string templatesRoot)
        {
            // find all csproj files under the root and construct a zip archive
            // for the project using the same relative path, placing it in the 
            // project folder's parent folder.
            Log.LogMessage("...processing {0}", Path.GetFileName(templatesRoot));

            var projectFiles = Directory.GetFiles(templatesRoot, "*.csproj", SearchOption.AllDirectories);
            foreach (var pf in projectFiles)
            {
                var projectFolder = Path.GetDirectoryName(pf);
                var projectParentFolder = Directory.GetParent(Path.GetDirectoryName(pf)).FullName;

                var projectMembers = Directory.GetFiles(projectFolder, "*.*", SearchOption.AllDirectories);

                var relativeOutFolder = projectParentFolder.Substring(BaseTemplatesFolder.Length + 1);
                var projectOutputFolder = Path.Combine(OutputFolder, relativeOutFolder);
                if (!Directory.Exists(projectOutputFolder))
                    Directory.CreateDirectory(projectOutputFolder);

                var projectFolderName = Path.GetFileNameWithoutExtension(projectFolder);
                var archiveName = Path.Combine(projectOutputFolder, projectFolderName + ".zip");

                using (var zipFile = new FileStream(archiveName, FileMode.Create))          
                {
                    using (var zipArchive = new ZipArchive(zipFile, ZipArchiveMode.Create))
                    {
                        foreach (var pm in projectMembers)
                        {
                            if (pm.IndexOf("/bin/", StringComparison.OrdinalIgnoreCase) > 0)
                                continue;
                            if (pm.IndexOf("/obj/", StringComparison.OrdinalIgnoreCase) > 0)
                                continue;

                            zipArchive.CreateEntryFromFile(pm, pm.Substring(projectFolder.Length + 1));
                        }
                    }
                }
            }

        }
    }
}
