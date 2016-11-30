using System;
using System.Collections.Generic;
using System.ComponentModel;

using log4net;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;

namespace Amazon.AWSToolkit.SimpleWorkers
{
    public class QueryKMSKeysWorker
    {
        public delegate void DataAvailableCallback(IEnumerable<KeyListEntry> keys, IEnumerable<AliasListEntry> aliases);

        readonly DataAvailableCallback _callback;

        struct WorkerData
        {
            public IAmazonKeyManagementService KMSClient { get; set; }
            public ILog Logger { get; set; }
        }

        /// <summary>
        /// Do an asynchronous query for all keys available to the user in the client's
        /// bound region.
        /// </summary>
        /// <param name="kmsClient"></param>
        /// <param name="logger"></param>
        /// <param name="callback"></param>
        public QueryKMSKeysWorker(IAmazonKeyManagementService kmsClient, ILog logger, DataAvailableCallback callback)
        {
            _callback = callback;

            var bw = new BackgroundWorker();
            bw.DoWork += Worker;
            bw.RunWorkerCompleted += WorkerCompleted;
            bw.RunWorkerAsync(new WorkerData
            {
                KMSClient = kmsClient,
                Logger = logger
            });
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            var workerData = (WorkerData)e.Argument;

            var keys = new List<KeyListEntry>();
            var aliases = new List<AliasListEntry>();

            try
            {
                workerData.Logger.Info("Querying KMS keys");
                string nextMarker = null;
                do
                {
                    var keyQueryResponse = workerData.KMSClient.ListKeys(new ListKeysRequest{ Marker = nextMarker });
                    keys.AddRange(keyQueryResponse.Keys);
                    nextMarker = keyQueryResponse.NextMarker;
                } while (!string.IsNullOrEmpty(nextMarker));

                workerData.Logger.Info("Querying KMS key aliases");
                nextMarker = null;
                do
                {
                    var aliasQueryResponse = workerData.KMSClient.ListAliases(new ListAliasesRequest { Marker = nextMarker });
                    aliases.AddRange(aliasQueryResponse.Aliases);
                    nextMarker = aliasQueryResponse.NextMarker;
                } while (!string.IsNullOrEmpty(nextMarker));
            }
            catch (Exception exc)
            {
                workerData.Logger.Error(GetType().FullName + ", exception in Worker", exc);
            }

            e.Result = new object[] { keys, aliases };
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
            {
                var results = e.Result as object[];
                _callback(results[0] as List<KeyListEntry>, results[1] as List<AliasListEntry>);
            }
        }

        private QueryKMSKeysWorker() { }
    }
}
