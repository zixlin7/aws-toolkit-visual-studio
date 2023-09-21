using System;
using System.Runtime.Serialization;

namespace Amazon.AWSToolkit.Models.Text
{
    /// <summary>
    /// Represents a location within a document or piece of text.
    /// Whether or not the location is 0-indexed or 1-indexed depends on where it is used.
    /// The recommendation is to treat this as 0-indexed for consistency.
    /// </summary>
    [DataContract]
    public class Position : IEquatable<Position>
    {
        public Position()
        {
        }

        public Position(int line, int column)
        {
            Line = line;
            Column = column;
        }

        [DataMember(Name = "line")]
        public int Line { get; set; }

        [DataMember(Name = "column")]
        public int Column { get; set; }

        public bool Equals(Position other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Line == other.Line && Column == other.Column;
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

            return Equals((Position)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Line * 397) ^ Column;
            }
        }
    }
}
