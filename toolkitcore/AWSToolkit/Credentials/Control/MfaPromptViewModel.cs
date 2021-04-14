using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.Credentials.Control
{
    /// <summary>
    /// Backing data for the MFA Login Prompt
    /// </summary>
    public class MfaPromptViewModel : BaseModel
    {
        private string _mfaToken;
        private string _mfaSerialNumber;
        private string _profileName;

        /// <summary>
        /// MFA Token needed for authentication
        /// </summary>
        public string MfaToken
        {
            get => _mfaToken;
            set
            {
                SetProperty(ref _mfaToken, value, () => MfaToken);
            }
        }

        /// <summary>
        /// Serial number of the MFA Device associated with the profile
        /// </summary>
        public string MfaSerialNumber
        {
            get => _mfaSerialNumber;
            set
            {
                SetProperty(ref _mfaSerialNumber, value, () => MfaSerialNumber);
            }
        }

        /// <summary>
        /// Name of the profile
        /// </summary>
        public string ProfileName
        {
            get => _profileName;
            set
            {
                SetProperty(ref _profileName, value, () => ProfileName);
            }
        }
    }
}
