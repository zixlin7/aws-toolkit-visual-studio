using System.ComponentModel.Design;

using Microsoft.VisualStudio.Shell;

using Task = System.Threading.Tasks.Task;

namespace Amazon.AwsToolkit.VsSdk.Common.Services
{
    /// <summary>
    /// Allows us to dependency inject services into commands.
    /// This is primarily intended for use with MEF components, which
    /// can be activated independently from the main AWS Toolkit Package.
    /// </summary>
    /// <example>
    /// Injecting the OleMenuCommandService into a MEF component for command registration
    /// </example>
    public interface IPluginCommand
    {
        Task RegisterAsync(IAsyncServiceProvider service);
    }
}
