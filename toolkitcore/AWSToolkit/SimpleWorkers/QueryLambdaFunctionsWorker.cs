using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Amazon.Lambda;
using Amazon.Lambda.Model;

using log4net;


namespace Amazon.AWSToolkit.SimpleWorkers
{
    public class QueryLambdaFunctionsWorker
    {
        public delegate void DataAvailableCallback(ICollection<FunctionConfiguration> data);
        DataAvailableCallback _callback;

        /// <summary>
        /// Perform synchronous fetch 
        /// </summary>
        /// <param name="client"></param>
        public static List<FunctionConfiguration> FetchFunctions(IAmazonLambda client, ILog logger)
        {
            List<FunctionConfiguration> functions = new List<FunctionConfiguration>();

            QueryLambdaFunctionsWorker workerObj = new QueryLambdaFunctionsWorker();
            workerObj.Query(client, logger, functions);
            return functions;
        }

        /// <summary>
        /// Perform async fetch
        /// </summary>
        /// <param name="ec2Client"></param>
        /// <param name="logger"></param>
        /// <param name="callback"></param>
        public QueryLambdaFunctionsWorker(IAmazonLambda client,
                                         ILog logger,
                                         DataAvailableCallback callback)
        {
            _callback = callback;

            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;

            bw.DoWork += new DoWorkEventHandler(Worker);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(WorkerCompleted);
            bw.RunWorkerAsync(new object[] { client, logger });
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            object[] args = e.Argument as object[];
            IAmazonLambda client = args[0] as IAmazonLambda;
            ILog logger = args[1] as ILog;

            List<FunctionConfiguration> functions = new List<FunctionConfiguration>();
            Query(client, logger, functions);
            e.Result = functions;
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
                _callback(e.Result as List<FunctionConfiguration>);
        }

        void Query(IAmazonLambda client, ILog logger, List<FunctionConfiguration> stacks)
        {
            try
            {
                ListFunctionsResponse response = null;
                ListFunctionsRequest request = new ListFunctionsRequest();

                do
                {
                    if (response != null)
                        request.Marker = response.NextMarker;

                    response = client.ListFunctions(request);
                    stacks.AddRange(response.Functions);
                } while (!string.IsNullOrEmpty(response.NextMarker));
            }
            catch (Exception exc)
            {
                logger.Error(GetType().FullName + ", exception in Query", exc);
            }
        }

        private QueryLambdaFunctionsWorker() { }
    }
}
