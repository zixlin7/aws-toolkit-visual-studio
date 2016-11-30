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
    /// Fetches the application options from the environment for a deployed application
    /// (PARAM1..5, app access and secret key)
    /// </summary>
    internal class QueryEnvironmentSettingsWorker
    {
        public delegate void DataAvailableCallback(IEnumerable<ConfigurationOptionSetting> data);
        DataAvailableCallback _callback;

        struct WorkerData
        {
            public string ApplicationName { get; set; }
            public string EnvironmentName { get; set; }
            public IAmazonElasticBeanstalk BeanstalkClient { get; set; }
            public ILog Logger { get; set; }
        }

        public QueryEnvironmentSettingsWorker(AccountViewModel accountViewModel, 
                                              RegionEndPointsManager.RegionEndPoints region,
                                              string applicationName,
                                              string environmentName,
                                              ILog logger,
                                              DataAvailableCallback callback)
        {
            _callback = callback;

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(Worker);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(WorkerCompleted);
            bw.RunWorkerAsync(new WorkerData
            {
                ApplicationName = applicationName,
                EnvironmentName = environmentName,
                BeanstalkClient = DeploymentWizardHelper.GetBeanstalkClient(accountViewModel, region),
                Logger = logger
            });
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            var workerData = (WorkerData)e.Argument;

            List<ConfigurationOptionSetting> configSettings = new List<ConfigurationOptionSetting>();
            try
            {
                var request = new DescribeConfigurationSettingsRequest() { ApplicationName = workerData.ApplicationName, EnvironmentName = workerData.EnvironmentName };
                var response = workerData.BeanstalkClient.DescribeConfigurationSettings(request);
                // return all configs even though at this stage the wizard is only interested in PARAM1..5 and AWS keys
                configSettings.AddRange(response.ConfigurationSettings[0].OptionSettings);
            }
            catch (Exception exc)
            {
                workerData.Logger.Error(GetType().FullName + ", exception in Worker", exc);
            }

            e.Result = configSettings;
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
                _callback(e.Result as IEnumerable<ConfigurationOptionSetting>);
        }

        private QueryEnvironmentSettingsWorker() { }
    }
}
