using System;
using Amazon.CloudWatchLogs.Model;

namespace Amazon.AWSToolkit.Lambda.Model
{
    public class LogStreamWrapper
    {
        readonly LogStream _originalStream;

        public LogStreamWrapper(LogStream stream) => this._originalStream = stream;
        public string LogStreamName => this._originalStream.LogStreamName;
        public DateTime FirstEventTimestamp => this._originalStream.FirstEventTimestamp.ToLocalTime();
        public DateTime LastEventTimestamp => this._originalStream.LastEventTimestamp.ToLocalTime();
        public string FormattedStoredBytes => this._originalStream.StoredBytes.ToString("N0");
    }
}
