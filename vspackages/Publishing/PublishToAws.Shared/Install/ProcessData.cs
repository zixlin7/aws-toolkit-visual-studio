namespace Amazon.AWSToolkit.Publish.Install
{
    /// <summary>
    /// Represents Process data such as standard output and standard error
    /// </summary>
    public class ProcessData
    {
        public string Output { get; }
        public string Error { get; }

        public ProcessData(string output, string error)
        {
            Output = output;
            Error = error;
        }
    }
}
