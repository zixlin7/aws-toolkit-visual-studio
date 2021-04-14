using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon.AWSToolkit.EC2.Nodes;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;
using Amazon.AWSToolkit.Util;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class ViewKeyPairsController : FeatureController<ViewKeyPairsModel>
    {
        ViewKeyPairsControl _control;

        protected override void DisplayView()
        {
            this._control = new ViewKeyPairsControl(this);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);
        }

        public void LoadModel()
        {
            RefreshKeyPairs();
        }


        public void RefreshKeyPairs()
        {
            var response = this.EC2Client.DescribeKeyPairs(new DescribeKeyPairsRequest());            

            var account = this.FeatureViewModel.AccountViewModel;
            var region = this.FeatureViewModel.Region.Id;
            ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
            {
                this.Model.KeyPairs.Clear();
                foreach (var keyPair in response.KeyPairs.OrderBy(x => x.KeyName.ToLower()))
                {
                    this.Model.KeyPairs.Add(new KeyPairWrapper(account, region, keyPair));
                }
            }));
        }

        public void ResetSelection(IList<KeyPairWrapper> keyPairs)
        {
            this.Model.SelectedKeys.Clear();
            foreach (var keyPair in keyPairs)
            {
                this.Model.SelectedKeys.Add(keyPair);
            }
        }

        public bool DeleteKeyPairs()
        {
            var controller = new DeleteKeyPairController();
            var results = controller.Execute((EC2KeyPairsViewModel)this.FeatureViewModel, this.Model.SelectedKeys);

            if (results.Success)
            {
                this.RefreshKeyPairs();
            }
            return results.Success;
        }

        public bool CreateKeyPair()
        {
            var controller = new CreateKeyPairController();
            var results = controller.Execute(this.FeatureViewModel as EC2KeyPairsViewModel);

            if (results.Success)
            {
                this.RefreshKeyPairs();
            }
            return results.Success;
        }

        public void ClearPrivateKeys(IList<KeyPairWrapper> keys)
        {
            if (!ToolkitFactory.Instance.ShellProvider.Confirm("Clear Private Key(s)", "Are you sure you want to clear the private key(s)?"))
            {
                return;
            }

            foreach (var key in keys)
            {
                if (key.IsStoredLocally)
                {
                    KeyPairLocalStoreManager.Instance.ClearPrivateKey(this.FeatureViewModel.AccountViewModel,
                        this.FeatureViewModel.Region.Id, key.NativeKeyPair.KeyName);
                    key.RaiseStoredLocallyEvent();
                }
            }
        }

        public void ImportPrivatekey(KeyPairWrapper key, string file)
        {
            string privateKey = null;
            using (StreamReader reader = new StreamReader(file))
            {
                privateKey = reader.ReadToEnd();
            }

            KeyPairLocalStoreManager.Instance.SavePrivateKey(this.FeatureViewModel.AccountViewModel,
                this.FeatureViewModel.Region.Id, key.NativeKeyPair.KeyName, privateKey);
            key.RaiseStoredLocallyEvent();
        }

        public void ExportPrivatekey(KeyPairWrapper key, string file)
        {
            string privateKey = KeyPairLocalStoreManager.Instance.GetPrivateKey(this.FeatureViewModel.AccountViewModel,
                this.FeatureViewModel.Region.Id, key.NativeKeyPair.KeyName);

            using (StreamWriter writer = new StreamWriter(file))
            {
                writer.Write(privateKey);
            }
        }
    }
}
