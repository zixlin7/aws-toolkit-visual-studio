using Amazon.AWSToolkit.Credentials.Utils;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles
{
    public partial class StaticProfileSubform : ProfileSubform
    {
        public StaticProfileSubform()
        {
            InitializeComponent();
        }

        public override CredentialType CredentialType => CredentialType.StaticProfile;
    }
}
