using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.Nodes;

using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class DeleteKeyPairController
    {
        IAmazonEC2 _ec2Client;
        CreateKeyPairModel _model;
        EC2KeyPairsViewModel _keyPairModel;

        public ActionResults Execute(EC2KeyPairsViewModel keyPairModel, IList<KeyPairWrapper> keypairs)
        {
            this._keyPairModel = keyPairModel;
            this._ec2Client = this._keyPairModel.EC2Client;
            this._model = new CreateKeyPairModel();


            string msg = buildConfirmMessage(keypairs);
            if (!ToolkitFactory.Instance.ShellProvider.Confirm("Deleting Key Pairs", msg))
            {
                return new ActionResults().WithSuccess(false);
            }

            Dictionary<KeyPairWrapper, Exception> failures = new Dictionary<KeyPairWrapper, Exception>();
            foreach (var keypair in keypairs)
            {
                try
                {
                    var request = new DeleteKeyPairRequest() { KeyName = keypair.NativeKeyPair.KeyName };
                    this._ec2Client.DeleteKeyPair(request);

                    KeyPairLocalStoreManager.Instance.ClearPrivateKey(this._keyPairModel.AccountViewModel,
                        this._keyPairModel.RegionSystemName, keypair.NativeKeyPair.KeyName);
                }
                catch (Exception e)
                {
                    failures[keypair] = e;
                }
            }

            if (failures.Count > 0)
            {
                string failedMsg = buildFailureErrorMessage(failures);
                ToolkitFactory.Instance.ShellProvider.ShowError(failedMsg);

                if (keypairs.Count == failures.Count)
                    return new ActionResults().WithSuccess(false);
            }

            return new ActionResults().WithSuccess(true);
        }

        private string buildConfirmMessage(IList<KeyPairWrapper> keypairs)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Are you sure you want to delete the following key pair(s)?");
            sb.AppendLine();

            foreach (var keypair in keypairs)
            {
                sb.AppendLine(keypair.NativeKeyPair.KeyName);
            }

            return sb.ToString();
        }

        private string buildFailureErrorMessage(Dictionary<KeyPairWrapper, Exception> failures)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Failed to delete the following key pairs:");
            sb.AppendLine();

            foreach (var kvp in failures)
            {
                sb.AppendFormat("{0}: {1}\n", kvp.Key.NativeKeyPair.KeyName, kvp.Value.Message);
            }

            return sb.ToString();
        }
    }
}
