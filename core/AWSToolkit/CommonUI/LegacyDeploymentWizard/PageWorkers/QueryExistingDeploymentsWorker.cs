using System;
using System.Collections.Generic;
using System.ComponentModel;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;

using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;

using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;

using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard;

using log4net;
using Amazon.AWSToolkit.PluginServices.Deployment;

namespace Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.PageWorkers
{
    /// <summary>
    /// Worker thread class to query for existing CloudFormation and Beanstalk
    /// deployments for a given account/region.
    /// </summary>
    internal class QueryExistingDeploymentsWorker
    {
        public delegate void DataAvailableCallback(AccountViewModel forAccount, string forRegion, List<ExistingServiceDeployment> deployments);
        DataAvailableCallback _callback;

        struct WorkerData
        {
            public AccountViewModel Account { get; set; }
            public string Region { get; set; }
            public IEnumerable<IAWSToolkitDeploymentService> DeploymentServices { get; set; }
            public ILog Logger { get; set; }
        }

        /// <summary>
        /// Do an asynchronous check on CloudFormation stacks/Beanstalk environments
        /// </summary>
        /// <param name="accountViewModel"></param>
        /// <param name="region"></param>
        /// <param name="deploymentServices"></param>
        /// <param name="logger"></param>
        /// <param name="callback"></param>
        public QueryExistingDeploymentsWorker(AccountViewModel accountViewModel, 
                                              string region,
                                              IEnumerable<IAWSToolkitDeploymentService> deploymentServices,
                                              ILog logger,
                                              DataAvailableCallback callback)
        {
            _callback = callback;

            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;

            bw.DoWork += new DoWorkEventHandler(Worker);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(WorkerCompleted);
            bw.RunWorkerAsync(new WorkerData
            {
                Account = accountViewModel,
                Region = region,
                DeploymentServices = deploymentServices,
                Logger = logger
            });
        }

        /// <summary>
        /// Synchronous verification of existing CloudFormation/Beanstalk deployments
        /// </summary>
        public static List<ExistingServiceDeployment> LoadToolkitDeployments(AccountViewModel accountViewModel,
                                                                             string region,
                                                                             IEnumerable<IAWSToolkitDeploymentService> deploymentServices,
                                                                             ILog logger)
        {
            WorkerData workerData = new WorkerData
            {
                Account = accountViewModel,
                Region = region,
                DeploymentServices = deploymentServices,
                Logger = logger
            };   

            QueryExistingDeploymentsWorker workerObj = new QueryExistingDeploymentsWorker();
            return workerObj.QueryDeployments(workerData);
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            var workerData = (WorkerData)e.Argument;
            e.Result = new object[] { workerData.Account, workerData.Region, QueryDeployments(workerData) };
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
            {
                object[] results = e.Result as object[];
                _callback(results[0] as AccountViewModel, results[1] as string, results[2] as List<ExistingServiceDeployment>);
            }
        }

        List<ExistingServiceDeployment> QueryDeployments(WorkerData workerData)
        {
            List<ExistingServiceDeployment> deployments = new List<ExistingServiceDeployment>();

            foreach (var deploymentService in workerData.DeploymentServices)
            {
                var serviceDeployments = deploymentService.QueryToolkitDeployments(workerData.Account, workerData.Region, workerData.Logger);
                if (serviceDeployments != null)
                    deployments.AddRange(serviceDeployments);
            }

            return deployments;
        }
        
        private QueryExistingDeploymentsWorker() { }
    }
}
