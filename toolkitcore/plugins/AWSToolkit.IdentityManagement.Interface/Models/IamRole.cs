namespace Amazon.AWSToolkit.IdentityManagement.Models
{
    public class IamRole
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Arn { get; set; }
        public string AssumeRolePolicyDocument { get; set; }
    }
}
