using System;

using Amazon.AWSToolkit.Shared;

using log4net;

namespace Amazon.AWSToolkit.Navigator
{
    /// <summary>
    /// Instantiates and executes <see cref="IConnectionContextCommand"/> commands on the UI Thread
    /// </summary>
    public class ConnectionContextCommandExecutor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ConnectionContextCommandExecutor));

        public delegate IConnectionContextCommand CreateCommand();

        private readonly CreateCommand _commandProducer;
        private readonly IAWSToolkitShellProvider _shellProvider;

        public ConnectionContextCommandExecutor(CreateCommand commandProducer, IAWSToolkitShellProvider shellProvider)
        {
            _commandProducer = commandProducer;
            _shellProvider = shellProvider;
        }

        public ActionResults Execute()
        {
            try
            {
                ActionResults results = null;
                _shellProvider.ExecuteOnUIThread(() =>
                {
                    var command = _commandProducer();
                    results = command.Execute();
                });
                return results;
            }
            catch (Exception e)
            {
                Logger.Error("Failed to run context command", e);
                _shellProvider.ShowError("Error running command",
                    $"Unable to perform command:{Environment.NewLine}{Environment.NewLine}{e.Message}");
                return new ActionResults().WithSuccess(false);
            }
        }
    }
}
