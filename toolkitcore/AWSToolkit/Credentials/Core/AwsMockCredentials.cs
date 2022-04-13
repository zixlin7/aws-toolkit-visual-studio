using Amazon.Runtime;

namespace Amazon.AWSToolkit.Credentials.Core
{
    public class AwsMockCredentials : AWSCredentials
    {
        private readonly ImmutableCredentials _mockCredentials;

        public AwsMockCredentials()
        {
            _mockCredentials = new ImmutableCredentials("accessKey", "secretKey", "");
        }

        public override ImmutableCredentials GetCredentials()
        {
            return _mockCredentials;
        }
    }
}
