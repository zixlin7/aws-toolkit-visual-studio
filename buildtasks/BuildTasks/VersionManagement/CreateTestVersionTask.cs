using System;

using Microsoft.Build.Framework;

namespace BuildTasks.VersionManagement
{
    /// <summary>
    /// Produces a version suitable for making test builds on a daily basis.
    /// The 4th version number (Revision) is altered by applying a date-stamp to it.
    /// Example: A version of 1.22.333.4 On Oct 7 is transformed to 1.22.333.10074
    /// </summary>
    public class CreateTestVersionTask : BuildTaskBase
    {
        public string InitialVersion { get; set; }

        [Output]
        public string Version { get; set; }

        public override bool Execute()
        {
            return Execute(DateTime.Now);
        }

        /// <summary>
        /// Overload allows for easier testing with a specific time
        /// </summary>
        /// <param name="time">Date used in producing the version number</param>
        public bool Execute(DateTime time)
        {
            var version = System.Version.Parse(InitialVersion);

            Version = new Version(
                version.Major,
                version.Minor,
                version.Build,
                GenerateTestRevision(time, version.Revision)).ToString();

            return true;
        }

        private static int GenerateTestRevision(DateTime dateTime, int currentRevision)
        {
            // Version values cannot exceed 65534 (when building VSIX files)
            // The maximum value (12310) added to the revision remains below this limit.
            var month = dateTime.Month;
            var day = dateTime.Day;
            return (month * 1000) + (day * 10) + currentRevision;
        }
    }
}
