using System;
using System.Threading;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.DynamoDB.Nodes;
using Amazon.AWSToolkit.DynamoDB.View;
using Amazon.AWSToolkit.DynamoDB.Model;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Amazon.AWSToolkit.DynamoDB.Controller
{
    public class StreamPropertiesController : BaseContextCommand
    {
        IAmazonDynamoDBStreams _dynamoDBStreamsClient;

        StreamPropertiesControl _control;
        StreamPropertiesModel _model;
        DynamoDBTableViewModel _rootModel;
        ActionResults _results;

        public override ActionResults Execute(IViewModel model)
        {
            this._rootModel = model as DynamoDBTableViewModel;
            if (this._rootModel == null)
                return new ActionResults().WithSuccess(false);

            var serviceRoot = this._rootModel.Parent as ServiceRootViewModel;
            if (serviceRoot == null)
            {
                return new ActionResults().WithSuccess(false);
            }

            this._dynamoDBStreamsClient =
                this._rootModel.AccountViewModel.CreateServiceClient<AmazonDynamoDBStreamsClient>(serviceRoot.Region);

            this._model = new StreamPropertiesModel(this._rootModel.Table);
            this._control = new StreamPropertiesControl(this);
            ToolkitFactory.Instance.ShellProvider.ShowModal(this._control);

            if (this._results == null)
                return new ActionResults().WithSuccess(false);
            return this._results;
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
                    return true;
                }

                // Changing view type. The stream must be disabled first and then renabled with the new view type.
                if(newStreamEnabled &&
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

                this._results = new ActionResults()
                    .WithSuccess(true)
                    .WithFocalname(this._model.TableName)
                    .WithShouldRefresh(false);

                return true;
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error updating stream: " + e.Message);
                this._results = new ActionResults().WithSuccess(false);
                return false;
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
