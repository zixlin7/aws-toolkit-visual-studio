using System;
using System.Runtime.Serialization;

namespace Amazon.AWSToolkit.Models.Text
{
    /// <summary>
    /// Represents a range of text within a document or piece of text.
    /// </summary>
    [DataContract]
    public class Range : IEquatable<Range>
    {
        [DataMember(Name = "start")]
        public Position Start { get; set; }

        [DataMember(Name = "end")]
        public Position End { get; set; }

        public bool Equals(Range other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Equals(Start, other.Start) && Equals(End, other.End);
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((Range)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Start != null ? Start.GetHashCode() : 0) * 397) ^ (End != null ? End.GetHashCode() : 0);
            }
        }
    }
}
