using System;

namespace Amazon.AWSToolkit.CodeCatalyst.Models
{
    public interface ICodeCatalystSpace
    {
        string Name { get; }

        string DisplayName { get; }

        string Description { get; }

        string RegionId { get; }
    }
}
