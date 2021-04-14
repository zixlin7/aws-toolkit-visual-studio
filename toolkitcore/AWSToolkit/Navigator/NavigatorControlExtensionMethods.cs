using System;
using System.Threading;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;

using log4net;

namespace Amazon.AWSToolkit.Navigator
{
    /// <summary>
    /// Helper class for syncing up the deploy wizard's connection settings with the navigator/aws explorer
    /// </summary>
    public static class NavigatorControlExtensionMethods
    {
        private const int ConnectionValidationTimeout= 4000;
        private static readonly ILog LOGGER = LogManager.GetLogger(typeof(NavigatorControlExtensionMethods));

        /// <summary>
        /// Updates navigator connection settings if they are different from the deploy wizards
        /// returns true if the connection is valid,
        /// returns false if it is not valid or the account has not yet updated after timeout
        /// </summary>
        /// <param name="navigator"><see cref="NavigatorControl"/></param>
        /// <param name="connectionManager"><see cref="IAwsConnectionManager"/></param>
        /// <param name="account">the deploy wizard's selected account</param>
        /// <param name="region">the deploy wizard's selected region</param>
        public static bool TryWaitForSelection(this NavigatorControl navigator, IAwsConnectionManager connectionManager, AccountViewModel account, ToolkitRegion region)
        {
            var isValid = false;
            var connectionStateIsTerminalEvent = new ManualResetEvent(false);

            void CheckTerminalState(object sender, ConnectionStateChangeArgs args)
            {
                if (!(sender is IAwsConnectionManager awsConnectionManager))
                {
                    throw new ArgumentException("Sender object for connection state change is not of type IAwsConnectionManager");
                }
                if (awsConnectionManager.ConnectionState.IsTerminal)
                {
                    connectionStateIsTerminalEvent.Set();
                }
            }

            try
            {
                connectionManager.ConnectionStateChanged += CheckTerminalState;
                connectionStateIsTerminalEvent.Reset();

                //check if selected account/regions with the deploy wizard are different from the explorer,
                //if yes, update the connection manager
                if (navigator.SelectedAccount != account || navigator.SelectedRegion != region)
                {
                    ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action) (() =>
                    {
                        connectionManager.ChangeConnectionSettings(account?.Identifier,
                            region);
                    }));

                    //give sufficient time for the changes to propagate, else exit gracefully
                    connectionStateIsTerminalEvent.WaitOne(ConnectionValidationTimeout);

                    if (!connectionManager.ConnectionState.IsTerminal || !string.Equals(
                        navigator.SelectedAccount?.Identifier?.Id, account?.Identifier?.Id))
                    {
                        LOGGER.Error($"Threshold exceeded waiting for connection validation of: {account} and {region}");
                    }
                    else
                    {
                        isValid = true;
                        LOGGER.Info("Connection validation successful.");
                    }
                }
                else
                {
                    isValid = true;
                    LOGGER.Info("Connection settings are already valid.");
                }
            }
            catch(Exception e)
            {
                LOGGER.Error("Unexpected error occurred while updating connection settings", e);
            }
            finally
            {
                connectionManager.ConnectionStateChanged -= CheckTerminalState;
            }
            return isValid;
        }
    }
}
