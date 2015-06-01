using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Text;


using log4net;

namespace Amazon.AWSToolkit.SimpleWorkers
{
    public class QueryCloudFormationSamplesWorker
    {
        public delegate void DataAvailableCallback(ICollection<SampleSummary> data);
        DataAvailableCallback _callback;


        /// <summary>
        /// Perform synchronous fetch 
        /// </summary>
        /// <param name="client"></param>
        public static List<SampleSummary> FetchSamples(string region, ILog logger)
        {
            List<SampleSummary> samples = new List<SampleSummary>();

            QueryCloudFormationSamplesWorker workerObj = new QueryCloudFormationSamplesWorker();
            workerObj.Query(region, logger, samples);
            return samples;
        }

        /// <summary>
        /// Perform async fetch
        /// </summary>
        /// <param name="ec2Client"></param>
        /// <param name="logger"></param>
        /// <param name="callback"></param>
        public QueryCloudFormationSamplesWorker(string region,
                                         ILog logger,
                                         DataAvailableCallback callback)
        {
            _callback = callback;

            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;

            bw.DoWork += new DoWorkEventHandler(Worker);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(WorkerCompleted);
            bw.RunWorkerAsync(new object[] { region, logger });
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            object[] args = e.Argument as object[];
            var region = args[0] as string;
            ILog logger = args[1] as ILog;

            List<SampleSummary> stacks = new List<SampleSummary>();
            Query(region, logger, stacks);
            e.Result = stacks;
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
                _callback(e.Result as List<SampleSummary>);
        }

        void Query(string region, ILog logger, List<SampleSummary> stacks)
        {
            string content = S3FileFetcher.Instance.GetFileContent("CloudFormationTemplates/SampleTemplateManifest.xml", S3FileFetcher.CacheMode.PerInstance);
            if (content == null)
                return;

            try
            {
                var xdoc = XDocument.Load(new StringReader(content));

                var query = from s in xdoc.Root.Elements("region").Elements("template")
                            where s.Parent.Attribute("systemname").Value == region
                            select new QueryCloudFormationSamplesWorker.SampleSummary()
                            {
                                URL = s.Attribute("location").Value,
                                Description = s.Value
                            };

                foreach (var template in query)
                {
                    stacks.Add(template);
                }
            }
            catch (Exception e)
            {
                logger.Error("Error parsing sample template manifest", e);
            }
        }

        private QueryCloudFormationSamplesWorker() { }

        public class SampleSummary
        {
            public string Description
            {
                get;
                set;
            }

            public string URL
            {
                get;
                set;
            }
        }
    }
}
