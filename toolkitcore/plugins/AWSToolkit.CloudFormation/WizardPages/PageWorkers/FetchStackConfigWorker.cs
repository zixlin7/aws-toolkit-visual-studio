using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CloudFormation.Controllers;

using Amazon.S3;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.AutoScaling;
using Amazon.AWSToolkit.Regions;
using Amazon.ElasticLoadBalancing;
using ThirdParty.Json.LitJson;
using log4net;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.CloudFormation.WizardPages.PageWorkers
{
    /// <summary>
    /// Fetches and decrypts the current deployment config file for a given regional
    /// deployment and application, returning the parsed json data on completion.
    /// </summary>
    internal class FetchStackConfigWorker
    {
        public delegate void DataAvailableCallback(JsonData configData);
        DataAvailableCallback _callback;

        struct WorkerData
        {
            public ClientFactory Factory { get; set; }
            public Stack RunningStack { get; set; }
            public string DefaultBucketName { get; set; }
            public string DefaultConfigFileKey { get; set; }
            public ILog Logger { get; set; }
        }

        class ClientFactory
        {
            public ClientFactory(AccountViewModel account, ToolkitRegion region)
            {
                _account = account;
                _region = region;
            }

            AccountViewModel _account;
            public AccountViewModel Account => _account;

            ToolkitRegion _region;
            public ToolkitRegion Region => _region;

            IAmazonAutoScaling _autoScalingClient;
            public IAmazonAutoScaling AutoScalingClient
            {
                get
                {
                    if (_autoScalingClient == null)
                    {
                        _autoScalingClient = Account.CreateServiceClient<AmazonAutoScalingClient>(Region);
                    }

                    return _autoScalingClient;
                }
            }

            IAmazonS3 _s3Client;
            public IAmazonS3 S3Client
            {
                get
                {
                    if (_s3Client == null)
                    {
                        _s3Client = Account.CreateServiceClient<AmazonS3Client>(Region);
                    }

                    return _s3Client;
                }
            }

            IAmazonEC2 _ec2Client;
            public IAmazonEC2 EC2Client
            {
                get
                {
                    if (_ec2Client == null)
                    {
                        _ec2Client = Account.CreateServiceClient<AmazonEC2Client>(Region);
                    }

                    return _ec2Client;
                }
            }

            IAmazonCloudFormation _cloudFormationClient;
            public IAmazonCloudFormation CloudFormationClient
            {
                get
                {
                    if (_cloudFormationClient == null)
                    {
                        _cloudFormationClient = Account.CreateServiceClient<AmazonCloudFormationClient>(Region);
                    }

                    return _cloudFormationClient;
                }
            }

            IAmazonElasticLoadBalancing _elbClient;
            public IAmazonElasticLoadBalancing ELBClient
            {
                get
                {
                    if (_elbClient == null)
                    {
                        _elbClient = Account.CreateServiceClient<AmazonElasticLoadBalancingClient>(Region);
                    }

                    return _elbClient;
                }
            }


            private ClientFactory() { }
        }

        public FetchStackConfigWorker(AccountViewModel accountViewModel,
                                      ToolkitRegion region,
                                      Stack runningStack,
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
                Factory = new ClientFactory(accountViewModel, region),
                RunningStack = runningStack,
                DefaultBucketName = DeploymentControllerBase.DefaultBucketName(accountViewModel, region),
                DefaultConfigFileKey = DeploymentControllerBase.DefaultConfigFileKey(runningStack.StackName),
                Logger = logger
            });
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            var workerData = (WorkerData)e.Argument;
            JsonData configData = null;

            try
            {
                Reservation reservation = FetchRunningInstance(workerData.Factory, workerData.RunningStack.StackName);
                if (reservation == null)
                    throw new Exception("Failed to obtain reservation for running instance, cannot proceed with reading prior configuration");

                configData = CloudFormationUtil.GetConfig(workerData.Factory.S3Client, workerData.RunningStack, reservation,
                    workerData.DefaultBucketName, workerData.DefaultConfigFileKey);
            }
            catch (Exception exc)
            {
                workerData.Logger.Error(GetType().FullName + ", exception in Worker", exc);
            }

            e.Result = configData;
        }

        /// <summary>
        /// Have to hope that the instance we choose is up-to-date with respect to the config we
        /// want to decrypt
        /// </summary>
        /// <returns></returns>
        Reservation FetchRunningInstance(ClientFactory clientFactory, string stackName)
        {
            Reservation reservation = null;

            var stackResources = AmazonCloudFormationClientExt.GetStackResources(clientFactory.CloudFormationClient, stackName);

            Dictionary<string, object> fetchedDescribes = new Dictionary<string, object>();

            var instanceIds = AmazonCloudFormationClientExt.GetListOfInstanceIdsForStack(clientFactory.AutoScalingClient, 
                                                                                         clientFactory.ELBClient, 
                                                                                         stackResources, 
                                                                                         fetchedDescribes);

            if (instanceIds != null && instanceIds.Count > 0)
            {
                var describeInstancesRequest = new DescribeInstancesRequest() { InstanceIds = instanceIds.ToList() };
                var describeInstanceResponse = clientFactory.EC2Client.DescribeInstances(describeInstancesRequest);
                reservation = describeInstanceResponse.Reservations[0];
            }

            return reservation;
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
                _callback(e.Result as JsonData);
        }

        private FetchStackConfigWorker() { }

    }
}
