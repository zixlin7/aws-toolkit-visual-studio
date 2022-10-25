using System;

namespace Amazon.AWSToolkit.CodeCatalyst.Models
{
    public interface ICodeCatalystAccessToken
    {
        string Name { get; }

        string Secret { get; }

        DateTime ExpiresOn { get; }
    }
}
