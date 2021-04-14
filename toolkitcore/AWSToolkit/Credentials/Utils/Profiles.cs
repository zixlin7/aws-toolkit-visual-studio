using System.Collections.Generic;
using Amazon.Runtime.CredentialManagement;

namespace Amazon.AWSToolkit.Credentials.Utils
{
    public class Profiles
    {
       public  Dictionary<string, CredentialProfile> ValidProfiles = new Dictionary<string, CredentialProfile>();
       public  Dictionary<string, string> InvalidProfiles = new Dictionary<string, string>();
    }
}
