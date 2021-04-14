using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Settings;

namespace Amazon.AWSToolkit.Credentials.IO
{
    public class PersistConnectionSettings : IDisposable
    {
        private readonly IAwsConnectionManager _awsConnectionManager;

        public PersistConnectionSettings(IAwsConnectionManager awsConnectionManager)
        {
            _awsConnectionManager = awsConnectionManager;
            _awsConnectionManager.ConnectionSettingsChanged += OnConnectionSettingsChanged;
        }

        private void OnConnectionSettingsChanged(object sender, ConnectionSettingsChangeArgs e)
        {
            if (e.Region != null)
            {
                ToolkitSettings.Instance.LastSelectedRegion = e.Region?.Id;
            }
            ToolkitSettings.Instance.LastSelectedCredentialId = e.CredentialIdentifier?.Id;
        }

        public void Dispose()
        {
            _awsConnectionManager.ConnectionSettingsChanged -= OnConnectionSettingsChanged;
        }
    }
}
