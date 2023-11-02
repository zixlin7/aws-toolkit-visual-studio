using System.Threading.Tasks;

namespace Amazon.AwsToolkit.VsSdk.Common.Commands
{
    /// <summary>
    /// Abstraction for providing details about Visual Studio Commands
    /// </summary>
    public interface IVsCommandRepository
    {
        Task<string> GetCommandBindingAsync(string commandName);
    }
}
