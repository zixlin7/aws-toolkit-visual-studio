using Amazon.AWSToolkit.Credentials.Utils;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles
{
    public partial class SsoProfileSubform : ProfileSubform
    {
        public static readonly string StartUrlHintText = $"https://<YOUR_SUBDOMAIN>.awsapps.com/start";
        public SsoProfileSubform()
        {
            InitializeComponent();
        }

        public override CredentialType CredentialType => CredentialType.SsoProfile;
    }
}
