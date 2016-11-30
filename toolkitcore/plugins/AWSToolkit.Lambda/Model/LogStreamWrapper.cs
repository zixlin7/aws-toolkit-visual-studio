using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.CloudWatchLogs.Model;

namespace Amazon.AWSToolkit.Lambda.Model
{
    public class LogStreamWrapper
    {
        LogStream _originalStream;

        public LogStreamWrapper(LogStream stream)
        {
            this._originalStream = stream;
        }

        public string LogStreamName
        {
            get { return this._originalStream.LogStreamName; }
        }

        public DateTime FirstEventTimestamp
        {
            get { return this._originalStream.FirstEventTimestamp.ToLocalTime(); }
        }

        public DateTime LastEventTimestamp
        {
            get { return this._originalStream.LastEventTimestamp.ToLocalTime(); }
        }

        public string FormattedStoredBytes
        {
            get { return this._originalStream.StoredBytes.ToString("N0"); }
        }
    }
}
