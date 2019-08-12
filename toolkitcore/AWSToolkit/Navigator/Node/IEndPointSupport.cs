namespace Amazon.AWSToolkit.Navigator.Node
{
    public interface IEndPointSupport : IViewModel
    {
        RegionEndPointsManager.EndPoint CurrentEndPoint { get; }
        void UpdateEndPoint(string regionName);
    }
}
