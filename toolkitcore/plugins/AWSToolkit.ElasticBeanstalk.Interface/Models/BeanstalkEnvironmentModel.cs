namespace Amazon.AWSToolkit.ElasticBeanstalk.Models
{
    public class BeanstalkEnvironmentModel
    {
        public string Id { get; }
        public string Name { get; }
        public string ApplicationName { get; }
        public string Cname { get; }

        public BeanstalkEnvironmentModel(string id, string name,
            string applicationName, string cname)
        {
            Id = id;
            Name = name;
            ApplicationName = applicationName;
            Cname = cname;
        }

        protected bool Equals(BeanstalkEnvironmentModel other)
        {
            return Id == other.Id && Name == other.Name && ApplicationName == other.ApplicationName && Cname == other.Cname;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((BeanstalkEnvironmentModel)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Id != null ? Id.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ApplicationName != null ? ApplicationName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Cname != null ? Cname.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(BeanstalkEnvironmentModel left, BeanstalkEnvironmentModel right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(BeanstalkEnvironmentModel left, BeanstalkEnvironmentModel right)
        {
            return !Equals(left, right);
        }
    }
}
