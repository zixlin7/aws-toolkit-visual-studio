using System;
using System.Threading;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.DynamoDB.Nodes;
using Amazon.AWSToolkit.DynamoDB.View;
using Amazon.AWSToolkit.DynamoDB.Model;
using Amazon.AWSToolkit.DynamoDB.Util;
using Amazon.AWSToolkit.Telemetry;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.AWSToolkit.Clients;

namespace Amazon.AWSToolkit.DynamoDB.Controller
{
    public class StreamPropertiesController : BaseContextCommand
    {
        private readonly ToolkitContext _toolkitContext;

        IAmazonDynamoDBStreams _dynamoDBStreamsClient;

        StreamPropertiesControl _control;
        StreamPropertiesModel _model;
        DynamoDBTableViewModel _rootModel;
        ActionResults _results;

        public StreamPropertiesController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public override ActionResults Execute(IViewModel model)
        {
            ActionResults actionResults = null;

            void Invoke() => actionResults = ViewTableStream(model);

            void Record(ITelemetryLogger _)
            {
                _toolkitContext.RecordDynamoDbView(DynamoDbTarget.TableStream, actionResults,
                    _rootModel?.DynamoDBRootViewModel?.AwsConnectionSettings);
            }

            _toolkitContext.TelemetryLogger.InvokeAndRecord(Invoke, Record);
            return actionResults;
        }

        public ActionResults ViewTableStream(IViewModel model)
        {
            this._rootModel = model as DynamoDBTableViewModel;
            if (this._rootModel == null)
            {
                return ActionResults.CreateFailed();
            }

            var serviceRoot = this._rootModel.Parent as ServiceRootViewModel;
            if (serviceRoot == null)
            {
                return ActionResults.CreateFailed();
            }

            this._dynamoDBStreamsClient =
                _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonDynamoDBStreamsClient>(
                    _rootModel.DynamoDBRootViewModel.AwsConnectionSettings);

            this._model = new StreamPropertiesModel(this._rootModel.Table);
            this._control = new StreamPropertiesControl(this);
            if (!_toolkitContext.ToolkitHost.ShowModal(this._control))
            {
                return ActionResults.CreateCancelled();
            }

            return _results ?? ActionResults.CreateFailed();
        }

        public StreamPropertiesModel Model => this._model;

        public void LoadModel()
        {
            DescribeTableResponse response = null;
            try
            {
                response = this._rootModel.DynamoDBClient.DescribeTable(new DescribeTableRequest { TableName = this.Model.TableName });
            }
            catch (Exception)
            {
                throw new ApplicationException("Failed to find table " + this.Model.TableName + ".");
            }

            if(response.Table.StreamSpecification != null)
            {
                this._model.EnableStream = response.Table.StreamSpecification.StreamEnabled;
                if(this._model.EnableStream)
                {
                    this._model.StreamARN = response.Table.LatestStreamArn;
                    this._model.SelectedViewType = this.Model.FindViewType(response.Table.StreamSpecification.StreamViewType);
                }
            }
        }

        public bool Persist()
        {
            var updateResults = UpdateTableStream();

            _toolkitContext.RecordDynamoDbEdit(DynamoDbTarget.TableStream, updateResults,
                _rootModel?.DynamoDBRootViewModel?.AwsConnectionSettings);

            _results = updateResults;
            return updateResults.Success;
        }

        private ActionResults UpdateTableStream()
        {
            try
            {
                var describeResponse = this._rootModel.DynamoDBClient.DescribeTable(new DescribeTableRequest { TableName = this.Model.TableName });

                var existingStreamEnabled = describeResponse.Table.StreamSpecification != null ? describeResponse.Table.StreamSpecification.StreamEnabled : false;
                var existingStreamViewType = describeResponse.Table.StreamSpecification != null ? describeResponse.Table.StreamSpecification.StreamViewType : null;
                
                var newStreamEnabled = this.Model.EnableStream;
                var newStreamViewType = this.Model.SelectedViewType != null ? this.Model.SelectedViewType.ViewType : null;

                if(existingStreamEnabled == newStreamEnabled && 
                    existingStreamViewType == newStreamViewType)
                {
                    return new ActionResults()
                        .WithSuccess(true)
                        .WithFocalname(this._model.TableName)
                        .WithShouldRefresh(false);
                }

                // Changing view type. The stream must be disabled first and then renabled with the new view type.
                if (newStreamEnabled &&
                    existingStreamViewType != null &&
                    existingStreamViewType != newStreamViewType)
                {
                    var disableRequest = new UpdateTableRequest
                    {
                        TableName = this.Model.TableName,
                        StreamSpecification = new StreamSpecification
                        {
                            StreamEnabled = false
                        }
                    };
                    this._rootModel.DynamoDBClient.UpdateTable(disableRequest);
                    this.WaitForStreamToDisable();
                }

                var updateRequest = new UpdateTableRequest
                {
                    TableName = this.Model.TableName,
                    StreamSpecification = new StreamSpecification
                    {
                        StreamEnabled = newStreamEnabled
                    }
                };

                if(newStreamEnabled)
                {
                    updateRequest.StreamSpecification.StreamViewType = newStreamViewType;
                }

                this._rootModel.DynamoDBClient.UpdateTable(updateRequest);

                return new ActionResults()
                    .WithSuccess(true)
                    .WithFocalname(this._model.TableName)
                    .WithShouldRefresh(false);
            }
            catch (Exception e)
            {
                _toolkitContext.ToolkitHost.ShowError("Error updating stream: " + e.Message);
                return ActionResults.CreateFailed(e);
            }
        }

        private void  WaitForStreamToDisable()
        {
            var request = new DescribeStreamRequest
            {
                StreamArn = this.Model.StreamARN
            };

            long start = DateTime.Now.Ticks;
            DescribeStreamResponse response = null;
            do
            {
                Thread.Sleep(3);
                response = this._dynamoDBStreamsClient.DescribeStream(request);
            } while (response.StreamDescription.StreamStatus != StreamStatus.DISABLED && new TimeSpan(DateTime.Now.Ticks - start).TotalMinutes < 3);
        }

    }
}
