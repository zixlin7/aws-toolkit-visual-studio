using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

using Amazon.EC2;
using Amazon.EC2.Model;
using AMIImage = Amazon.EC2.Model.Image;

using log4net;

namespace Amazon.AWSToolkit.SimpleWorkers
{
    /// <summary>
    /// Wraps async call to fetch a set of AMI instance details based on some
    /// caller-defined criteria (currently hard-wired to Windows)
    /// </summary>
    public class QueryAMIInstancesWorker
    {
        public delegate void DataAvailableCallback(IEnumerable<AMIImage> data);
        DataAvailableCallback _callback;

        struct WorkerData
        {
            public IAmazonEC2 EC2Client { get; set; }
            public string Owner { get; set; }
            public Filter[] AdditionalFilters { get; set; }
            public ILog Logger { get; set; }
        }

        /// <summary>
        /// Perform synchronous fetch 
        /// </summary>
        /// <param name="ec2Client"></param>
        /// <param name="filterByOwner"></param>
        /// <param name="additionalFilters"></param>
        public static List<Image> FetchImages(IAmazonEC2 ec2Client,
                                              string filterByOwner,
                                              Filter[] additionalFilters,
                                              ILog logger)
        {
            WorkerData workerData = new WorkerData();
            workerData.EC2Client = ec2Client;
            workerData.Owner = filterByOwner;
            workerData.AdditionalFilters = additionalFilters;
            workerData.Logger = logger;

            List<Image> images = new List<Image>();

            QueryAMIInstancesWorker workerObj = new QueryAMIInstancesWorker();
            workerObj.QueryImages(workerData, images);
            return images;
        }

        /// <summary>
        /// Perform async fetch
        /// </summary>
        /// <param name="ec2Client"></param>
        /// <param name="filterByOwner"></param>
        /// <param name="additionalFilters"></param>
        /// <param name="logger"></param>
        /// <param name="callback"></param>
        public QueryAMIInstancesWorker(IAmazonEC2 ec2Client,
                                       string filterByOwner,
                                       Filter[] additionalFilters,
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
                EC2Client = ec2Client,
                Owner = filterByOwner,
                AdditionalFilters = additionalFilters,
                Logger = logger
            });
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            List<Image> images = new List<AMIImage>();
            var workerData = (WorkerData)e.Argument;
            QueryImages(workerData, images);
            e.Result = images;
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
                _callback(e.Result as List<Image>);
        }

        void QueryImages(WorkerData workerData, List<AMIImage> images)
        {
            try
            {
                DescribeImagesRequest req = new DescribeImagesRequest();

                if (!string.IsNullOrEmpty(workerData.Owner))
                    req.Owners = new List<string>(){ workerData.Owner};

                if (workerData.AdditionalFilters != null && workerData.AdditionalFilters.Length > 0)
                    req.Filters = workerData.AdditionalFilters.ToList();

                DescribeImagesResponse response = workerData.EC2Client.DescribeImages(req);
                // discovered not all images have names, so filter the results
                foreach (Image ami in response.Images)
                {
                    if (!string.IsNullOrEmpty(ami.Name))
                        images.Add(ami);
                }
            }
            catch (Exception exc)
            {
                workerData.Logger.Error(GetType().FullName + ", exception in QueryImages", exc);
            }
        }

        private QueryAMIInstancesWorker() { }
    }
}
