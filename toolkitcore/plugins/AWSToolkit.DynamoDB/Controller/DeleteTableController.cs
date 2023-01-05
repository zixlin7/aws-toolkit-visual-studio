using System;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.DynamoDB.Nodes;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Telemetry;
using Amazon.DynamoDBv2.Model;

namespace Amazon.AWSToolkit.DynamoDB.Controller
{
    public class DeleteTableController : BaseContextCommand
    {
        private readonly ToolkitContext _toolkitContext;

        public DeleteTableController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public override ActionResults Execute(IViewModel model)
        {
            var result = DeleteTable(model);
            RecordMetric(result, model);
            return result;
        }

        public ActionResults DeleteTable(IViewModel model)
        {
            DynamoDBTableViewModel tableModel = model as DynamoDBTableViewModel;
            if (tableModel == null)
            {
                return ActionResults.CreateFailed();
            }

            var msg = $"Are you sure you want to delete DynamoDB Table: {model.Name}";

            if (!_toolkitContext.ToolkitHost.Confirm("Delete Table", msg))
            {
                return ActionResults.CreateCancelled();
            }

            try
            {
                var request = new DeleteTableRequest
                {
                    TableName = model.Name
                };
                tableModel.DynamoDBClient.DeleteTable(request);

                return new ActionResults()
                    .WithSuccess(true)
                    .WithFocalname(model.Name)
                    .WithShouldRefresh(true);
            }
            catch (Exception e)
            {
                _toolkitContext.ToolkitHost.ShowError($"Error deleting table:{Environment.NewLine}{e.Message}");
                return ActionResults.CreateFailed(e);
            }
        }

        private void RecordMetric(ActionResults results, IViewModel viewModel)
        {
            var tableModel = viewModel as DynamoDBTableViewModel;
            var awsConnectionSettings = tableModel?.DynamoDBRootViewModel.AwsConnectionSettings;

            var data = new DynamodbDeleteTable()
            {
                AwsAccount = awsConnectionSettings?.GetAccountId(_toolkitContext.ServiceClientManager) ?? MetadataValue.Invalid,
                AwsRegion = awsConnectionSettings?.Region?.Id ?? MetadataValue.Invalid,
                Result = results.AsTelemetryResult(),
                Reason = TelemetryHelper.GetMetricsReason(results.Exception),
            };

            _toolkitContext.TelemetryLogger.RecordDynamodbDeleteTable(data);
        }
    }
}
