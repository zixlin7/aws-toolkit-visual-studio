using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;

using Amazon.EC2;
using Amazon.EC2.Model;

using Amazon.AWSToolkit.EC2.Model;

using log4net;
using Amazon.AWSToolkit.Account;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;

namespace Amazon.AWSToolkit.EC2.LaunchWizard.PageWorkers
{
    /// <summary>
    /// Worker used to fetch the set of kernel ids associated with the selected image
    /// </summary>
    internal class QueryInstanceProfilesWorker
    {
        public delegate void DataAvailableCallback(ICollection<InstanceProfile> data);
        DataAvailableCallback _callback;

        struct WorkerData
        {
            public IAmazonIdentityManagementService IAMClient { get; set; }
            public ILog Logger { get; set; }
        }

        public QueryInstanceProfilesWorker(
                                    AccountViewModel accountViewModel, 
                                    RegionEndPointsManager.RegionEndPoints regionEndPoints,
                                    ILog logger,
                                    DataAvailableCallback callback)
        {
            _callback = callback;

            var bw = new BackgroundWorker {WorkerReportsProgress = true, WorkerSupportsCancellation = true};

            var endpoint = regionEndPoints.GetEndpoint(RegionEndPointsManager.IAM_SERVICE_NAME);
            var iamConfig = new AmazonIdentityManagementServiceConfig {ServiceURL = endpoint.Url};
            if (endpoint.Signer != null)
                iamConfig.SignatureVersion = endpoint.Signer;
            if (endpoint.AuthRegion != null)
                iamConfig.AuthenticationRegion = endpoint.AuthRegion;

            var iamClient = new AmazonIdentityManagementServiceClient(accountViewModel.Credentials, iamConfig);

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

            ListInstanceProfilesResponse response;
            var request = new ListInstanceProfilesRequest();
            try
            {
                do
                {
                    response = workerData.IAMClient.ListInstanceProfiles(request);
                    var profileSet = response.InstanceProfiles;
                    profiles.AddRange(profileSet);
                    request.Marker = response.Marker;
                } while (response.IsTruncated);
            }
            catch (Exception ex)
            {
                workerData.Logger.Warn("Unable to list instance profiles: " + ex.Message);
            }

            e.Result = profiles;
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
                _callback(e.Result as List<InstanceProfile>);
        }

        private QueryInstanceProfilesWorker() { }
    }
}
