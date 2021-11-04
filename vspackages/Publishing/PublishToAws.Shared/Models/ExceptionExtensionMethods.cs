using System;

using Amazon.AWSToolkit.Shared;

using log4net;

namespace Amazon.AWSToolkit.Publish.Models
{
    public static class ExceptionExtensionMethods
    {
        public static void OutputError(this IAWSToolkitShellProvider shellProvider, Exception exception, ILog logger)
        {
            logger.Error(exception?.Message, exception);
            shellProvider?.OutputToHostConsole(exception?.Message, true);
        }
    }
}
