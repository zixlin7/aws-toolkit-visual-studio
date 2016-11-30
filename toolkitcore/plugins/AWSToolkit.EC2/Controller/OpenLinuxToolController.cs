using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.Util;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.Runtime.Internal.Settings;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.Nodes;
using Amazon.AWSToolkit.EC2.View;
using Amazon.AWSToolkit.EC2.ConnectionUtils;

using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public abstract class OpenLinuxToolController
    {
        protected ActionResults _results;
        protected IAmazonEC2 _ec2Client;
        protected RunningInstanceWrapper _instance;
        protected OpenLinuxToolModel _model;
        protected FeatureViewModel _featureViewModel;
        private bool _usingStoredPrivateKey;

        public abstract string Executable
        {
            get;
        }

        public abstract string ToolSearchFolders
        {
            get;
        }

        public OpenLinuxToolModel Model
        {
            get { return this._model; }
        }

        public string InstanceId
        {
            get { return this._instance.InstanceId; }
        }

        public abstract OpenLinuxToolControl CreateControl(bool useKeyPair, string password);

        public ActionResults Execute(FeatureViewModel featureViewModel, RunningInstanceWrapper instance)
        {
            if (!instance.HasPublicAddress)
            {
                if (!ToolkitFactory.Instance.ShellProvider.Confirm("Instance IP Address", EC2Constants.NO_PUBLIC_IP_CONFIRM_CONNECT_PRIVATE_IP))
                {
                    return new ActionResults().WithSuccess(false);
                }
            }

            this._featureViewModel = featureViewModel;
            this._ec2Client = featureViewModel.EC2Client;
            this._instance = instance;
            this._model = new OpenLinuxToolModel(instance);
            this._model.ToolLocation = ToolsUtil.FindTool(Executable, ToolSearchFolders);

            if (KeyPairLocalStoreManager.Instance.DoesPrivateKeyExist(this._featureViewModel.AccountViewModel,
                this._featureViewModel.RegionSystemName, this._instance.NativeInstance.KeyName))
            {
                this._model.PrivateKey = KeyPairLocalStoreManager.Instance.GetPrivateKey(this._featureViewModel.AccountViewModel,
                    this._featureViewModel.RegionSystemName, this._instance.NativeInstance.KeyName);

                this._usingStoredPrivateKey = true;
            }

            bool useKeyPair;
            string password;
            loadLastSelectedValues(out useKeyPair, out password);
            ToolkitFactory.Instance.ShellProvider.ShowModal(CreateControl(useKeyPair, password));

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

                    if (!isOpen && ToolkitFactory.Instance.ShellProvider.ShowModal(control, MessageBoxButton.OKCancel))
                    {
                        var request = new AuthorizeSecurityGroupIngressRequest() { GroupId = this._instance.NativeInstance.SecurityGroups[0].GroupId };

                        IpPermission permission = new IpPermission()
                        {
                            IpRanges = new List<string>() { control.IPAddress },
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
                    ToolkitFactory.Instance.ShellProvider.ShowError(
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
                KeyPairLocalStoreManager.Instance.SavePrivateKey(this._featureViewModel.AccountViewModel,
                    this._featureViewModel.RegionSystemName,
                    this._instance.NativeInstance.KeyName,
                    this.Model.PrivateKey);
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
