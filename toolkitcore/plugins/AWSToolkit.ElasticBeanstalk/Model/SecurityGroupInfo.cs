namespace Amazon.AWSToolkit.ElasticBeanstalk.Model
{
    /// <summary>
    /// Used to handle multi-select of security groups in an items collection control
    /// and to deal with different RDS/EC2-VPC security group types. All we care about
    /// is the id/name
    /// </summary>
    public class SecurityGroupInfo
    {
        public string Name { get; set; }

        // only set group id for VPC mode
        public string Id { get; set; }

        public string Description { get; set; }

        public string DisplayName => IsVPCGroup ? string.Format("{0} (VPC)", Id) : Name;

        public bool IsVPCGroup => !string.IsNullOrEmpty(Id);
    }
}