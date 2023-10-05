namespace Amazon.AWSToolkit.Credentials.Utils
{
    /// <summary>
    /// Class representing properties associated with a credential profile
    /// </summary>
    public class ProfileProperties
    {
        public string Name { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string Token { get; set; }
        public string CredentialProcess { get; set; }
        public string RoleArn { get; set; }
        public string MfaSerial { get; set; }
        public string EndpointName { get; set; }
        public string CredentialSource { get; set; }
        public string SourceProfile { get; set; }

        /// <summary>
        /// The unique key for this CredentialProfile.
        /// This key is used by the toolkit to associate external artifacts with this profile.
        /// </summary>
        public string UniqueKey { get; set; }
        public string Region { get; set; }

        public string SsoSession { get; set; }
        public string SsoAccountId { get; set; }
        public string SsoRegion { get; set; }
        public string SsoRoleName { get; set; }
        public string SsoStartUrl { get; set; }

        public ProfileProperties ShallowClone()
        {
            return MemberwiseClone() as ProfileProperties;
        }
    }
}
