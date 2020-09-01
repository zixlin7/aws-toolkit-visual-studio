using System;
using System.IO;
using System.Text;

namespace BuildTasks.ChangeLog
{
    public abstract class ChangeLogVersionTransformBase
    {
        protected ChangeLogVersion ReleaseLog;
        protected string OutputFile;

        protected ChangeLogVersionTransformBase(string outputFile, ChangeLogVersion releaseLog)
        {
            OutputFile = outputFile;
            ReleaseLog = releaseLog;
        }

        /// <summary>
        /// Add release log and generate/update output file 
        /// </summary>
        public void GenerateOutputFile()
        {
            var appendData = new StringBuilder();
            appendData.Append(GenerateHeading());
            appendData.Append(GenerateEntries());
            appendData.Append(GetExistingOutputFile());
            File.WriteAllText(OutputFile, appendData.ToString());
        }

        /// <summary>
        /// Add heading for new release log to output file
        /// </summary>
        /// <returns></returns>
        protected abstract string GenerateHeading();

        /// <summary>
        /// Add changelog entries for new release log to output file
        /// </summary>
        /// <returns></returns>
        protected abstract string GenerateEntries();

        /// <summary>
        /// Retrieve existing content of the output file
        /// </summary>
        /// <returns></returns>
        private string GetExistingOutputFile()
        {
            var fileData = string.Empty;
            if (File.Exists(OutputFile))
            {
                fileData = Environment.NewLine + File.ReadAllText(OutputFile);
            }

            return fileData;
        }
    }
}