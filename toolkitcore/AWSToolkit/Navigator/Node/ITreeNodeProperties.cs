namespace Amazon.AWSToolkit.Navigator.Node
{
    public interface ITreeNodeProperties
    {
        bool IsSelected { get; set; }
        bool IsExpanded { get; set; }

        string TextDecoration { get; }
    }
}
