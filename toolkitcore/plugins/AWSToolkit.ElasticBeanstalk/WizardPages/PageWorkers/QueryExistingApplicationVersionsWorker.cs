using System;
using System.Collections.Generic;
using System.ComponentModel;
using Amazon.AWSToolkit.Account;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;

using log4net;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageWorkers
{
    /// <summary>
    /// Worker used to retrieve existing version labels during app redeployment. Returns
    /// a collection of ApplicationVersionDescription as a result.
    /// </summary>
    class QueryExistingApplicationVersionsWorker
    {
        public delegate void DataAvailableCallback(IEnumerable<ApplicationVersionDescription> data);
        readonly DataAvailableCallback _callback;

        /// <summary>
        /// Asynchronous fetch of existing versions for an application
        /// </summary>
        /// <param name="accountViewModel"></param>
        /// <param name="appName"></param>
        /// <param name="logger"></param>
        /// <param name="callback"></param>
        public QueryExistingApplicationVersionsWorker(AccountViewModel accountViewModel,
                                                      RegionEndPointsManager.RegionEndPoints region,
                                                      string appName,
                                                      ILog logger,
                                                      DataAvailableCallback callback)
        {
            _callback = callback;

            var bw = new BackgroundWorker();
            bw.DoWork += Worker;
            bw.RunWorkerCompleted += WorkerCompleted;
            bw.RunWorkerAsync(new object[] { DeploymentWizardHelper.GetBeanstalkClient(accountViewModel, region), appName, logger });
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as object[];
            var beanstalkClient = args[0] as IAmazonElasticBeanstalk;
            var appName = args[1] as string;
            var logger = args[2] as ILog;

            var versions = new List<ApplicationVersionDescription>();
            try
            {
                var response = beanstalkClient.DescribeApplicationVersions(new DescribeApplicationVersionsRequest { ApplicationName = appName });
                versions.AddRange(response.ApplicationVersions);
            }
            catch (Exception exc)
            {
                logger.Error(GetType().FullName + ", exception in Worker", exc);
            }

            e.Result = versions;
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
                _callback(e.Result as List<ApplicationVersionDescription>);
        }

        private QueryExistingApplicationVersionsWorker() { }
    }
}
