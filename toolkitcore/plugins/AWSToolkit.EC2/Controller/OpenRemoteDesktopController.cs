using System;
using System.Collections.Generic;
using System.Windows;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.EC2.ConnectionUtils;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Telemetry;
using Amazon.AWSToolkit.Util;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Runtime.Internal.Settings;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class OpenRemoteDesktopController
    {
        private readonly ToolkitContext _toolkitContext;

        private ActionResults _results;
        private IAmazonEC2 _ec2Client;
        private RunningInstanceWrapper _instance;
        private OpenRemoteDesktopModel _model;
        private string _uniqueKey;
        private bool _usingStoredPrivateKey;
        private AwsConnectionSettings _connectionSettings;

        public OpenRemoteDesktopController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public ActionResults Execute(AwsConnectionSettings connectionSettings, RunningInstanceWrapper instance)
        {
            _connectionSettings = connectionSettings;
            _uniqueKey = _toolkitContext.CredentialSettingsManager.GetUniqueKey(_connectionSettings.CredentialIdentifier);
            ActionResults actionResults = null;

            void Invoke()
            {
                actionResults = OpenConnection(connectionSettings.CredentialIdentifier, instance);
            }

            void Record(ITelemetryLogger telemetryLogger)
            {
                RecordRemoteConnection(telemetryLogger, actionResults.AsTelemetryResult());
            }

            _toolkitContext.TelemetryLogger.InvokeAndRecord(Invoke, Record);
            return actionResults;
        }

        private ActionResults OpenConnection(ICredentialIdentifier credentialIdentifier, RunningInstanceWrapper instance)
        {
            if (!instance.HasPublicAddress)
            {
                if (!_toolkitContext.ToolkitHost.Confirm("Instance IP Address", EC2Constants.NO_PUBLIC_IP_CONFIRM_CONNECT_PRIVATE_IP))
                {
                    return new ActionResults().WithSuccess(false);
                }
            }

            _ec2Client = _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonEC2Client>(credentialIdentifier, _connectionSettings.Region);
            _instance = instance;
            _model = new OpenRemoteDesktopModel(instance);

            if (KeyPairLocalStoreManager.Instance.DoesPrivateKeyExist(_uniqueKey, _connectionSettings.Region.Id, _instance.NativeInstance.KeyName))
            {
                _model.PrivateKey = KeyPairLocalStoreManager.Instance.GetPrivateKey(_uniqueKey, _connectionSettings.Region.Id, _instance.NativeInstance.KeyName);
                _usingStoredPrivateKey = true;
            }

            loadLastSelectedValues(out var useKeyPair, out var password);

            var dlgResult = _toolkitContext.ToolkitHost.ShowModal(new OpenRemoteDesktopControl(this, useKeyPair, password));

            var actionResult = _results != null;

            return new ActionResults()
                .WithCancelled(!dlgResult)
                .WithSuccess(actionResult);
        }

        public string InstanceId => _instance.InstanceId;

        public OpenRemoteDesktopModel Model => _model;

        public void OpenRemoteDesktopWithCredentials(string username, string password)
        {
            checkIfOpenPort();

            RemoteDesktopUtil.Connect(_instance.ConnectName, username, password, _model.MapDrives);

            this._results = new ActionResults().WithSuccess(true);
            persistLastSelectedValues(false, password);
        }

        public void OpenRemoteDesktopWithEC2KeyPair()
        {
            var request = new GetPasswordDataRequest() { InstanceId = _instance.NativeInstance.InstanceId };
            var response = _ec2Client.GetPasswordData(request);

            if (!GetPasswordController.ValidateEncryptedPassword(_instance, response))
            {
                return;
            }

            string decryptedPassword = response.GetDecryptedPassword(_model.PrivateKey);

            OpenRemoteDesktopWithCredentials(EC2Constants.DEFAULT_ADMIN_USER, decryptedPassword);
            persistLastSelectedValues(true, null);
        }

        private bool checkIfOpenPort()
        {
            bool isOpen = EC2Utilities.checkIfPortOpen(_ec2Client, _instance.NativeInstance.SecurityGroups, NetworkProtocol.RDP.DefaultPort.Value);

            if (!isOpen)
            {
                if (_instance.NativeInstance.SecurityGroups.Count == 1)
                {
                    var externalIpAddress = IPAddressUtil.DetermineIPFromExternalSource();
                    if (externalIpAddress != null)
                        externalIpAddress += "/32";

                    string msg = string.Format("The security group \"{0}\" used for this instance does not have remote desktop port open, " +
                        "would you like to open the remote desktop port?", _instance.SecurityGroups);

                    var control = new AskToOpenPort(msg, externalIpAddress);

                    if (!isOpen && _toolkitContext.ToolkitHost.ShowModal(control, MessageBoxButton.OKCancel))
                    {
                        var request = new AuthorizeSecurityGroupIngressRequest() { GroupId = _instance.NativeInstance.SecurityGroups[0].GroupId };

                        IpPermission permission = new IpPermission()
                        {
                            Ipv4Ranges = new List<IpRange> { new IpRange { CidrIp = control.IPAddress } },
                            IpProtocol = NetworkProtocol.RDP.UnderlyingProtocol.ToString().ToLower(),
                            FromPort = NetworkProtocol.RDP.DefaultPort.GetValueOrDefault(),
                            ToPort = NetworkProtocol.RDP.DefaultPort.GetValueOrDefault()
                        };
                        request.IpPermissions = new List<IpPermission> { permission };

                        _ec2Client.AuthorizeSecurityGroupIngress(request);
                        return true;
                    }
                }
                else
                {
                    _toolkitContext.ToolkitHost.ShowError(
                        string.Format("Port {0} is restricted in all security groups associated with this instance.  " +
                        "To connect to this instance allow access to port {0} in one of the associated security groups.", NetworkProtocol.RDP.DefaultPort.Value));
                    return false;
                }
            }

            return isOpen;
        }

        private void persistLastSelectedValues(bool useKeyPair, string password)
        {
            var settings = PersistenceManager.Instance.GetSettings(ToolkitSettingsConstants.EC2ConnectSettings);
            var os = settings[InstanceId];

            os[ToolkitSettingsConstants.EC2InstanceUseKeyPair] = useKeyPair.ToString();
            os[ToolkitSettingsConstants.EC2InstanceMapDrives] = _model.MapDrives.ToString();
            os[ToolkitSettingsConstants.EC2InstanceSaveCredentials] = _model.SaveCredentials.ToString();

            if (useKeyPair || !_model.SaveCredentials)
            {
                os[ToolkitSettingsConstants.EC2InstanceUserName] = null;
                os[ToolkitSettingsConstants.EC2InstancePassword] = null;
            }
            else
            {
                os[ToolkitSettingsConstants.EC2InstanceUserName] = _model.EnteredUsername;
                os[ToolkitSettingsConstants.EC2InstancePassword] = password;
            }

            PersistenceManager.Instance.SaveSettings(ToolkitSettingsConstants.EC2ConnectSettings, settings);

            if (string.IsNullOrEmpty(password) && !_usingStoredPrivateKey && Model.SavePrivateKey)
            {
                KeyPairLocalStoreManager.Instance.SavePrivateKey(_uniqueKey, _connectionSettings.Region.Id, _instance.NativeInstance.KeyName, Model.PrivateKey);
            }
        }

        private void loadLastSelectedValues(out bool useKeyPair, out string password)
        {
            var settings = PersistenceManager.Instance.GetSettings(ToolkitSettingsConstants.EC2ConnectSettings);
            var os = settings[InstanceId];

            useKeyPair = Convert.ToBoolean(os.GetValueOrDefault(ToolkitSettingsConstants.EC2InstanceUseKeyPair, true.ToString()));
            password = os[ToolkitSettingsConstants.EC2InstancePassword];

            _model.MapDrives = Convert.ToBoolean(os.GetValueOrDefault(ToolkitSettingsConstants.EC2InstanceMapDrives, true.ToString()));
            _model.SaveCredentials = Convert.ToBoolean(os.GetValueOrDefault(ToolkitSettingsConstants.EC2InstanceSaveCredentials, true.ToString()));
            _model.EnteredUsername = os[ToolkitSettingsConstants.EC2InstanceUserName];
        }

        private void RecordRemoteConnection(ITelemetryLogger telemetryLogger, Result result)
        {
            var accountId = _toolkitContext.ServiceClientManager.GetAccountId(_connectionSettings) ??
                            MetadataValue.Invalid;

            telemetryLogger.RecordEc2ConnectToInstance(new Ec2ConnectToInstance()
            {
                Result = result,
                Ec2ConnectionType = Ec2ConnectionType.RemoteDesktop,
                AwsAccount = accountId,
                AwsRegion = _connectionSettings.Region?.Id ?? MetadataValue.NotSet,
            });
        }
    }
}
