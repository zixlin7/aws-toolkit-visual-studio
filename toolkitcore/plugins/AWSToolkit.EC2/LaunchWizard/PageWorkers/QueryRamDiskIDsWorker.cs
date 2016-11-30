using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;

using Amazon.EC2;
using Amazon.EC2.Model;

using Amazon.AWSToolkit.EC2.Model;

using log4net;

namespace Amazon.AWSToolkit.EC2.LaunchWizard.PageWorkers
{
    /// <summary>
    /// Worker used to fetch the set of ram disk ids associated with the selected image
    /// </summary>
    internal class QueryRamDiskIDsWorker
    {
        public delegate void DataAvailableCallback(ICollection<string> data);
        DataAvailableCallback _callback;

        struct WorkerData
        {
            public string AmiID { get; set; }
            public IAmazonEC2 EC2Client { get; set; }
            public ILog Logger { get; set; }
        }

        public QueryRamDiskIDsWorker(IAmazonEC2 ec2Client,
                                     string amiID,
                                     ILog logger,
                                     DataAvailableCallback callback)
        {
            _callback = callback;

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(Worker);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(WorkerCompleted);
            bw.RunWorkerAsync(new WorkerData()
            {
                AmiID = amiID,
                EC2Client = ec2Client,
                Logger = logger
            });
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            WorkerData workerData = (WorkerData)e.Argument;

            List<string> ramDiskIDs = new List<string>();
            try
            {
                DescribeImageAttributeRequest request
                        = new DescribeImageAttributeRequest()
                        {
                            ImageId = workerData.AmiID,
                            Attribute = "ramdisk"
                        };

                DescribeImageAttributeResponse response = workerData.EC2Client.DescribeImageAttribute(request);
                ramDiskIDs.Add(response.ImageAttribute.RamdiskId);
            }
            catch (Exception exc)
            {
                workerData.Logger.Error(GetType().FullName + ", exception in Worker", exc);
            }

            e.Result = ramDiskIDs;
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
                _callback(e.Result as List<string>);
        }

        private QueryRamDiskIDsWorker() { }
    }
}
