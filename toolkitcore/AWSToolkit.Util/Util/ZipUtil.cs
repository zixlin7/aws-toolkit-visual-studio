using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using log4net;

namespace Amazon.AWSToolkit.Util
{
    public static class ZipUtil
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ZipUtil));

        /// <summary>
        /// Produce a Zip file from all files (recursively) within <paramref name="sourceFolder"/>
        /// </summary>
        /// <param name="zipFile">Full path of zip file to create</param>
        /// <param name="sourceFolder">Folder of files to zip</param>
        public static void CreateZip(string zipFile, string sourceFolder)
        {
            if (File.Exists(zipFile))
            {
                throw new Exception($"File already exists: {zipFile}");
            }

            var zipContents = Directory.GetFiles(sourceFolder, "*.*", SearchOption.AllDirectories)
                .ToDictionary(
                    file => file,
                    file => file.Substring(sourceFolder.Length + 1));

            CreateZip(zipFile, zipContents);
        }

        /// <summary>
        /// Produce a Zip file using the given file-destination mapping
        /// </summary>
        /// <param name="zipFile">Full path of zip file to create</param>
        /// <param name="contents">Files to add to zip file, mapped from full path to destination path within the zip file</param>
        public static void CreateZip(string zipFile, Dictionary<string, string> contents)
        {
            if (File.Exists(zipFile))
            {
                throw new Exception($"File already exists: {zipFile}");
            }

            Logger.Debug($"Creating Zip file: {zipFile}");

            using (var zipArchive = ZipFile.Open(zipFile, ZipArchiveMode.Create))
            {
                contents.ToList().ForEach(keyValue =>
                {
                    // Write to zip with forward slashes for compatibility with non-Windows systems
                    var zipEntryPath = keyValue.Value.Replace('\\', '/');
                    zipArchive.CreateEntryFromFile(keyValue.Key, zipEntryPath);
                });
            }
        }

        /// <summary>
        /// Extract the contents of a zip file
        /// </summary>
        /// <param name="zipFile">File to unzip</param>
        /// <param name="destFolder">Location to extract files to</param>
        /// <param name="overwriteFiles">true will overwrite existing files, false will throw an error if an overwrite occurs</param>
        public static void ExtractZip(string zipFile, string destFolder, bool overwriteFiles)
        {
            if (!File.Exists(zipFile))
            {
                throw new FileNotFoundException();
            }

            if (overwriteFiles)
            {
                RemoveExistingFiles(zipFile, destFolder);
            }

            ZipFile.ExtractToDirectory(zipFile, destFolder);
        }

        private static void RemoveExistingFiles(string zipFile, string destFolder)
        {
            using (var zipArchive = ZipFile.OpenRead(zipFile))
            {
                zipArchive.Entries
                    .ToList()
                    .ForEach(archiveEntry =>
                    {
                        var destPath = Path.Combine(destFolder, archiveEntry.FullName);
                        if (File.Exists(destPath))
                        {
                            File.Delete(destPath);
                        }
                    });
            }
        }
    }
}