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
    public class QueryLambdaFunctionSamplesWorker
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

            QueryLambdaFunctionSamplesWorker workerObj = new QueryLambdaFunctionSamplesWorker();
            workerObj.Query(region, logger, samples);
            return samples;
        }

        /// <summary>
        /// Perform async fetch
        /// </summary>
        /// <param name="ec2Client"></param>
        /// <param name="logger"></param>
        /// <param name="callback"></param>
        public QueryLambdaFunctionSamplesWorker(string region,
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
            string content = S3FileFetcher.Instance.GetFileContent("LambdaSampleFunctions/SampleFunctionsManifest.xml", S3FileFetcher.CacheMode.PerInstance);
            if (content == null)
                return;

            try
            {
                var xdoc = XDocument.Load(new StringReader(content));

                var query = from s in xdoc.Root.Elements("function")
                            select new QueryLambdaFunctionSamplesWorker.SampleSummary()
                            {
                                File = s.Element("file").Value,
                                Description = s.Element("name").Value
                            };

                foreach (var function in query)
                {
                    stacks.Add(function);
                }
            }
            catch (Exception e)
            {
                logger.Error("Error parsing sample lambda function manifest", e);
            }
        }

        private QueryLambdaFunctionSamplesWorker() { }

        public class SampleSummary
        {
            public string Description
            {
                get;
                set;
            }

            public string File
            {
                get;
                set;
            }
        }
    }
}
