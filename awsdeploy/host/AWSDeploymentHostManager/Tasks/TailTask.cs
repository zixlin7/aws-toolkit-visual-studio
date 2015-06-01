using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Threading;

using log4net;
using AWSDeploymentHostManager.Persistence;

namespace AWSDeploymentHostManager.Tasks
{
    public class TailTask : Task
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(TailTask));

        private int nLines = 100;
        private long lines = 0;

        private PersistenceManager pm = new PersistenceManager();

        public enum TailLineCodes{
            NOT_SET = 0,
            ALL = -1,
            CURRENT = -2,
            LAST = -3
        }

        public override string Execute()
        {
            LOGGER.Info("Execute");
            string tail = String.Empty;
            TailLineCodes special = TailLineCodes.NOT_SET;

            //Read the optional parameters.
            string nLinesStr = null;
            if (this.parameters.TryGetValue("lines", out nLinesStr))
            {
                if (!Int32.TryParse(nLinesStr, out nLines))
                {
                    if (!Enum.TryParse<TailLineCodes>(nLinesStr, out special))
                    {
                        LOGGER.Error("Invalid Parameter: lines");
                        Event.LogWarn(Operation, "Invalid parameter in request: lines");
                        return "";
                    }
                }
                if (nLines < 0)
                {
                    if (Enum.IsDefined(special.GetType(), nLines))
                    {
                        special = (TailLineCodes)nLines;
                    }
                    else
                    {
                        LOGGER.Error("Invalid Parameter: lines");
                        Event.LogWarn(Operation, "Invalid parameter in request: lines");
                        return "";
                    }
                }
            }
            string logName = null;
            this.parameters.TryGetValue("log", out logName);

            //Tail all the logs
            IList<EntityObject> logDirectories = pm.SelectByStatus(EntityType.TimeStamp, "LogDirectoryScan");

            foreach (EntityObject logDir in logDirectories)
            {
                DirectoryInfo dirInfo = new DirectoryInfo(logDir.Parameters["path"]);

                if (!dirInfo.Exists)
                {
                    LOGGER.WarnFormat("Log directory {0} not found", logDir.Parameters["path"]);
                    continue;
                }

                LOGGER.InfoFormat("Scanning log directory {0}", logDir.Parameters["path"]);

                IDictionary<long, FileInfo> filesByDate = new Dictionary<long, FileInfo>();

                foreach (FileInfo fileInfo in dirInfo.GetFiles())
                {
                    bool added = false;
                    fileInfo.Refresh();
                    long ticks = fileInfo.LastWriteTime.Ticks;
                    while (!added)
                    {
                        try
                        {
                            filesByDate.Add(ticks, fileInfo);
                            added = true;
                        }
                        catch (ArgumentException)
                        {
                            ticks++;
                        }
                    }
                }

                var latest = from k in filesByDate.Keys
                             orderby k descending
                             select filesByDate[k];

                if (latest.Count() > 0)
                {
                    string filename = latest.ElementAt(0).Name;

                    if (logName != null)
                    {
                        if (!logName.Equals(filename))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        tail = String.Format("{0}{1}", tail, String.Format("\n{0}:\n\n", filename));
                    }
                    lines = 0;

                    FileInfo fi = null;
                    switch (special)
                    {
                        case TailLineCodes.CURRENT:
                            fi = (FileInfo)latest.ElementAt(0);
                            tail = String.Format("{0}{1}", tail, ReadEndLines(fi, Int64.MaxValue));
                            break;
                        case TailLineCodes.LAST:
                            if (latest.Count() > 1)
                            {
                                fi = (FileInfo)latest.ElementAt(1);
                                tail = String.Format("{0}{1}", tail, ReadEndLines(fi, Int64.MaxValue));
                            }
                            break;
                        case TailLineCodes.ALL:
                            for (int i = 0; i < latest.Count(); i++)
                            {
                                fi = (FileInfo)latest.ElementAt(i);

                                tail = String.Format("{0}{1}", tail, ReadEndLines(fi, Int64.MaxValue));
                            }
                            break;
                        default:
                            for (int i = 0; i < latest.Count(); i++)
                            {
                                fi = (FileInfo)latest.ElementAt(i);

                                tail = String.Format("{0}{1}", tail, ReadEndLines(fi, nLines - lines));

                                if (nLines == lines)
                                {
                                    break;
                                }
                            }
                            break;
                    }
                }
            }

            return GenerateResponse(Convert.ToBase64String(Encoding.UTF8.GetBytes(tail)));
        }

        public override string Operation
        {
            get { return "Tail";  }
        }

        public string ReadEndLines(FileInfo file, Int64 nLines)
        {
            string lineSeparator = "\n";
            int sizeOfChar = Encoding.UTF8.GetByteCount(lineSeparator);
            byte[] buffer = Encoding.UTF8.GetBytes(lineSeparator);

            using (FileStream fs = Triopinen(file))
            {
                if (null == fs)
                {
                    LOGGER.WarnFormat("Unable to open log file for reading. {0}", file.FullName);
                    return String.Empty;
                }

                Int64 tokenCount = 0;
                Int64 end = fs.Length;
                Int64 endPosition = end / sizeOfChar;
                
                for (Int64 position = sizeOfChar; position < endPosition; position += sizeOfChar)
                {
                    fs.Seek(end-position, SeekOrigin.Begin);
                    fs.Read(buffer, 0, buffer.Length);

                    if (Encoding.UTF8.GetString(buffer) == lineSeparator)
                    {
                        tokenCount++;
                        if (tokenCount == nLines)
                        {
                            byte[] returnBuffer = new byte[position-sizeOfChar];
                            fs.Read(returnBuffer, 0, returnBuffer.Length);
                            return Encoding.UTF8.GetString(returnBuffer);
                        }
                    }
                }

                fs.Seek(0, SeekOrigin.Begin);
                buffer = new byte[end];
                fs.Read(buffer, 0, buffer.Length);
                lines += tokenCount;
                return Encoding.UTF8.GetString(buffer);
            }
        }

        // Make a couple attempts to open a file for reading
        public static FileStream Triopinen(FileInfo file)
        {
            for (int i = 0, j = 100; i < 5; i++, j += 100)
            {
                try
                {
                    FileStream fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    if (null == fs)
                    {
                        Thread.Sleep(j);
                        continue;
                    }
                    return fs;
                }
                catch (Exception e)
                {
                    LOGGER.Debug("Exception opening logfile", e);
                    Thread.Sleep(j);
                }
            }
            return null;
        }

    }
}
