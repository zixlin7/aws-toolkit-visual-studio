using System.Collections.Generic;

namespace BuildTasks.ChangeLog
{
    /// <summary>
    /// Class represents a changelog release version 
    /// </summary>
    public class ChangeLogVersion
    {
        public string Date;
        public string Version;
        public List<ChangeLogEntry> Entries;
    }
}