using System;
using System.Threading.Tasks;

namespace Amazon.AWSToolkit.CodeCatalyst.Models
{
    public interface ICodeCatalystRepository
    {
        string Name { get; }

        string SpaceName { get; }

        string ProjectName { get; }

        string Description { get; }

        Task<Uri> GetCloneUrlAsync(CloneUrlType cloneUrlType);
    }

    public enum CloneUrlType
    {
        Https
    }
}
