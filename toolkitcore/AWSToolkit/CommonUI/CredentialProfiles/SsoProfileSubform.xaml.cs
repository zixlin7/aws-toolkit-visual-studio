using Amazon.AWSToolkit.Credentials.Utils;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles
{
    public partial class SsoProfileSubform : ProfileSubform
    {
        public SsoProfileSubform()
        {
            InitializeComponent();
        }

        public override CredentialType CredentialType => CredentialType.SsoProfile;
    }
}
