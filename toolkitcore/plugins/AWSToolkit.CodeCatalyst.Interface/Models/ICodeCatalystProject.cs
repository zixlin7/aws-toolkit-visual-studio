namespace Amazon.AWSToolkit.CodeCatalyst.Models
{
    public interface ICodeCatalystProject
    {
        string Name { get; }

        string SpaceName { get; }

        string DisplayName { get; }

        string Description { get; }
    }
}
