using System;
using System.Collections.Generic;
using System.Windows;
using Amazon.AWSToolkit.Navigator;
using Amazon.Runtime.Internal.Settings;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.Nodes;
using Amazon.AWSToolkit.EC2.View;
using Amazon.AWSToolkit.EC2.ConnectionUtils;

using Amazon.EC2;
using Amazon.EC2.Model;

using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class OpenRemoteDesktopController
    {
        const string _mapDrivesSettingName = "MapDrives";

        ActionResults _results;
        IAmazonEC2 _ec2Client;
        RunningInstanceWrapper _instance;
        OpenRemoteDesktopModel _model;
        FeatureViewModel _featureViewModel;
        bool _usingStoredPrivateKey;

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
            this._model = new OpenRemoteDesktopModel(instance);

            if (KeyPairLocalStoreManager.Instance.DoesPrivateKeyExist(this._featureViewModel.AccountViewModel,
                this._featureViewModel.Region.Id, this._instance.NativeInstance.KeyName))
            {
                this._model.PrivateKey = KeyPairLocalStoreManager.Instance.GetPrivateKey(this._featureViewModel.AccountViewModel,
                    this._featureViewModel.Region.Id, this._instance.NativeInstance.KeyName);

                this._usingStoredPrivateKey = true;
            }

            bool useKeyPair;
            string password;
            loadLastSelectedValues(out useKeyPair, out password);

            ToolkitFactory.Instance.ShellProvider.ShowModal(new OpenRemoteDesktopControl(this, useKeyPair, password));

            if(_results == null)
                return new ActionResults().WithSuccess(false);

            return new ActionResults().WithSuccess(true);
        }

        public string InstanceId => this._instance.InstanceId;

        public OpenRemoteDesktopModel Model => this._model;

        public void OpenRemoteDesktopWithCredentials(string username, string password)
        {
            checkIfOpenPort();

            RemoteDesktopUtil.Connect(this._instance.ConnectName, username, password, this._model.MapDrives);

            this._results = new ActionResults().WithSuccess(true);
            persistLastSelectedValues(false, password);
        }

        public void OpenRemoteDesktopWithEC2KeyPair()
        {
            var request = new GetPasswordDataRequest() { InstanceId = this._instance.NativeInstance.InstanceId };
            var response = this._ec2Client.GetPasswordData(request);

            if (!GetPasswordController.ValidateEncryptedPassword(this._instance, response))
            {
                return;
            }

            string decryptedPassword = response.GetDecryptedPassword(this._model.PrivateKey);

            OpenRemoteDesktopWithCredentials(EC2Constants.DEFAULT_ADMIN_USER, decryptedPassword);
            persistLastSelectedValues(true, null);
        }


        bool checkIfOpenPort()
        {
            bool isOpen = EC2Utilities.checkIfPortOpen(this._ec2Client, this._instance.NativeInstance.SecurityGroups, NetworkProtocol.RDP.DefaultPort.Value);

            if (!isOpen)
            {
                if (this._instance.NativeInstance.SecurityGroups.Count == 1)
                {
                    var externalIpAddress = IPAddressUtil.DetermineIPFromExternalSource();
                    if (externalIpAddress != null)
                        externalIpAddress += "/32";

                    string msg = string.Format("The security group \"{0}\" used for this instance does not have remote desktop port open, " +
                        "would you like to open the remote desktop port?", this._instance.SecurityGroups);

                    var control = new AskToOpenPort(msg, externalIpAddress);

                    if (!isOpen && ToolkitFactory.Instance.ShellProvider.ShowModal(control, MessageBoxButton.OKCancel))
                    {
                        var request = new AuthorizeSecurityGroupIngressRequest() { GroupId = this._instance.NativeInstance.SecurityGroups[0].GroupId };

                        IpPermission permission = new IpPermission()
                        {
                            Ipv4Ranges = new List<IpRange> { new IpRange { CidrIp = control.IPAddress } },
                            IpProtocol = NetworkProtocol.RDP.UnderlyingProtocol.ToString().ToLower(),
                            FromPort = NetworkProtocol.RDP.DefaultPort.GetValueOrDefault(),
                            ToPort = NetworkProtocol.RDP.DefaultPort.GetValueOrDefault()
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
                        "To connect to this instance allow access to port {0} in one of the associated security groups.", NetworkProtocol.RDP.DefaultPort.Value));
                    return false;
                }
            }

            return isOpen;
        }

        void persistLastSelectedValues(bool useKeyPair, string password)
        {
            var settings = PersistenceManager.Instance.GetSettings(ToolkitSettingsConstants.EC2ConnectSettings);
            var os = settings[this.InstanceId];

            os[ToolkitSettingsConstants.EC2InstanceUseKeyPair] = useKeyPair.ToString();
            os[ToolkitSettingsConstants.EC2InstanceMapDrives] = this._model.MapDrives.ToString();
            os[ToolkitSettingsConstants.EC2InstanceSaveCredentials] = this._model.SaveCredentials.ToString();

            if (useKeyPair || !this._model.SaveCredentials)
            {
                os[ToolkitSettingsConstants.EC2InstanceUserName] = null;
                os[ToolkitSettingsConstants.EC2InstancePassword] = null;
            }
            else
            {
                os[ToolkitSettingsConstants.EC2InstanceUserName] = this._model.EnteredUsername;
                os[ToolkitSettingsConstants.EC2InstancePassword] = password;
            }

            PersistenceManager.Instance.SaveSettings(ToolkitSettingsConstants.EC2ConnectSettings, settings);

            if (string.IsNullOrEmpty(password) && !this._usingStoredPrivateKey && this.Model.SavePrivateKey)
            {
                KeyPairLocalStoreManager.Instance.SavePrivateKey(this._featureViewModel.AccountViewModel,
                    this._featureViewModel.Region.Id,
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

            this._model.MapDrives = Convert.ToBoolean(os.GetValueOrDefault(ToolkitSettingsConstants.EC2InstanceMapDrives, true.ToString()));
            this._model.SaveCredentials = Convert.ToBoolean(os.GetValueOrDefault(ToolkitSettingsConstants.EC2InstanceSaveCredentials, true.ToString()));
            this._model.EnteredUsername = os[ToolkitSettingsConstants.EC2InstanceUserName];
        }
    }
}
