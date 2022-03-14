namespace Amazon.AWSToolkit.CloudWatch.Models
{
    /// <summary>
    /// Represent a CloudWatch log group
    /// </summary>
    public class LogGroup
    {
        public string Name { get; set; }

        public string Arn { get; set; }

        protected bool Equals(LogGroup other)
        {
            return Name == other.Name && Arn == other.Arn;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((LogGroup)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ (Arn != null ? Arn.GetHashCode() : 0);
            }
        }
    }
}
