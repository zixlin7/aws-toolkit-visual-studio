using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.Credentials.Utils
{
    /// <summary>
    /// Event arguments that indicate active toolkit connection settings- identifier and region have changed
    /// </summary>
    public class ConnectionSettingsChangeArgs : EventArgs
    {
        public ICredentialIdentifier CredentialIdentifier { get; set; }
        public ToolkitRegion Region { get; set; }
    }
}
