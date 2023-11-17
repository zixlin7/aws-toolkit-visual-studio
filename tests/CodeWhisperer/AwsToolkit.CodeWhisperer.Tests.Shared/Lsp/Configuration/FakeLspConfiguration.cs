using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Configuration;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Configuration
{
    public class FakeLspConfiguration : ILspConfiguration
    {
        public readonly List<object> RaisedDidChangeConfigurations = new List<object>();

        public Task RaiseDidChangeConfigurationAsync(object configuration)
        {
            RaisedDidChangeConfigurations.Add(configuration);
            return Task.CompletedTask;
        }
    }
}
