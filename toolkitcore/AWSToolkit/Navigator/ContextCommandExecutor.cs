using System;
using Amazon.AWSToolkit.Navigator.Node;
using log4net;

namespace Amazon.AWSToolkit.Navigator
{
    /// <summary>
    /// Executes <see cref="IContextCommand"/> commands on the UI Thread
    /// </summary>
    public class ContextCommandExecutor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ContextCommandExecutor));

        public delegate IContextCommand CreateCommand();

        private readonly CreateCommand _commandProducer;

        public ContextCommandExecutor(CreateCommand commandProducer)
        {
            _commandProducer = commandProducer;
        }

        public ActionResults Execute(IViewModel viewModel)
        {
            try
            {
                ActionResults results = null;
                ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread(() =>
                {
                    var command = _commandProducer();
                    results = command.Execute(viewModel);
                });
                return results;
            }
            catch (Exception e)
            {
                Logger.Error("Failed to run context command", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Unknown Error: " + e.Message);
                return new ActionResults().WithSuccess(false);
            }
        }
    }
}
