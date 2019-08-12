namespace Amazon.AWSToolkit.VisualStudio.Loggers
{
    /// <summary>
    /// Interface declaration for pluggable loggers into the build/deployment
    /// process.
    /// </summary>
    internal interface IBuildAndDeploymentLogger
    {
        /// <summary>
        /// Requests the host to display a message; the host window that
        /// receives the message is not necessarily made visible.
        /// </summary>
        /// <param name="message">Message to emit</param>
        void OutputMessage(string message);

        /// <summary>
        /// Requests the host to display a message; the host window that
        /// receives the message is not necessarily made visible.
        /// </summary>
        /// <param name="message">Message to emit</param>
        /// <param name="addToLog">If true, the message is also added to the toolkit's logfile</param>
        void OutputMessage(string message, bool addToLog);

        /// <summary>
        /// Requests the host to display a message and if possible make the
        /// window receiving the message visible, although this is not
        /// guaranteed.
        /// </summary>
        /// <param name="message">Message to emit</param>
        /// <param name="addToLog">If true, the message is also added to the toolkit's logfile</param>
        /// <param name="requestForceVisible">
        /// True to request that the host attempt tp force the final window containing 
        /// the message to be visible
        /// </param>
        void OutputMessage(string message, bool addToLog, bool requestForceVisible);

        /// <summary>
        /// Outputs a message to the log file only.
        /// </summary>
        /// <param name="message">Message to emit</param>
        void LogMessage(string message);
    }
}
