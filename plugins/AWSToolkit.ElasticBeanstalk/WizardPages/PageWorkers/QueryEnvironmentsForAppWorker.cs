using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.ElasticBeanstalk.Nodes;

using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;

using log4net;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageWorkers
{
    /// <summary>
    /// Worker used to retrieve environment names for an already-deployed application; only
    /// environments in non-terminating/terminated state are returned
    /// </summary>
    internal class QueryEnvironmentsForAppWorker
    {
        public delegate void DataAvailableCallback(IEnumerable<EnvironmentDescription> data);

        readonly DataAvailableCallback _callback;

        struct WorkerData
        {
            public string ApplicationName { get; set; }
            public IAmazonElasticBeanstalk BeanstalkClient { get; set; }
            public ILog Logger { get; set; }
        }

        public QueryEnvironmentsForAppWorker(AccountViewModel accountViewModel, 
                                             RegionEndPointsManager.RegionEndPoints region,
                                             string applicationName,
                                             ILog logger,
                                             DataAvailableCallback callback)
        {
            _callback = callback;

            var bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(Worker);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(WorkerCompleted);
            bw.RunWorkerAsync(new WorkerData
            {
                ApplicationName = applicationName,
                BeanstalkClient = DeploymentWizardHelper.GetBeanstalkClient(accountViewModel, region),
                Logger = logger
            });
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            var workerData = (WorkerData)e.Argument;

            var validEnvironments = new List<EnvironmentDescription>();
            try
            {
                var response = workerData.BeanstalkClient.DescribeEnvironments(new DescribeEnvironmentsRequest(){ ApplicationName = workerData.ApplicationName });
                validEnvironments.AddRange(response.Environments.Where(environment => environment.Status == BeanstalkConstants.STATUS_READY 
                                                                                        || environment.Status == BeanstalkConstants.STATUS_LAUNCHING 
                                                                                        || environment.Status == BeanstalkConstants.STATUS_UPDATING));
            }
            catch (Exception exc)
            {
                workerData.Logger.Error(GetType().FullName + ", exception in Worker", exc);
            }

            e.Result = validEnvironments;
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
                _callback(e.Result as IEnumerable<EnvironmentDescription>);
        }

        private QueryEnvironmentsForAppWorker() { }
    }

}
