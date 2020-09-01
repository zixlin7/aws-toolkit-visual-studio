using System;
using System.Text;

namespace BuildTasks.ChangeLog
{
    public class ReleaseNotesTransform : ChangeLogVersionTransformBase
    {
        public ReleaseNotesTransform(string outputFile, ChangeLogVersion releaseLog) : base(outputFile, releaseLog)
        {
        }

        protected override string GenerateHeading()
        {
            return $"v{ReleaseLog.Version} ({ReleaseLog.Date}){Environment.NewLine}";
        }

        protected override string GenerateEntries()
        {
            var releaseData = new StringBuilder();
            ReleaseLog.Entries.ForEach(change => releaseData.AppendLine($"* {change.Description}"));
            return releaseData.ToString();
        }
    }
}