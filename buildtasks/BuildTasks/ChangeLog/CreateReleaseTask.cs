using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.Build.Framework;

namespace BuildTasks.ChangeLog
{
    public class CreateReleaseTask : BuildTaskBase
    {
        /// <summary>
        /// Name of the sub-directory containing release versions and next-release changelogs
        /// </summary>
        private const string ChangeDirectory = ".changes";

        /// <summary>
        /// The root folder of the repository containing change directory and files to be updated
        /// </summary>
        public string RepositoryRoot { get; set; }

        /// <summary>
        ///  The version number to be used for the next release
        /// </summary>
        public string ReleaseVersion { get; set; }

        /// <summary>
        /// The location for changes sub-directory containing release versions and next-release changelogs
        /// </summary>
        [Output]
        public string ChangeDirectoryPath { get; set; }

        /// <summary>
        /// The location for ReleaseNotes text file
        /// </summary>
        [Output]
        public string ReleaseNotesPath { get; set; }

        /// <summary>
        /// The location for next-release subfolder containing queued up changelogs
        /// </summary>
        [Output]
        public string NextReleasePath { get; set; }

        /// <summary>
        /// The location for CHANGELOG markdown file
        /// </summary>
        [Output]
        public string ChangeLogPath { get; set; }

        public override bool Execute()
        {
            ChangeDirectoryPath = Path.Combine(RepositoryRoot, ChangeDirectory);
            NextReleasePath = Path.Combine(ChangeDirectoryPath, "next-release");
            var releaseVersionFilePath = Path.Combine(ChangeDirectoryPath, $"{ReleaseVersion}.json");
            ReleaseNotesPath = Path.Combine(RepositoryRoot, "vspackages", "AWSToolkitPackage", "ReleaseNotes.txt");
            ChangeLogPath = Path.Combine(RepositoryRoot, "CHANGELOG.md");

            //validate files for release
            if (!ValidateFilesForRelease(releaseVersionFilePath))
            {
                return false;
            }

            //create change log version and write/update files
            var releaseLog = CreateChangeLogVersion();
            WriteJsonReleaseVersion(releaseLog, releaseVersionFilePath);
            UpdateChangeLogAndReleaseNotes(releaseLog);
            return true;
        }

        /// <summary>
        /// validate if there are no change files or if a release version already exists
        /// </summary>
        /// <param name="releaseVersionFilePath"></param>
        /// <returns>false if no change files or version already exists, else true</returns>
        private bool ValidateFilesForRelease(string releaseVersionFilePath)
        {
            //no change files in next-release directory
            if (!Directory.Exists(NextReleasePath) || (Directory.GetFiles(NextReleasePath).Length == 0))
            {
                Console.WriteLine("Error: no changes to release!");
                return false;
            }

            //changelog file already exists
            if (File.Exists(releaseVersionFilePath))
            {
                Console.WriteLine(
                    $"Error changelog file {releaseVersionFilePath} already exists for version  {ReleaseVersion}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Create a release object and retrieve changelog objects
        /// </summary>
        /// <returns></returns>
        public ChangeLogVersion CreateChangeLogVersion()
        {
            var time = DateTime.Now.ToString("yyyy-MM-dd");
            var changelogObjects = RetrieveChangeLogObjects();
            var releaseLog = new ChangeLogVersion
                {Date = time, Version = ReleaseVersion, Entries = changelogObjects};
            return releaseLog;
        }

        /// <summary>
        /// Retrieve changelogObjects from the change logs present in the next release directory
        /// </summary>
        /// <returns>List of ChangeLogEntry objects</returns>
        public List<ChangeLogEntry> RetrieveChangeLogObjects()
        {
            return Directory.EnumerateFiles(NextReleasePath, "*.*").Select(File.ReadAllText)
                .Select(JsonConvert.DeserializeObject<ChangeLogEntry>).ToList();
        }

        /// <summary>
        /// Write the change logs into a new release version json file
        /// </summary>
        /// <param name="releaseLog"></param>
        /// <param name="releaseVersionFilePath"></param>
        public static void WriteJsonReleaseVersion(ChangeLogVersion releaseLog, string releaseVersionFilePath)
        {
            var jsonString = JsonConvert.SerializeObject(releaseLog, Formatting.Indented);
            File.WriteAllText(releaseVersionFilePath, jsonString);
        }

        /// <summary>
        /// Update CHANGELOG and release notes file with new release version 
        /// </summary>
        /// <param name="releaseLog"></param>
        public void UpdateChangeLogAndReleaseNotes(ChangeLogVersion releaseLog)
        {
            var changelogTransform = new ChangeLogTransform(ChangeLogPath, releaseLog);
            var releaseNotesTransform = new ReleaseNotesTransform(ReleaseNotesPath, releaseLog);

            changelogTransform.GenerateOutputFile();
            releaseNotesTransform.GenerateOutputFile();
        }
    }
}