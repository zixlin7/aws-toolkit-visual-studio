using System;
using System.Collections.Generic;
using Amazon.Runtime.CredentialManagement;

namespace Amazon.AWSToolkit.Credentials.Utils
{
    /// <summary>
    /// Class responsible for holding a collection of credential profiles <see cref="CredentialProfile"/>
    /// </summary>
    public interface IProfileHolder
    {
        void UpdateProfiles(Dictionary<string, CredentialProfile> profiles);
        CredentialProfile GetProfile(string name);
        Dictionary<string, CredentialProfile> GetCurrentProfiles();
    }
}
