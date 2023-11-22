using System.Threading.Tasks;

using Amazon.AwsToolkit.VsSdk.Common.Commands;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Commands
{
    public class FakeVsCommandRepository : IVsCommandRepository
    {
        public string CommandBinding;

        public Task<string> GetCommandBindingAsync(string commandName)
        {
            return Task.FromResult(CommandBinding);
        }
    }
}
