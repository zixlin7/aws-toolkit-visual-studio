using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Account;

using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;

using log4net;

namespace Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.PageWorkers
{
    /// <summary>
    /// Worker class to fetch IAM user accounts and match up with accounts held
    /// locally that have the secret key available. IAM users where the secret key
    /// is not held locally are discarded. The caller receives a dictionary of user
    /// names and the set of one or more access keys available for that user where
    /// the secret key is available in the toolkit.
    /// </summary>
    internal class FetchCompatibleIAMUsersWorker
    {
        public delegate void DataAvailableCallback(Dictionary<string, List<string>> compatibleIAMUsers);
        DataAvailableCallback _callback;

        struct WorkerData
        {
            public IAmazonIdentityManagementService IAMClient { get; set; }
            public IEnumerable<string> LocallyHeldUserKeys { get; set; }
            public ILog Logger { get; set; }
        }

        public FetchCompatibleIAMUsersWorker(AccountViewModel account, 
                                             RegionEndPointsManager.RegionEndPoints regionEndPoints, 
                                             IEnumerable<string> locallyHeldUserKeys,
                                             ILog logger,
                                             DataAvailableCallback callback)
        {
            _callback = callback;

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(Worker);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(WorkerCompleted);

            var iamConfig = new AmazonIdentityManagementServiceConfig {ServiceURL = regionEndPoints.GetEndpoint(RegionEndPointsManager.IAM_SERVICE_NAME).Url};
            var iamClient = new AmazonIdentityManagementServiceClient(account.Credentials, iamConfig);

            bw.RunWorkerAsync(new WorkerData
            {
                IAMClient = iamClient,
                LocallyHeldUserKeys = locallyHeldUserKeys,
                Logger = logger
            });
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            var workerData = (WorkerData)e.Argument;
            Dictionary<string, List<string>> compatibleUsers = new Dictionary<string, List<string>>();

            HashSet<string> localKeyLookup = new HashSet<string>();
            if (workerData.LocallyHeldUserKeys != null)
            {
                foreach (string accessKey in workerData.LocallyHeldUserKeys)
                {
                    localKeyLookup.Add(accessKey);
                }
            }

            try
            {
                var response = workerData.IAMClient.ListUsers(new ListUsersRequest());
                foreach (var user in response.Users)
                {
                    List<string> userAccessKeys = QueryUserKeys(workerData.IAMClient, user, workerData.Logger);
                    foreach (string userAccessKey in userAccessKeys)
                    {
                        if (localKeyLookup.Contains(userAccessKey))
                        {
                            List<string> accessKeys;
                            if (compatibleUsers.ContainsKey(user.UserName))
                                accessKeys = compatibleUsers[user.UserName];
                            else
                            {
                                accessKeys = new List<string>();
                                compatibleUsers.Add(user.UserName, accessKeys);
                            }

                            accessKeys.Add(userAccessKey);
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                workerData.Logger.Error(GetType().FullName + ", exception in Worker", exc);
            }

            e.Result = compatibleUsers;
        }

        /// <summary>
        /// Assembles the set of access keys for a given user into one list, handling pagination if necessary
        /// </summary>
        List<string> QueryUserKeys(IAmazonIdentityManagementService iamClient, User user, ILog logger)
        {
            List<string> keys = new List<string>();

            try
            {
                var listAccessKeysRequest =
                    new ListAccessKeysRequest() { UserName = user.UserName };
                ListAccessKeysResponse listAccessKeysResponse = null;
                do
                {
                    if (listAccessKeysResponse != null)
                        listAccessKeysRequest.Marker = listAccessKeysResponse.Marker;
                    listAccessKeysResponse = iamClient.ListAccessKeys(listAccessKeysRequest);

                    if (listAccessKeysResponse.AccessKeyMetadata != null)
                    {
                        foreach (var metadata in listAccessKeysResponse.AccessKeyMetadata)
                        {
                            keys.Add(metadata.AccessKeyId);
                        }
                    }
                }
                while (listAccessKeysResponse.IsTruncated);
            }
            catch (Exception exc)
            {
                logger.Error(string.Format("{0} , exception in Worker's QueryUserKeys for user '{1}'", GetType().FullName, user.UserName), exc);
            }

            return keys;
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
                _callback(e.Result as Dictionary<string, List<string>>);
        }

        private FetchCompatibleIAMUsersWorker() { }
    }
}
