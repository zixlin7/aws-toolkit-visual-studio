using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AwsToolkit.VsSdk.Common.Tasks;

using EnvDTE;

using Microsoft;

namespace Amazon.AwsToolkit.VsSdk.Common.Commands
{
    /// <summary>
    /// Toolkit implementation for providing details about Visual Studio Commands
    /// </summary>
    public class ToolkitVsCommandRepository : IVsCommandRepository
    {
        private readonly ToolkitJoinableTaskFactoryProvider _taskFactoryProvider;
        private readonly IServiceProvider _serviceProvider;

        public ToolkitVsCommandRepository(IServiceProvider serviceProvider, ToolkitJoinableTaskFactoryProvider taskFactoryProvider)
        {
            _serviceProvider = serviceProvider;
            _taskFactoryProvider = taskFactoryProvider;
        }

        public async Task<string> GetCommandBindingAsync(string commandName)
        {
            await _taskFactoryProvider.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = (DTE) _serviceProvider.GetService(typeof(DTE));

            Assumes.Present(dte);

            var binding = ((IEnumerable) dte.Commands.Item(commandName).Bindings)
                .Cast<object>()
                .ToList()
                .LastOrDefault()?
                .ToString();

            return KeyBindingUtilities.FormatKeyBindingDisplayText(binding);
        }
    }
}
