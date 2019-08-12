using System;
using System.Collections.Generic;
using log4net;
using Amazon.AWSToolkit.Account;
using System.ComponentModel;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageWorkers
{
    /// <summary>
    /// Worker class to query established IAM instance profiles for a selected account
    /// </summary>
    internal class QueryInstanceProfilesWorker
    {
        public delegate void DataAvailableCallback(IEnumerable<InstanceProfile> data);
        DataAvailableCallback _callback;

        struct WorkerData
        {
            public IAmazonIdentityManagementService IAMClient { get; set; }
            public ILog Logger { get; set; }
        }

        public QueryInstanceProfilesWorker(AccountViewModel accountViewModel,
                                           RegionEndPointsManager.RegionEndPoints regionEndPoints,
                                           ILog logger,
                                           DataAvailableCallback callback)
        {
            _callback = callback;

            var iamConfig = new AmazonIdentityManagementServiceConfig ();
            regionEndPoints.GetEndpoint(RegionEndPointsManager.IAM_SERVICE_NAME).ApplyToClientConfig(iamConfig);
            var iamClient = new AmazonIdentityManagementServiceClient(accountViewModel.Credentials, iamConfig);

            var bw = new BackgroundWorker();
            bw.DoWork += Worker;
            bw.RunWorkerCompleted += WorkerCompleted;
            bw.RunWorkerAsync(new WorkerData
            {
                IAMClient = iamClient,
                Logger = logger
            });
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            var workerData = (WorkerData)e.Argument;

            var profiles = new List<InstanceProfile>();

            try
            {
                ListInstanceProfilesResponse response;
                var request = new ListInstanceProfilesRequest();
                do
                {
                    response = workerData.IAMClient.ListInstanceProfiles(request);
                    var profileSet = response.InstanceProfiles;
                    profiles.AddRange(profileSet);
                    request.Marker = response.Marker;
                } while (response.IsTruncated);
            }
            catch (Exception exc)
            {
                workerData.Logger.Error(GetType().FullName + ", exception in Worker", exc);
            }

            e.Result = profiles;
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
                _callback(e.Result as IEnumerable<InstanceProfile>);
        }

        private QueryInstanceProfilesWorker() { }
    }
}
