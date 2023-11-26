using System;

namespace Amazon.AWSToolkit
{
    /// <summary>
    /// Defines a version range with a start and end(excluded)
    /// </summary>
    public class VersionRange
    {
        public VersionRange(Version start, Version end)
        {
            Start = start;
            End = end;
        }

        /// <summary>
        /// Represents the start of a version range(included)
        /// </summary>
        public Version Start { get; }

        /// <summary>
        /// Represents the end value(not included) for a version range
        /// </summary>
        public Version End { get; }


        public bool ContainsVersion(Version version)
        {
            return version != null && version >= Start && version < End;
        }
    }
}
