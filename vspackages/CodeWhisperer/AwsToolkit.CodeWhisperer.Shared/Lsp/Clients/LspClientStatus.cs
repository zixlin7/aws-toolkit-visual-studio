namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients
{
    public enum LspClientStatus
    {
        /// <summary>
        /// The client is not running (and the Toolkit has never attempted to run it).
        /// The client is also not running if the status is <see cref="Error"/>.
        /// The difference is that NotRunning is an expected starting state for the system.
        /// </summary>
        NotRunning,
        /// <summary>
        /// The client:
        /// - is being installed
        /// - is running but has not been initialized yet
        /// </summary>
        SettingUp,
        /// <summary>
        /// The client is running and has been initialized
        /// </summary>
        Running,
        /// <summary>
        /// The client has stopped (or was not launched) due to an error
        /// </summary>
        Error,
    }
}
