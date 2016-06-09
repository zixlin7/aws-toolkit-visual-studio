using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

using Amazon.AWSToolkit;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Lambda.View;
using Amazon.AWSToolkit.Lambda.Model;
using Amazon.AWSToolkit.Lambda.Nodes;

using Amazon.Auth.AccessControlPolicy;

using Amazon.Lambda;
using Amazon.Lambda.Model;

using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;

using log4net;

namespace Amazon.AWSToolkit.Lambda.Controller
{
    public class ViewFunctionController : BaseContextCommand
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(ViewFunctionController));

        ViewFunctionModel _model;
        ViewFunctionControl _control;
        IAmazonLambda _lambdaClient;
        LambdaFunctionViewModel _viewModel;

        AccountViewModel _account;
        string _region;

        AmazonCloudWatchLogsClient _logsClient;

        public override ActionResults Execute(IViewModel model)
        {
            this._viewModel = model as LambdaFunctionViewModel;
            if (this._viewModel == null)
                return new ActionResults().WithSuccess(false);


            this._lambdaClient = this._viewModel.LambdaClient;
            this._model = new ViewFunctionModel(this._viewModel.FunctionName, this._viewModel.FunctionArn);
            this._control = new ViewFunctionControl(this);

            this._account = ((LambdaFunctionViewModel)this._viewModel).LambdaRootViewModel.AccountViewModel;
            this._region = ((LambdaFunctionViewModel)this._viewModel).LambdaRootViewModel.CurrentEndPoint.RegionSystemName;
            RegionEndPointsManager.RegionEndPoints endPoints = RegionEndPointsManager.Instance.GetRegion(this._region);

            var endpointURL = endPoints.GetEndpoint(RegionEndPointsManager.CLOUDWATCH_LOGS_NAME).Url;
            if (endpointURL != null)
            {
                var logsConfig = new AmazonCloudWatchLogsConfig();
                logsConfig.ServiceURL = endpointURL;
                this._logsClient = new AmazonCloudWatchLogsClient(this._account.Credentials, logsConfig);
            }


            ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);

            return new ActionResults()
                    .WithSuccess(true);
        }

        public ViewFunctionModel Model
        {
            get { return this._model; }
        }

        public void LoadModel()
        {
            Refresh();
        }

        public void Refresh()
        {
            RefreshFunctionConfiguration();
            RefreshEventSources();
            RefreshLogs();
        }

        public void RefreshFunctionConfiguration()
        {
            var response = this._lambdaClient.GetFunctionConfiguration(this._model.FunctionName);

            this._model.CodeSize = response.CodeSize;
            this._model.Description = response.Description;
            this._model.FunctionArn = response.FunctionArn;
            this._model.Handler = response.Handler;
            this._model.LastModified = DateTime.Parse(response.LastModified);
            this._model.MemorySize = response.MemorySize;
            this._model.Role = response.Role;
            this._model.Timeout = response.Timeout;
            this._model.Runtime = RuntimeOption.ALL_OPTIONS.FirstOrDefault(x => string.Equals(x.Value, response.Runtime.ToString(), StringComparison.OrdinalIgnoreCase));

            this._model.IsDirty = false;
        }

        public void RefreshEventSources()
        {
            var request = new ListEventSourceMappingsRequest{FunctionName = this._model.FunctionName};
            ListEventSourceMappingsResponse response = null;

            this._model.EventSources.Clear();
            do {
                if(response != null)
                    request.Marker = response.NextMarker;
                
                response = this._lambdaClient.ListEventSourceMappings(request);

                foreach(var eventSourceConfiguration in response.EventSourceMappings)
                {
                    if (!eventSourceConfiguration.State.Equals("enabled", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var wrapper = new EventSourceWrapper(eventSourceConfiguration);
                    this._model.EventSources.Add(wrapper);
                }

            }while(!string.IsNullOrEmpty(response.NextMarker));


            var policyStr = GetPolicy();
            if (!string.IsNullOrEmpty(policyStr))
            {
                Policy policy = Policy.FromJson(policyStr);
                foreach (var statement in policy.Statements)
                {
                    // Unexpected statement that we don't know how to handle. The Lambda API should only allow one of each type.
                    if (statement.Principals.Count != 1 || statement.Resources.Count != 1 || statement.Actions.Count != 1 || statement.Effect != Statement.StatementEffect.Allow ||
                        statement.Conditions.Count != 1 || statement.Conditions[0].Values.Count() != 1)
                        continue;

                    var wrapper = new EventSourceWrapper(statement);
                    this._model.EventSources.Add(wrapper);
                }
            }

        }

        // Lambda throws exception if the policy does not exist.
        private string GetPolicy()
        {
            try
            {
                return this._lambdaClient.GetPolicy(new GetPolicyRequest { FunctionName = this._model.FunctionName }).Policy;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string CloudWatchLogGroup
        {
            get
            {
                return string.Format("/aws/lambda/{0}", this._model.FunctionName);
            }
        }

        public void RefreshLogs()
        {
            this._model.Logs.Clear();
            if (this._logsClient == null)
                return;

            var request = new DescribeLogStreamsRequest
            {
                LogGroupName = this.CloudWatchLogGroup,
                OrderBy = OrderBy.LastEventTime,
                Descending = true
            };

            try
            {
                this._logsClient.BeginDescribeLogStreams(request, this.RefreshLogsCallback, null);
            }
            catch (Exception e)
            {
                // Most likely lambda doesn't have permission to write logs.
                LOGGER.Info("Error getting cloudwatch logs for " + request.LogGroupName, e);
            }
        }

        private void RefreshLogsCallback(IAsyncResult result)
        {
            try
            {
                var response = this._logsClient.EndDescribeLogStreams(result);

                ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke((Action)(() =>
                {
                    foreach (var stream in response.LogStreams)
                    {
                        this._model.Logs.Add(new LogStreamWrapper(stream));
                    }
                }));
            }
            catch (Exception e)
            {
                // Most likely lambda doesn't have permission to write logs.
                LOGGER.Info("Error getting cloudwatch logs", e);
            }
        }

        public void DownloadLog(string logStream)
        {
            var fileName = Path.GetTempFileName() + ".txt";

            using (var writer = new StreamWriter(fileName))
            {
                var request = new GetLogEventsRequest
                {
                    LogGroupName = this.CloudWatchLogGroup,
                    LogStreamName  = logStream
                };

                var response = this._logsClient.GetLogEvents(request);
                foreach (var evnt in response.Events)
                {
                    writer.Write("{0:yyyy-mm-dd HH:mm:ss}: {1}", evnt.Timestamp.ToLocalTime(), evnt.Message);
                }
            }

            var process = new Process();
            process.StartInfo.FileName = fileName;
            process.StartInfo.Verb = "Open";
            process.Start();
        }

        public void DeleteLog(LogStreamWrapper logStream)
        {
            var request = new DeleteLogStreamRequest
            {
                LogGroupName = this.CloudWatchLogGroup,
                LogStreamName = logStream.LogStreamName
            };

            this._logsClient.DeleteLogStream(request);
            this._model.Logs.Remove(logStream);
        }

        public void UpdateConfiguration()
        {
            var request = new UpdateFunctionConfigurationRequest
            {
                Description = this._model.Description,
                FunctionName = this._model.FunctionName,
                Handler = this._model.Handler,
                MemorySize = this._model.MemorySize,
                Timeout = this._model.Timeout,
                Runtime = this._model.Runtime.Value
            };
            var response = this._lambdaClient.UpdateFunctionConfiguration(request);

            this._model.LastModified = DateTime.Parse(response.LastModified);
            this._model.IsDirty = false;
        }

        public bool UploadNewFunctionSource()
        {
            var controller = new UploadFunctionController();
            var results = controller.Execute(this._lambdaClient, this._model.FunctionName);

            return results.Success;
        }

        public bool AddEventSource()
        {
            var controller = new AddEventSourceController();
            return controller.Execute(this._lambdaClient, this._account, this._region, this._model.FunctionArn, this._model.Role);
        }

        public void DeleteEventSource(EventSourceWrapper wrapper)
        {
            if (wrapper.Type == EventSourceWrapper.EventSourceType.Pull)
            {
                var request = new DeleteEventSourceMappingRequest
                {
                    UUID = wrapper.UUID
                };
                this._lambdaClient.DeleteEventSourceMapping(request);
            }
            else
            {
                var request = new RemovePermissionRequest 
                { 
                    FunctionName = this._model.FunctionName, 
                    StatementId = wrapper.UUID 
                };
                this._lambdaClient.RemovePermission(request);
            }
        }
    }
}
