using System;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.DynamoDB.Nodes;
using Amazon.DynamoDBv2.Model;

namespace Amazon.AWSToolkit.DynamoDB.Controller
{
    public class DeleteTableController : BaseContextCommand
    {
        public override ActionResults Execute(IViewModel model)
        {
            DynamoDBTableViewModel tableModel = model as DynamoDBTableViewModel;
            if (tableModel == null)
                return new ActionResults().WithSuccess(false);

            var msg = string.Format("Are you sure you want to delete the {0} table?", model.Name);

            if (ToolkitFactory.Instance.ShellProvider.Confirm("Delete Table", msg))
            {
                try
                {
                    var request = new DeleteTableRequest
                    {
                        TableName = model.Name
                    };
                    tableModel.DynamoDBClient.DeleteTable(request);
                }
                catch (Exception e)
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Error deleting table: " + e.Message);
                    return new ActionResults().WithSuccess(false);
                }
                return new ActionResults()
                    .WithSuccess(true)
                    .WithFocalname(model.Name)
                    .WithShouldRefresh(true);
            }

            return new ActionResults().WithSuccess(false);
        }
    }
}
