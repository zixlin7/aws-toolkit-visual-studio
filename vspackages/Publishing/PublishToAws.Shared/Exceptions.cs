using System;

using AWS.Deploy.ServerMode.Client;

namespace Amazon.AWSToolkit.Publish
{
    /// <summary>
    /// High level exception indicating a problem in the Publish Experience.
    /// </summary>
    public class PublishException : Exception
    {
        public PublishException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Throw if an invalid Session Id is provided.
    /// </summary>
    public class InvalidSessionIdException : Exception
    {
        public InvalidSessionIdException(string message) : base(message) { }
    }

    public class InvalidApplicationNameException : Exception
    {
        public const string ErrorText = "Invalid cloud application name";

        public static bool TryCreate(ApiException<ProblemDetails> apiException,
            out InvalidApplicationNameException exception)
        {
            exception = null;

            try
            {
                if (apiException.StatusCode != 400)
                {
                    return false;
                }

                var detail = apiException.Result?.Detail;
                if (string.IsNullOrWhiteSpace(detail))
                {
                    return false;
                }

                if (!detail.Contains(ErrorText))
                {
                    return false;
                }

                exception = new InvalidApplicationNameException(detail, apiException);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public InvalidApplicationNameException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Throw if an invalid parameter is provided.
    /// </summary>
    public class InvalidParameterException : Exception
    {
        public InvalidParameterException(string message) : base(message) { }
    }

    /// <summary>
    /// Throw if an Option Setting Item Type is not supported.
    /// </summary>
    public class UnsupportedOptionSettingItemTypeException : Exception
    {
        public UnsupportedOptionSettingItemTypeException(string message) : base(message) { }
    }

    /// <summary>
    /// An Exception indicating a problem with the Session.
    /// </summary>
    public class SessionException : Exception
    {
        public SessionException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// An Exception indicating a problem from the DeployTool.
    /// </summary>
    public class DeployToolException : Exception
    {
        public DeployToolException(string message) : base(message) { }
        public DeployToolException(string message, Exception innerException) : base(message, innerException) { }
    }

}
