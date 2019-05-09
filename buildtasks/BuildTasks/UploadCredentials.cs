using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using BuildCommon;

namespace BuildTasks
{
    public class BasicCredentials
    {
        public string Username { get; private set; }
        public string Password { get; private set; }

        public BasicCredentials(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }

    public static class UploadCredentials
    {
        public static AWSCredentials DefaultAWSCredentials
        {
            get { return FallbackCredentialsFactory.GetCredentials(); }
        }

        public static BasicCredentials GitAccessCredentials
        {
            get
            {
                return GetCredentials("GitAccessCredentials");
            }
        }

        public static string DefaultNugetAccessKey
        {
            get
            {
                var credentials = GetCredentials("NuGetCredentials");
                return credentials.Password;
            }
        }

        public static BasicCredentials XamarinAccessCredentials
        {
            get
            {
                return GetCredentials("XamarinCredentials");
            }
        }


        public static BasicCredentials GetCredentials(string id)
        {
            var credentials = TestCredentials.GetCredentials(id);
            return new BasicCredentials(credentials.AccessKey, credentials.SecretKey);
        }

        private static AWSCredentials AWSTestCredentials { get { return AWSCredentials("TestArtifactReleaseCredentials"); } }
        private static AWSCredentials AWSArtifactReleaseCredentials { get { return AWSCredentials("ArtifactReleaseCredentials"); } }

        public static AWSCredentials AWSCredentials(string id)
        {
            var credentials = TestCredentials.GetCredentials(id);
            if (credentials.SessionToken == null)
            {
                return new BasicAWSCredentials(credentials.AccessKey, credentials.SecretKey);
            }
            return new SessionAWSCredentials(credentials.AccessKey, credentials.SecretKey, credentials.SessionToken);
        }
    }

}
