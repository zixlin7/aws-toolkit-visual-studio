using System;
using System.Linq;

using Amazon.AWSToolkit.Exceptions;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.Common.DotNetCli.Tools;
using Amazon.Runtime;

namespace Amazon.AWSToolkit.Telemetry
{
    /// <summary>
    /// Misc Telemetry related functions
    /// </summary>
    public static class TelemetryHelper
    {
        public const string UnknownReason = "Unknown";

        /// <summary> 
        /// Returns an error metadata object by extracting details from the exception
        /// Not intended to surface user-identifiable details.
        /// </summary>
        /// <param name="ex">Exception to extract error details from</param>
        public static ErrorMetadata DetermineErrorMetadata(Exception ex)
        {
            return DetermineErrorMetadata(ex, CausedBy.Unknown);
        }

        /// <summary>
        /// Overload determining error metadata from the exception while using the provided causedBy value
        /// </summary>
        public static ErrorMetadata DetermineErrorMetadata(Exception ex, CausedBy causedBy)
        {
            var errorMetadata = CreateErrorMetadata(ex, causedBy);
            if (errorMetadata == null)
            {
                return new ErrorMetadata { Reason = UnknownReason };
            }

            // only traverses exception one level down from outermost exception
            AddInfoFromInnerException(errorMetadata, ex.InnerException);
            return errorMetadata;
        }


        /// <summary>
        /// Creates the error metadata object by extracting details from known exception types
        /// </summary>
        private static ErrorMetadata CreateErrorMetadata(Exception ex, CausedBy causedBy, bool isInnerException = false)
        {
            if (ex == null)
            {
                return null;
            }

            var errorMetadata = new ErrorMetadata { CausedBy = causedBy, Reason = ex.GetType().Name };

            switch (ex)
            {
                case AmazonServiceException awsException:
                    errorMetadata.ErrorCode = awsException.ErrorCode;
                    errorMetadata.CausedBy = DetermineCausedBy(awsException.ErrorType);
                    errorMetadata.RequestId = awsException.RequestId;
                    errorMetadata.HttpStatusCode = awsException.StatusCode.ToString();
                    errorMetadata.RequestServiceType = awsException.GetServiceName();
                    break;

                case ToolkitException toolkitException:
                    errorMetadata.ErrorCode = isInnerException
                        ? ConcatenateFragments(toolkitException.Code, toolkitException.ServiceErrorCode)
                        : toolkitException.Code;
                    errorMetadata.HttpStatusCode = toolkitException.ServiceStatusCode;
                    break;

                case ToolsException toolsException:
                    errorMetadata.ErrorCode = isInnerException
                        ? ConcatenateFragments(toolsException.Code, GetServiceCode(toolsException.ServiceCode))
                        : toolsException.Code;
                    errorMetadata.HttpStatusCode = GetServiceStatusCode(toolsException.ServiceCode);
                    break;

                case SystemException systemException:
                    errorMetadata.ErrorCode = systemException.HResult.ToString();
                    break;

                // TODO: Introduce handling for Publish to AWS exceptions as a separate story because it has a different model for error handling    
            }

            return errorMetadata;
        }

        public static void AddInfoFromInnerException(ErrorMetadata errorMetadata, Exception exception)
        {
            var innerErrorData = CreateErrorMetadata(exception, CausedBy.Unknown, true);
            if (innerErrorData == null)
            {
                return;
            }

            // add info from inner exception to unpopulated fields of the metadata object
            foreach (var propertyInfo in errorMetadata.GetType().GetProperties())
            {
                if (propertyInfo.GetValue(errorMetadata) == null ||
                    string.IsNullOrEmpty(propertyInfo.GetValue(errorMetadata).ToString()))
                {
                    propertyInfo.SetValue(errorMetadata, propertyInfo.GetValue(innerErrorData));
                }
            }

            // update caused by if not unknown for inner exception
            if (!string.Equals(innerErrorData.CausedBy, CausedBy.Unknown))
            {
                errorMetadata.CausedBy = innerErrorData.CausedBy;
            }

            // concatenate reason and error code from inner exception
            errorMetadata.Reason = ConcatenateFragments(errorMetadata.Reason, innerErrorData.Reason);
            errorMetadata.ErrorCode = ConcatenateFragments(errorMetadata.ErrorCode, innerErrorData.ErrorCode);
        }

        /// <summary>
        /// Parses and returns service status code from <see cref="ToolsException"/> ServiceCode property.
        /// Represented in the format ServiceCode = $"{ae.ErrorCode}-{ae.StatusCode}";
        /// </summary>
        private static string GetServiceStatusCode(string serviceCode)
        {
            if (string.IsNullOrEmpty(serviceCode))
            {
                return string.Empty;
            }

            var position = serviceCode.IndexOf('-');
            return position == -1 ? null : serviceCode.Substring(position + 1);
        }

        /// <summary>
        /// Parses and returns service error code from <see cref="ToolsException"/> ServiceCode property.
        /// Represented in the format ServiceCode = $"{ae.ErrorCode}-{ae.StatusCode}";
        /// </summary>
        private static string GetServiceCode(string serviceCode)
        {
            if (string.IsNullOrEmpty(serviceCode))
            {
                return string.Empty;
            }

            var position = serviceCode.IndexOf('-');
            return position == -1 ? null : serviceCode.Substring(0, position);
        }

        private static CausedBy DetermineCausedBy(ErrorType errorType)
        {
            return errorType == ErrorType.Receiver ? CausedBy.Service : CausedBy.Unknown;
        }

        private static string ConcatenateFragments(params string[] fragments)
        {
            return string.Join("-", fragments.Where(r => !string.IsNullOrWhiteSpace(r)));
        }
    }
}
