using System;
using System.ComponentModel;

using log4net;

namespace Amazon.AWSToolkit.Events
{
    /// <summary>
    /// ChangedEventHandler decorator that logs and swallows any Exception thrown.
    /// </summary>
    public class LogExceptionAndForgetChangedHandler
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(LogExceptionAndForgetChangedHandler));

        public static PropertyChangedEventHandler Create(PropertyChangedEventHandler handler)
        {
            return (sender, args) =>
            {
                try
                {
                    handler.Invoke(sender, args);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            };
        }
    }
}
