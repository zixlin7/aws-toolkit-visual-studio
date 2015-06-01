using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.ElasticBeanstalk.Model;
using Amazon.EC2.Model;
using Amazon.ElasticBeanstalk;
using log4net;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageWorkers
{
    /// <summary>
    /// Worker class that determines the running environments for a
    /// user in a region and inverts the data from the service to
    /// return a set of applications and their deployed environments.
    /// This worker is used by the StartPage in the new deployment wizard.
    /// </summary>
    internal class QueryExistingDeploymentsWorker
    {
        public delegate void DataAvailableCallback(AccountViewModel forAccount, 
                                                   string forRegion, 
                                                   ICollection<DeployedApplicationModel> data);
        readonly DataAvailableCallback _callback;

        struct WorkerData
        {
            public AccountViewModel Account { get; set; }
            public RegionEndPointsManager.RegionEndPoints Region { get; set; }
            public ILog Logger { get; set; }
        }

        public QueryExistingDeploymentsWorker(AccountViewModel accountViewModel,
                                              RegionEndPointsManager.RegionEndPoints region,
                                              ILog logger,
                                              DataAvailableCallback callback)
        {
            _callback = callback;

            var bw = new BackgroundWorker();
            bw.DoWork += Worker;
            bw.RunWorkerCompleted += WorkerCompleted;
            bw.RunWorkerAsync(new WorkerData
            {
                Account = accountViewModel,
                Region = region,
                Logger = logger
            });
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            var workerData = (WorkerData)e.Argument;

            var applicationEnvironments = new Dictionary<string, DeployedApplicationModel>();

            try
            {
                var beanstalkClient = DeploymentWizardHelper.GetBeanstalkClient(workerData.Account, workerData.Region);
                var response = beanstalkClient.DescribeEnvironments();
                var validEnvironments 
                    = response.Environments.Where(environment => environment.Status == BeanstalkConstants.STATUS_READY 
                                                                     || environment.Status == BeanstalkConstants.STATUS_LAUNCHING 
                                                                     || environment.Status == BeanstalkConstants.STATUS_UPDATING).ToList();

                foreach (var env in validEnvironments)
                {
                    if (applicationEnvironments.ContainsKey(env.ApplicationName))
                    {
                        var model = applicationEnvironments[env.ApplicationName];
                        model.Environments.Add(env);
                    }
                    else
                    {
                        var model = new DeployedApplicationModel(env.ApplicationName);
                        model.Environments.Add(env);
                        applicationEnvironments.Add(env.ApplicationName, model);
                    }
                }
            }
            catch (Exception exc)
            {
                workerData.Logger.Error(GetType().FullName + ", exception in Worker", exc);
            }

            e.Result = new object[] { workerData.Account, workerData.Region.SystemName, applicationEnvironments.Values.ToList() };
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
            {
                var results = e.Result as object[];
                _callback(results[0] as AccountViewModel, results[1] as string, results[2] as ICollection<DeployedApplicationModel>);
            }
        }

        private QueryExistingDeploymentsWorker() { }
    }
}
