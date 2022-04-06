using System;
using System.Collections.Generic;
using System.Windows;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.EC2.ConnectionUtils;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Util;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Runtime.Internal.Settings;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public abstract class OpenLinuxToolController
    {
        protected ActionResults _results;
        protected IAmazonEC2 _ec2Client;
        protected RunningInstanceWrapper _instance;
        protected OpenLinuxToolModel _model;
        protected readonly ToolkitContext _toolkitContext;
        protected AwsConnectionSettings _connectionSettings;
        protected string _settingsUniqueKey;
        private bool _usingStoredPrivateKey;

        public OpenLinuxToolController(ToolkitContext toolkitContext)
{
            _toolkitContext = toolkitContext;
        }

        public abstract string Executable
        {
            get;
        }

        public abstract string ToolSearchFolders
        {
            get;
        }

        public OpenLinuxToolModel Model => this._model;

        public string InstanceId => this._instance.InstanceId;

        public abstract OpenLinuxToolControl CreateControl(bool useKeyPair, string password);

        public ActionResults Execute(AwsConnectionSettings connectionSettings, string settingsUniqueKey, RunningInstanceWrapper instance)
        {
            _connectionSettings = connectionSettings;
            _settingsUniqueKey = settingsUniqueKey;

            if (!instance.HasPublicAddress)
            {
                if (!_toolkitContext.ToolkitHost.Confirm("Instance IP Address", EC2Constants.NO_PUBLIC_IP_CONFIRM_CONNECT_PRIVATE_IP))
                {
                    return new ActionResults().WithSuccess(false);
                }
            }

            _ec2Client = _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonEC2Client>(_connectionSettings.CredentialIdentifier, _connectionSettings.Region);
            _instance = instance;
            _model = new OpenLinuxToolModel(instance);
            _model.ToolLocation = ToolsUtil.FindTool(Executable, ToolSearchFolders);

            if (KeyPairLocalStoreManager.Instance.DoesPrivateKeyExist(_settingsUniqueKey, _connectionSettings.Region.Id, _instance.NativeInstance.KeyName))
            {
                _model.PrivateKey = KeyPairLocalStoreManager.Instance.GetPrivateKey(_settingsUniqueKey, _connectionSettings.Region.Id, _instance.NativeInstance.KeyName);
                _usingStoredPrivateKey = true;
            }

            bool useKeyPair;
            string password;
            loadLastSelectedValues(out useKeyPair, out password);
            _toolkitContext.ToolkitHost.ShowModal(CreateControl(useKeyPair, password));

            if (_results == null)
                return new ActionResults().WithSuccess(false);

            return new ActionResults().WithSuccess(true);
        }

        protected bool CheckIfOpenPort()
        {
            bool isOpen = EC2Utilities.checkIfPortOpen(this._ec2Client, this._instance.NativeInstance.SecurityGroups, NetworkProtocol.SSH.DefaultPort.Value);

            if (!isOpen)
            {
                if (this._instance.NativeInstance.SecurityGroups.Count == 1)
                {
                    var externalIpAddress = IPAddressUtil.DetermineIPFromExternalSource();
                    if (externalIpAddress != null)
                        externalIpAddress += "/32";

                    string msg = string.Format("The security group \"{0}\" used for this instance does not have SSH port open, " +
                        "would you like to open the SSH port?", this._instance.SecurityGroups);

                    var control = new AskToOpenPort(msg, externalIpAddress);

                    if (!isOpen && _toolkitContext.ToolkitHost.ShowModal(control, MessageBoxButton.OKCancel))
                    {
                        var request = new AuthorizeSecurityGroupIngressRequest() { GroupId = this._instance.NativeInstance.SecurityGroups[0].GroupId };

                        IpPermission permission = new IpPermission()
                        {
                            Ipv4Ranges = new List<IpRange> { new IpRange { CidrIp = control.IPAddress } },
                            IpProtocol = NetworkProtocol.SSH.UnderlyingProtocol.ToString().ToLower(),
                            FromPort = NetworkProtocol.SSH.DefaultPort.GetValueOrDefault(),
                            ToPort = NetworkProtocol.SSH.DefaultPort.GetValueOrDefault()
                        };
                        request.IpPermissions = new List<IpPermission> { permission };

                        this._ec2Client.AuthorizeSecurityGroupIngress(request);
                        return true;
                    }
                }
                else
                {
                    _toolkitContext.ToolkitHost.ShowError(
                        string.Format("Port {0} is restricted in all security groups associated with this instance.  " +
                        "To connect to this instance allow access to port {0} in one of the associated security groups.", NetworkProtocol.SSH.DefaultPort.Value));
                    return false;
                }
            }

            return isOpen;
        }

        protected void PersistLastSelectedValues(bool useKeyPair, string password)
        {
            var settings = PersistenceManager.Instance.GetSettings(ToolkitSettingsConstants.EC2ConnectSettings);
            var os = settings[this.InstanceId];

            os[ToolkitSettingsConstants.EC2InstanceUseKeyPair] = useKeyPair.ToString();
            os[ToolkitSettingsConstants.EC2InstanceSaveCredentials] = this._model.SaveCredentials.ToString();
            os[ToolkitSettingsConstants.EC2InstanceUserName] = this._model.EnteredUsername;

            if (useKeyPair || !this._model.SaveCredentials)
            {
                os[ToolkitSettingsConstants.EC2InstancePassword] = null;
            }
            else
            {
                os[ToolkitSettingsConstants.EC2InstancePassword] = password;
            }

            PersistenceManager.Instance.SaveSettings(ToolkitSettingsConstants.EC2ConnectSettings, settings);

            if (string.IsNullOrEmpty(password) && !this._usingStoredPrivateKey && this.Model.SavePrivateKey)
            {
                KeyPairLocalStoreManager.Instance.SavePrivateKey(_settingsUniqueKey, _connectionSettings.Region.Id, _instance.NativeInstance.KeyName, Model.PrivateKey);
            }
        }

        void loadLastSelectedValues(out bool useKeyPair, out string password)
        {
            var settings = PersistenceManager.Instance.GetSettings(ToolkitSettingsConstants.EC2ConnectSettings);
            var os = settings[this.InstanceId];

            useKeyPair = Convert.ToBoolean(os.GetValueOrDefault(ToolkitSettingsConstants.EC2InstanceUseKeyPair, true.ToString()));
            password = os[ToolkitSettingsConstants.EC2InstancePassword];

            this._model.SaveCredentials = Convert.ToBoolean(os.GetValueOrDefault(ToolkitSettingsConstants.EC2InstanceSaveCredentials, true.ToString()));
            this._model.EnteredUsername = os.GetValueOrDefault(ToolkitSettingsConstants.EC2InstanceUserName, this._model.EnteredUsername);
        }
    }
}
