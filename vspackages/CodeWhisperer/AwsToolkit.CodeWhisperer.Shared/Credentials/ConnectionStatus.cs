namespace Amazon.AwsToolkit.CodeWhisperer.Credentials
{
    /// <summary>
    /// Represents the different CodeWhisperer Credentials connection states
    /// </summary>
    public enum ConnectionStatus
    {
        /// <summary>
        /// User is signed in with valid credentials
        /// </summary>
        Connected,
        /// <summary>
        /// User's credentials have expired. User is no longer connected.
        /// <see cref="Connection"/> will auto-progress the state to <see cref="Disconnected"/>
        /// </summary>
        Expired,
        /// <summary>
        /// User is not signed in.
        /// </summary>
        Disconnected,
    }
}
