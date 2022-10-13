namespace Amazon.AWSToolkit.Credentials.Core
{
    /// <summary>
    /// Different ways the Toolkit can connect to AWS
    /// </summary>
    public enum AwsConnectionType
    {
        /// <summary>
        /// Connections commonly made with <see cref="Amazon.Runtime.AWSCredentials"/>
        /// from the AWS .NET SDK.
        /// </summary>
        AwsCredentials,

        /// <summary>
        /// Bearer token
        /// </summary>
        AwsToken,
    }
}
