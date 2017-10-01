using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Runtime;

namespace Amazon.ECS.Tools
{
    /// <summary>
    /// The deploy tool exception. This is used to throw back an error to the user but is considerd a known error
    /// so the stack trace will not be displayed.
    /// </summary>
    public class DockerToolsException : Exception
    {
        public enum ErrorCode {

            DefaultsParseFail,
            CommandLineParseError,
            ProfileNotFound,
            RegionNotConfigured,
            MissingRequiredParameter,

            DotnetPublishFailed,
            DockerBuildFailed,

            FailedToSetupECRRepository,
            GetECRAuthTokens,
            DockerCLILoginFail,
            DockerTagFail,
            DockerPushFail,

            FailedToUpdateTaskDefinition,
            FailedToExpandImageTag,
            FailedToUpdateService,
            ClusterNotFound,

        }

        public DockerToolsException(string message, ErrorCode code) : base(message)
        {
            this.Code = code;
        }

        public DockerToolsException(string message, ErrorCode code, Exception e) : this(message, code)
        {
            var ae = e as AmazonServiceException;
            if (ae != null)
            {
                this.ServiceCode = $"{ae.ErrorCode}-{ae.StatusCode}";
            }
        }

        public ErrorCode Code { get; }

        public string ServiceCode { get; }
    }
}
