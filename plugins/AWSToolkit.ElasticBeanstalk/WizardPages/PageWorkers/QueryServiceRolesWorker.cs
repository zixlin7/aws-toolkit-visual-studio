using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Web;

using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.AWSToolkit.Account;
using log4net;
using Amazon.Auth.AccessControlPolicy;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageWorkers
{
    /// <summary>
    /// Worker class to query established IAM roles for a selected account
    /// that grant an assume role policy to a service.
    /// </summary>
    internal class QueryServiceRolesWorker
    {
        public delegate void DataAvailableCallback(IEnumerable<Role> data);
        DataAvailableCallback _callback;

        struct WorkerData
        {
            public string ServicePrincipal { get; set; }
            public IAmazonIdentityManagementService IAMClient { get; set; }
            public ILog Logger { get; set; }
        }

        public QueryServiceRolesWorker(AccountViewModel accountViewModel,
                                       RegionEndPointsManager.RegionEndPoints regionEndPoints,
                                       ILog logger,
                                       DataAvailableCallback callback)
        {
            _callback = callback;

            var iamConfig = new AmazonIdentityManagementServiceConfig { ServiceURL = regionEndPoints.GetEndpoint(RegionEndPointsManager.IAM_SERVICE_NAME).Url };
            var iamClient = new AmazonIdentityManagementServiceClient(accountViewModel.Credentials, iamConfig);

            var bw = new BackgroundWorker();
            bw.DoWork += Worker;
            bw.RunWorkerCompleted += WorkerCompleted;
            bw.RunWorkerAsync(new WorkerData
            {
                ServicePrincipal = regionEndPoints.GetPrincipalForAssumeRole(RegionEndPointsManager.ELASTICBEANSTALK_SERVICE_NAME),
                IAMClient = iamClient,
                Logger = logger
            });
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            var workerData = (WorkerData)e.Argument;

            var roles = new List<Role>();

            try
            {
                ListRolesResponse response;
                var request = new ListRolesRequest();
                do
                {
                    response = workerData.IAMClient.ListRoles(request);
                    var validRoles = RolePolicyFilter.FilterByAssumeRoleServicePrincipal(response.Roles, workerData.ServicePrincipal);
                    roles.AddRange(validRoles);
                    request.Marker = response.Marker;
                } while (response.IsTruncated);
            }
            catch (Exception exc)
            {
                workerData.Logger.Error(GetType().FullName + ", exception in Worker", exc);
            }

            e.Result = roles;
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
                _callback(e.Result as IEnumerable<Role>);
        }

        private QueryServiceRolesWorker() { }
    }
}
