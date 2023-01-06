using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.DynamoDB.Model;
using Amazon.AWSToolkit.DynamoDB.Nodes;
using Amazon.AWSToolkit.DynamoDB.Util;
using Amazon.AWSToolkit.DynamoDB.View;
using Amazon.AWSToolkit.DynamoDB.View.Columns;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;

using log4net;

namespace Amazon.AWSToolkit.DynamoDB.Controller
{
    public class TableBrowserController : BaseContextCommand
    {
        const int UNFILTER_LIMIT = 100;
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(TableBrowserController));

        private readonly ToolkitContext _toolkitContext;

        TableBrowserControl _control;
        TableBrowserModel _model;
        DynamoDBTableViewModel _rootModel;
        Search _lastSearch;
        Table _lastTable;

        public TableBrowserController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public override ActionResults Execute(IViewModel model)
        {
            this._rootModel = model as DynamoDBTableViewModel;
            if (this._rootModel == null)
                return new ActionResults().WithSuccess(false);

            this._model = new TableBrowserModel(this._rootModel.Table);
            this._model.SettingsKey = string.Format("{0}-{1}", this._rootModel.AccountViewModel.SettingsUniqueKey, this._rootModel.Table);

            this._control = new TableBrowserControl(this);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);

            return new ActionResults().WithSuccess(true);
        }

        public TableBrowserModel Model => this._model;

        public void LoadModel()
        {
            try
            {
                var response = this._rootModel.DynamoDBClient.DescribeTable(new DescribeTableRequest
                {
                    TableName = this.Model.TableName
                });
                this._model.TableDescription = response.Table;
                this._lastTable = Table.LoadTable(this._rootModel.DynamoDBClient, this.Model.TableName, DynamoDBEntryConversion.V2);
            }
            catch (Exception e)
            {
                LOGGER.Error("Failed to find table " + this.Model.TableName + ".", e);
                throw new ApplicationException("Failed to find table " + this.Model.TableName + ".");
            }
        }

        public DynamoDBColumnDefinition[] Execute()
        {
            this._model.HasData = false;
            this._model.HasMoreRows = true;
            this._model.Documents.Clear();
            this._lastSearch = null;

            ScanFilter filter = new ScanFilter();
            foreach(var sc in this._model.ScanConditions)
            {
                if (string.IsNullOrEmpty(sc.AttributeName))
                    continue;

                ScanOperator op = sc.Operator.Operator;
                DynamoDBEntry[] values = null;
                if (op != ScanOperator.IsNotNull && op != ScanOperator.IsNull)
                {
                    if (sc.IsSet || op == ScanOperator.Between)
                    {
                        if (op == ScanOperator.Between && sc.Values.Count() != 2)
                            throw new Exception("When using the Between operator you must specify 2 values. The first value is the less than or equal condition and the second value being the greater than or equal condition.");

                        var entries = new List<DynamoDBEntry>();
                        foreach (var token in sc.Values)
                        {
                            var cleanedToken = token.Trim();
                            if (!string.IsNullOrEmpty(cleanedToken))
                            {
                                entries.Add(new Primitive(cleanedToken, sc.IsNumeric));
                            }
                        }

                        values = entries.ToArray();
                    }
                    else
                    {
                        values = new DynamoDBEntry[] { new Primitive(sc.Values.FirstOrDefault<string>(), sc.IsNumeric) };
                    }
                }
                else
                    values = new DynamoDBEntry[0];

                filter.AddCondition(sc.AttributeName, op, values);
            }

            var config = new ScanOperationConfig();
            config.Filter = filter;

            if (this._model.ScanConditions.Count == 0)
                config.Limit = UNFILTER_LIMIT;

            this._lastSearch = ScanLastTable(config);
            return FetchFromLastSearch(1);
        }

        private Search ScanLastTable(ScanOperationConfig config)
        {
            Result result = Result.Failed;
            try
            {
                var scanResults = this._lastTable.Scan(config);
                result = Result.Succeeded;

                return scanResults;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error scanning DynamoDB table", e);
                result = Result.Failed;
                throw;
            }
            finally
            {
                RecordTableScan(result);
            }
        }

        private void RecordTableScan(Result result)
        {
            try
            {
                _toolkitContext.TelemetryLogger.RecordDynamodbFetchRecords(new DynamodbFetchRecords()
                {
                    AwsAccount = GetAccountId(),
                    AwsRegion = this._rootModel?.DynamoDBRootViewModel?.Region?.Id ?? MetadataValue.Invalid,
                    DynamoDbFetchType = DynamoDbFetchType.Scan,
                    Result = result,
                });
            }
            catch (Exception e)
            {
                LOGGER.Error(e);
            }
        }

        public void RecordViewTable(ActionResults results)
        {
            _toolkitContext.RecordDynamoDbView(DynamoDbTarget.Table, results,
                _rootModel.DynamoDBRootViewModel.AwsConnectionSettings);
        }

        public void RecordEditTable(ActionResults results)
        {
            _toolkitContext.RecordDynamoDbEdit(DynamoDbTarget.Table, results,
                _rootModel.DynamoDBRootViewModel.AwsConnectionSettings);
        }

        private string GetAccountId()
        {
            var accountId =
                _rootModel?.DynamoDBRootViewModel?.AwsConnectionSettings?.GetAccountId(
                    _toolkitContext.ServiceClientManager);

            if (string.IsNullOrWhiteSpace(accountId))
            {
                return MetadataValue.Invalid;
            }

            return accountId;
        }

        public DynamoDBColumnDefinition[] FetchFromLastSearch(int pages)
        {
            var columnDefs = new Dictionary<string, DynamoDBColumnDefinition>();
            if (this._lastSearch == null || this._lastSearch.IsDone)
                return new DynamoDBColumnDefinition[0];

            for (int i = 0; i < pages && !this._lastSearch.IsDone; i++)
            {
                var documents = this._lastSearch.GetNextSet();
                foreach (var document in documents)
                {
                    this._model.Documents.Add(document);

                    foreach (var attributeName in document.GetAttributeNames())
                    {
                        DynamoDBEntry entry;
                        if (!columnDefs.ContainsKey(attributeName) && document.TryGetValue(attributeName, out entry))
                        {
                            string type = null;
                            if (entry is Primitive)
                            {
                                type = DynamoDBConstants.ToConstant(((Primitive)entry).Type, false);
                            }
                            else if(entry is PrimitiveList)
                            {
                                type = DynamoDBConstants.ToConstant(((PrimitiveList)entry).Type, true);
                            }

                            columnDefs.Add(attributeName, new DynamoDBColumnDefinition(attributeName, type));
                        }
                    }
                }
            }

            this._model.HasData = this._model.Documents.Count > 0;
            this._model.HasMoreRows = !this._lastSearch.IsDone;

            return columnDefs.Values.ToArray();
        }

        public void RefreshTableStatus()
        {
            var response = this._rootModel.DynamoDBClient.DescribeTable(new DescribeTableRequest
            {
                TableName = this.Model.TableName
            });
            this._model.TableDescription = response.Table;
        }

        public string AddAttribute()
        {
            AddAttributeController controller = new AddAttributeController();
            if (controller.Execute())
            {
                string attributeName = controller.Model.AttributeName;
                return attributeName;
            }

            return null;
        }

        public void CommitChanges()
        {
            var documentBatchWrite = this._lastTable.CreateBatchWrite();
                        
            foreach (var document in this.Model.Documents)
            {
                if (document.IsDirty())
                {
                    if (document.Contains(this.Model.HashKeyElement.AttributeName))
                    {
                        if (document.IsAttributeChanged(this.Model.HashKeyElement.AttributeName))
                        {
                            documentBatchWrite.AddDocumentToPut(document);
                        }
                        else
                        {
                            this._lastTable.UpdateItem(document);
                        }
                    }
                }
            }

            foreach (var document in this.Model.DeletedDocuments)
            {
                documentBatchWrite.AddItemToDelete(document);
            }

            documentBatchWrite.Execute();
            this.Model.DeletedDocuments.Clear();
        }

        public void ExportResults(string filename, IEnumerable<string> atributeNames)
        {
            using (var writer = new StreamWriter(filename))
            {
                writer.WriteLine(generateColumnHeader(atributeNames));

                foreach (var item in this._model.Documents)
                {
                    var line = generatedCommaDelimitedString(item, atributeNames);
                    writer.WriteLine(line);
                }
            }
        }

        string generateColumnHeader(IEnumerable<string> attributeNames)
        {
            return AWSToolkit.Util.StringUtils.CreateCommaDelimitedList(attributeNames);
        }

        string generatedCommaDelimitedString(Document item, IEnumerable<string> attributeNames)
        {
            StringBuilder sb = new StringBuilder();


            foreach (var attributeName in attributeNames)
            {
                if (sb.Length > 0)
                    sb.Append(", ");

                DynamoDBEntry entry;
                if (item.TryGetValue(attributeName, out entry))
                {
                    if (entry is PrimitiveList)
                    {
                        var values = entry.AsListOfString();
                        var comma = AWSToolkit.Util.StringUtils.CreateCommaDelimitedList(values);
                        sb.AppendFormat("\"{0}\"", comma.Replace("\"", "\"\""));
                    }
                    else
                    {
                        var value = entry.AsString();
                        if (value.Contains(','))
                            sb.AppendFormat("\"{0}\"", value.Replace("\"", "\"\""));
                        else
                            sb.Append(value);
                    }
                }
            }

            return sb.ToString();
        }
    }
}
