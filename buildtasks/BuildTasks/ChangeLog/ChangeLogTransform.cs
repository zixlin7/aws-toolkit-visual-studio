using System;
using System.Text;

namespace BuildTasks.ChangeLog
{
    public class ChangeLogTransform : ChangeLogVersionTransformBase
    {
        public ChangeLogTransform(string outputFile, ChangeLogVersion releaseLog) : base(outputFile, releaseLog)
        {
        }

        protected override string GenerateHeading()
        {
            return $"## {ReleaseLog.Version} ({ReleaseLog.Date}){Environment.NewLine}{Environment.NewLine}";
        }

        protected override string GenerateEntries()
        {
            var appendData = new StringBuilder();
            ReleaseLog.Entries.ForEach(change => appendData.AppendLine($"- **{change.Type}** {change.Description}"));
            return appendData.ToString();
        }
    }
}