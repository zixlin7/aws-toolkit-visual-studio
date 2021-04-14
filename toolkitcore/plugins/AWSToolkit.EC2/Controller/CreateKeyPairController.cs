using System.Windows;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;
using Amazon.AWSToolkit.EC2.Nodes;
using Amazon.AWSToolkit.Util;

using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class CreateKeyPairController
    {
        IAmazonEC2 _ec2Client;
        ActionResults _results = new ActionResults().WithSuccess(false);
        CreateKeyPairModel _model;
        EC2KeyPairsViewModel _keyPairModel;

        public ActionResults Execute(EC2KeyPairsViewModel keyPairModel)
        {
            this._keyPairModel = keyPairModel;
            this._ec2Client = this._keyPairModel.EC2Client;
            this._model = new CreateKeyPairModel();

            ToolkitFactory.Instance.ShellProvider.ShowModal(new CreateKeyPairControl(this));

            if(this._results.Success)
                ToolkitFactory.Instance.ShellProvider.ShowModal(new CreateKeyPairResponseControl(this), MessageBoxButton.OK);

            return this._results;
        }

        public CreateKeyPairModel Model => this._model;

        public bool CreateKeyPair()
        {
            var request = new CreateKeyPairRequest() { KeyName = this.Model.KeyPairName };
            var response = this._ec2Client.CreateKeyPair(request);
            this._model.PrivateKey = response.KeyPair.KeyMaterial;
            this._model.Fingerprint = response.KeyPair.KeyFingerprint;

            this._results = new ActionResults().WithSuccess(true);
            return true;
        }

        public void PersistPrivateKey()
        {
            KeyPairLocalStoreManager.Instance.SavePrivateKey(this._keyPairModel.AccountViewModel, this._keyPairModel.Region.Id,
                    this._model.KeyPairName, this._model.PrivateKey);
        }
    }
}
