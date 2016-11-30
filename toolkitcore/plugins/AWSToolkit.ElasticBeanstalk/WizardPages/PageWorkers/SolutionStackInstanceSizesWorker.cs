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
    /// Worker used to retrieve available instance sizes for a given solution stack
    /// </summary>
    internal class SolutionStackInstanceSizesWorker
    {
        public delegate void DataAvailableCallback(IEnumerable<string> data);
        DataAvailableCallback _callback;

        struct WorkerData
        {
            public string SolutionStack { get; set; }
            public IAmazonElasticBeanstalk BeanstalkClient { get; set; }
            public ILog Logger { get; set; }
        }

        public SolutionStackInstanceSizesWorker(AccountViewModel accountViewModel,
                                                RegionEndPointsManager.RegionEndPoints region,
                                                string solutionStack,
                                                ILog logger,
                                                DataAvailableCallback callback)
        {
            _callback = callback;

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(Worker);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(WorkerCompleted);
            bw.RunWorkerAsync(new WorkerData
                                  { 
                                    SolutionStack = solutionStack,
                                    BeanstalkClient = DeploymentWizardHelper.GetBeanstalkClient(accountViewModel, region),
                                    Logger = logger
                                  });
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            var workerData = (WorkerData)e.Argument;

            List<string> instanceSizes = new List<string>();
            try
            {
                var descRequest = new DescribeConfigurationOptionsRequest() { SolutionStackName = workerData.SolutionStack };
                descRequest.Options.Add( new OptionSpecification()
                {
                    Namespace = "aws:autoscaling:launchconfiguration",
                    OptionName = "InstanceType"
                });

                var response
                        = workerData
                            .BeanstalkClient
                            .DescribeConfigurationOptions
                            (descRequest);

                instanceSizes.AddRange(response.Options[0].ValueOptions);
            }
            catch (Exception exc)
            {
                workerData.Logger.Error(GetType().FullName + ", exception in Worker", exc);
            }

            e.Result = instanceSizes;
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
                _callback(e.Result as List<string>);
        }

        private SolutionStackInstanceSizesWorker() { }
    }
}
