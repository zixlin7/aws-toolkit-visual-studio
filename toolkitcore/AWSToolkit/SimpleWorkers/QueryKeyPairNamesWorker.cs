using System;
using System.ComponentModel;
using System.Collections.Generic;
using Amazon.EC2;
using Amazon.EC2.Model;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Util;

using log4net;

namespace Amazon.AWSToolkit.SimpleWorkers
{
    /// <summary>
    /// Worker used to retrieve existing keypairs
    /// </summary>
    public class QueryKeyPairNamesWorker
    {
        public delegate void DataAvailableCallback(ICollection<string> existingKeyPairs, ICollection<string> keyPairsStoredInToolkit);
        DataAvailableCallback _callback;

        /// <summary>
        /// Simple worker to fetch the set of key pair names for a user
        /// </summary>
        /// <param name="ec2Client"></param>
        /// <param name="logger"></param>
        /// <param name="callback"></param>
        public QueryKeyPairNamesWorker(AccountViewModel account,
                                       string region,
                                       IAmazonEC2 ec2Client,
                                       ILog logger,
                                       DataAvailableCallback callback)
        {
            _callback = callback;

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(Worker);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(WorkerCompleted);
            bw.RunWorkerAsync(new object[] { account, region, ec2Client, logger });
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            object[] args = e.Argument as object[];
            Account.AccountViewModel account = args[0] as Account.AccountViewModel;
            string region = args[1] as string;
            IAmazonEC2 ec2 = args[2] as IAmazonEC2;
            ILog logger = args[3] as ILog;

            try
            {
                var response = ec2.DescribeKeyPairs(new DescribeKeyPairsRequest());

                List<string> keyNames = new List<string>();
                List<string> storedInToolkit = new List<string>();
                foreach(var keyPair in response.KeyPairs)
                {
                    keyNames.Add(keyPair.KeyName);
                    if(KeyPairLocalStoreManager.Instance.DoesPrivateKeyExist(account.SettingsUniqueKey, region, keyPair.KeyName))
                    {
                        storedInToolkit.Add(keyPair.KeyName);
                    }
                }

                keyNames.Sort(StringComparer.CurrentCultureIgnoreCase);
                e.Result = new List<string>[] { keyNames, storedInToolkit };
            }
            catch (Exception exc)
            {
                logger.Error(GetType().FullName + ", exception in Worker", exc);
                e.Result = exc;
            }
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
            {
                var args = e.Result as List<string>[];
                List<string> keyNames = args[0];
                List<string> storedInToolkit = args[1];

                _callback(keyNames, storedInToolkit);
            }
        }

        private QueryKeyPairNamesWorker() { }
    }
}
