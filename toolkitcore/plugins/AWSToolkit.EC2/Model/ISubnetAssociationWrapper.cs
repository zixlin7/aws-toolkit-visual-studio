namespace Amazon.AWSToolkit.EC2.Model
{
    public interface ISubnetAssociationWrapper : IWrapper
    {
        string VpcId { get; }

        bool CanDisassociate { get; }
    }
}
