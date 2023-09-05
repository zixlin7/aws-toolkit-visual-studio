using System.Collections.Generic;
using Amazon.Runtime.CredentialManagement;

namespace Amazon.AWSToolkit.Credentials.IO
{
    /// <summary>
    /// Reader responsible for reading and determining all credential profiles present in a credential file
    /// </summary>
    public interface ICredentialFileReader
    {
        /// <summary>
        /// Represents the list of names of profiles present in the credential file
        /// </summary>
        IEnumerable<string> ProfileNames { get; }

        /// <summary>
        /// Loads the credential files and determines all profile names present in it
        /// </summary>
        void Load();

        /// <summary>
        /// Retrieves the <see cref="CredentialProfileOptions"/> associated with the given profile name
        /// </summary>
        /// <param name="profileName"></param>
        CredentialProfileOptions GetCredentialProfileOptions(string profileName);

        /// <summary>
        /// Retrieves the <see cref="CredentialProfile"/> associated with the given profile name
        /// </summary>
        /// <param name="profileName"></param>
        CredentialProfile GetCredentialProfile(string profileName);
    }
}
