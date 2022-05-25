using System;
using System.Diagnostics;

namespace Amazon.AWSToolkit.Publish.Models.Configuration
{
    [DebuggerDisplay("{Id} ({Order}) | {DisplayName}")]
    public class Category : IEquatable<Category>, IComparable<Category>
    {
        public static readonly string FallbackCategoryId = "ToolkitDefaultCategory";

        public string Id { get; set; }
        public string DisplayName { get; set; }
        public int Order { get; set; } = int.MaxValue;

        public bool Equals(Category other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id && DisplayName == other.DisplayName && Order == other.Order;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Category)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Id != null ? Id.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (DisplayName != null ? DisplayName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Order;
                return hashCode;
            }
        }

        public static bool operator ==(Category left, Category right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Category left, Category right)
        {
            return !Equals(left, right);
        }

        public int CompareTo(Category other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;

            var orderComparison = Order.CompareTo(other.Order);
            if (orderComparison != 0) return orderComparison;

            var displayNameComparison = string.Compare(DisplayName, other.DisplayName, StringComparison.Ordinal);
            if (displayNameComparison != 0) return displayNameComparison;

            return string.Compare(Id, other.Id, StringComparison.Ordinal);
        }
    }
}
