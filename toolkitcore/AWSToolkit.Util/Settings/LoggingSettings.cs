using System;
using System.ComponentModel;

namespace Amazon.AWSToolkit.Settings
{
    /// <summary>
    /// Represents all logging related toolkit settings
    /// </summary>
    public class LoggingSettings : IEquatable<LoggingSettings>
    {
        public static class DefaultValues
        {
            public const int LogFileRetentionMonths = 3;
            public const int MaxLogDirectorySizeMb = 100;
            public const int MaxLogFileSizeMb = 10;
            public const int MaxFileBackups = 10;
        }

        [DefaultValue(DefaultValues.LogFileRetentionMonths)]
        public int LogFileRetentionMonths { get; set; } = DefaultValues.LogFileRetentionMonths;

        [DefaultValue(DefaultValues.MaxLogDirectorySizeMb)]
        public int MaxLogDirectorySizeMb { get; set; } = DefaultValues.MaxLogDirectorySizeMb;

        [DefaultValue(DefaultValues.MaxLogFileSizeMb)]
        public int MaxLogFileSizeMb { get; set; } = DefaultValues.MaxLogFileSizeMb;

        [DefaultValue(DefaultValues.MaxFileBackups)]
        public int MaxFileBackups { get; set; } = DefaultValues.MaxFileBackups;

        public bool Equals(LoggingSettings other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return LogFileRetentionMonths == other.LogFileRetentionMonths &&
                   MaxLogDirectorySizeMb == other.MaxLogDirectorySizeMb && MaxLogFileSizeMb == other.MaxLogFileSizeMb &&
                   MaxFileBackups == other.MaxFileBackups;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }
            return Equals((LoggingSettings) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = LogFileRetentionMonths;
                hashCode = (hashCode * 397) ^ MaxLogDirectorySizeMb;
                hashCode = (hashCode * 397) ^ MaxLogFileSizeMb;
                hashCode = (hashCode * 397) ^ MaxFileBackups;
                return hashCode;
            }
        }
    }
}
