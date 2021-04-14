using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.Credentials.Core;

namespace Amazon.AWSToolkit.Credentials.Utils
{

    /// <summary>
    /// Event arguments that indicates that credentials were manipulated in a way that the toolkit needs
    /// to be notified so state can be updated to give an accurate representation of the state of the credentials system
    /// </summary>
    public class CredentialChangeEventArgs: EventArgs
    {
        public List<ICredentialIdentifier> Added { get; set; }
        public List<ICredentialIdentifier> Removed { get; set; }
        public List<ICredentialIdentifier> Modified { get; set; }
    }
}
