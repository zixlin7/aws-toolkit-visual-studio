using System;

using Amazon.AWSToolkit.Shared;

using AWS.Deploy.ServerMode.Client;

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

        public static string GetExceptionInnerMessage(this Exception e)
        {
            switch (e)
            {
                case ApiException<ProblemDetails> detailsException:
                    return detailsException.Result.Detail;
                case ApiException apiException:
                    return apiException.Response;
                default:
                    return e.Message;
            }
        }
    }
}
