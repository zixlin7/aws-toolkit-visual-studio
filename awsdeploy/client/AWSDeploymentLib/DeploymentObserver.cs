namespace AWSDeployment
{
    /// <summary>
    /// Simple deployment observer. Drops all messages on the floor. This class is
    /// intended to be overridden, but can be partially overridden if need be.
    /// </summary>
    public class DeploymentObserver
    {
        // Status and Progress are for UI messaging and do not emit to the logfile
        public virtual void Status(string messageFormat, params object[] list) { }
        public virtual void Progress(string messageFormat, params object[] list) { }

        // Info, Warn and Error methods are also expected to write to logfile, if 
        // turned on
        public virtual void Info(string messageFormat, params object[] list) { }
        public virtual void Warn(string messageFormat, params object[] list) { }
        public virtual void Error(string messageFormat, params object[] list) { }

        // for observers attached to log files, outputs the message to the backing
        // logfile only - useful for thread messaging where to output to a console
        // puts messages out of 'flow'
        public virtual void LogOnly(string messageFormat, params object[] list) { }
    }
}
