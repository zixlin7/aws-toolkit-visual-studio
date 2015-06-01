using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.Nodes;
using Amazon.AWSToolkit.EC2.View;
using Amazon.AWSToolkit.Util;

using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class GetPasswordController
    {
        IAmazonEC2 _ec2Client;
        RunningInstanceWrapper _instance;
        GetPasswordModel _model;
        FeatureViewModel _featureViewModel;
        GetPasswordDataResponse _getResponse;

        public ActionResults Execute(FeatureViewModel featureViewModel, RunningInstanceWrapper instance)
        {
            this._featureViewModel = featureViewModel;
            this._ec2Client = featureViewModel.EC2Client;
            this._instance = instance;
            this._model = new GetPasswordModel(instance);

            try
            {
                var request = new GetPasswordDataRequest() { InstanceId = this._instance.NativeInstance.InstanceId };
                this._getResponse = this._ec2Client.GetPasswordData(request);
                this._model.EncryptedPassword = this._getResponse.PasswordData;

                if (!ValidateEncryptedPassword(this._instance, this._getResponse))
                {
                    return new ActionResults().WithSuccess(false);
                }
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error communicating with instance: " + e.Message);
                return new ActionResults().WithSuccess(false);
            }

            if (KeyPairLocalStoreManager.Instance.DoesPrivateKeyExist(this._featureViewModel.AccountViewModel,
                this._featureViewModel.RegionSystemName, this._instance.NativeInstance.KeyName))
            {
                this._model.PrivateKey = KeyPairLocalStoreManager.Instance.GetPrivateKey(this._featureViewModel.AccountViewModel,
                    this._featureViewModel.RegionSystemName, this._instance.NativeInstance.KeyName);
                this.DecryptPassword();
            }

            ToolkitFactory.Instance.ShellProvider.ShowModal(new GetPasswordControl(this));            
            return new ActionResults().WithSuccess(true);
        }

        public static bool ValidateEncryptedPassword(RunningInstanceWrapper instance, GetPasswordDataResponse response)
        {
            if (string.IsNullOrEmpty(response.PasswordData))
            {
                if (instance.LaunchTime < DateTime.Now.AddMinutes(-EC2Constants.TIME_BEFORE_DETECT_NOPASSWORD))
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("No password was found.\nThere does not appear to " +
                        "be a generated Administrator password for this Windows instance. It is likely that " +
                        "this AMI has a built-in password known only to its creator. Please check the Log " +
                        "Output for details or contact the AMI owner.");
                }
                else
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Not available yet.\nPassword generation and " +
                        "encryption can sometimes take more than 30 minutes. Please wait at " +
                        "least 15 minutes after launching an instance before trying to retrieve the generated password.");
                }

                return false ;
            }

            return true;
        }

        public GetPasswordModel Model
        {
            get { return this._model; }
        }

        public void DecryptPassword()
        {
            this._model.DecryptedPassword = this._getResponse.GetDecryptedPassword(this._model.PrivateKey);
        }
    }
}
