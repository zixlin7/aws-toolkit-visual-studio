using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CloudFormation.Nodes;

using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;

using log4net;

namespace Amazon.AWSToolkit.CloudFormation.WizardPages.PageWorkers
{
    /// <summary>
    /// Background worker to look up a stack by name to ensure it's not
    /// already in use
    /// </summary>
    internal class QueryExistingStackNameWorker
    {
        public delegate void DataAvailableCallback(bool stackNameInUse);
        DataAvailableCallback _callback;

        struct WorkerData
        {
            public string StackName { get; set; }
            public IAmazonCloudFormation CloudFormationClient { get; set; }
            public ILog Logger { get; set; }
        }

        public QueryExistingStackNameWorker(AccountViewModel accountViewModel, 
                                            string stackName,
                                            ILog logger,
                                            DataAvailableCallback callback)
        {
            _callback = callback;

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(Worker);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(WorkerCompleted);
            bw.RunWorkerAsync(new WorkerData
            {
                StackName = stackName,
                CloudFormationClient = DeploymentWizardHelper.GetGenericCloudFormationClient(accountViewModel),
                Logger = logger
            });
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            var workerData = (WorkerData)e.Argument;

            bool stackNameInUse = false;
            try
            {
                var response = workerData.CloudFormationClient.DescribeStacks(new DescribeStacksRequest() { StackName = workerData.StackName });
                if (response.Stacks.Count > 0)
                    stackNameInUse = true;
            }
            catch (Exception exc)
            {
                workerData.Logger.Error(GetType().FullName + ", exception in Worker", exc);
            }

            e.Result = stackNameInUse;
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
                _callback((bool)e.Result);
        }

        private QueryExistingStackNameWorker() { }
    }
}
