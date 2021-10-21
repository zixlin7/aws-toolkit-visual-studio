using log4net;

namespace Amazon.AWSToolkit.Publish.Install
{
    /// <summary>
    /// Logs a process's data i.e. standard output and error
    /// </summary>
    public class ProcessLogger
    {
        private readonly ProcessData _processData;
        private readonly ILog _logger;

        public ProcessLogger(ProcessData processData, ILog logger)
        {
            _processData = processData;
            _logger = logger;
        }

        public void Record()
        {
            Log(_processData.Output);
            Log(_processData.Error);
        }

        private void Log(string data)
        {
            if (!string.IsNullOrWhiteSpace(data))
            {
                _logger.Debug(data);
            }
        }
    }
}
