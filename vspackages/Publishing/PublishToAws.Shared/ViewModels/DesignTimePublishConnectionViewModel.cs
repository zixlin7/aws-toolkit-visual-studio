using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.Publish.ViewModels
{
    /// <summary>
    /// You can adjust these values and recompile the project to see how it looks in
    /// CredentialsRegionControl at design-time.
    /// </summary>
    public class DesignTimePublishConnectionViewModel : PublishConnectionViewModel
    {
        public DesignTimePublishConnectionViewModel() : base(null, null)
        {
            ConnectionStatus = ConnectionStatus.Bad;
            StatusMessage = "Sample validation failure message";
            CredentialsId = new SharedCredentialIdentifier("sample-profile");
            Region = new ToolkitRegion() { DisplayName = "some-region" };
        }
    }
}
