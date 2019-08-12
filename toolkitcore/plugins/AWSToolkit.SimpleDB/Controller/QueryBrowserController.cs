using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.SimpleDB.Nodes;
using Amazon.AWSToolkit.SimpleDB.View;
using Amazon.AWSToolkit.SimpleDB.Model;
using Amazon.SimpleDB;
using Amazon.SimpleDB.Model;
using Attribute = Amazon.SimpleDB.Model.Attribute;

namespace Amazon.AWSToolkit.SimpleDB.Controller
{
    public class QueryBrowserController : BaseContextCommand
    {
        public const int ITEM_NAME_COLUMN = 0;

        public const string ITEM_NAME_COLUMN_LABEL = "ItemName";
        public const int START_OF_ATTRIBUTE_COLUMNS = 1;

        IAmazonSimpleDB _sdbClient;
        QueryBrowserControl _control;
        QueryBrowserModel _model;
        SimpleDBDomainViewModel _rootModel;

        public QueryBrowserController()
        { 
        }

        public QueryBrowserController(IAmazonSimpleDB sdbClient, string domain)
        {
            this._sdbClient = sdbClient;
            this._model = new QueryBrowserModel();
            this._model.Domain = domain;
        }

        public IAmazonSimpleDB SimpleDBClient => this._sdbClient;

        public override ActionResults Execute(IViewModel model)
        {
            this._rootModel = model as SimpleDBDomainViewModel;
            if (this._rootModel == null)
                return new ActionResults().WithSuccess(false);

            this._sdbClient = this._rootModel.SimpleDBClient;
            this._model = new QueryBrowserModel();
            this._model.Domain = this._rootModel.Domain;

            this._control = new QueryBrowserControl(this, string.Format("SELECT * FROM `{0}` LIMIT 50", this._rootModel.Domain));
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);

            return new ActionResults().WithSuccess(true);
        }

        public QueryBrowserModel Model => this._model;

        public string AddAttribute()
        {
            AddAttributeController controller = new AddAttributeController();
            if (controller.Execute())
            {
                string attributeName = controller.Model.AttributeName;
                this._model.Results.DefaultView.Table.Columns.Add(new DataColumn(attributeName, typeof(List<string>)));
                return attributeName;
            }

            return null;
        }

        public void CommitChanges()
        {
            string domain = this._model.DeterminDomainFromLastExecutedQuery;
            if (string.IsNullOrEmpty(domain))
            {
                throw new ApplicationException("Unable to determine domain from query:\r\n" + this._model.LastExecutedQuery);
            }

            var batchDelete = new BatchDeleteAttributesRequest() { DomainName = domain };

            var batchPut = new BatchPutAttributesRequest() { DomainName = domain };

            DataTable changes = this._model.Results.GetChanges();
            if (changes != null)
            {
                foreach (DataRow row in changes.Rows)
                {
                    if (row.RowState == DataRowState.Modified || row.RowState == DataRowState.Added)
                    {
                        ReplaceableItem ritem;
                        DeletableItem ditem;

                        checkRowDifferences(row, out ritem, out ditem);

                        if (ritem.Attributes.Count > 0)
                        {
                            batchPut.Items.Add(ritem);
                        }
                        if (ditem.Attributes.Count > 0)
                        {
                            batchDelete.Items.Add(ditem);
                        }
                    }
                    else if (row.RowState == DataRowState.Deleted)
                    {
                        string itemName = row[ITEM_NAME_COLUMN, DataRowVersion.Original] as string;
                        batchDelete.Items.Add(new DeletableItem() { Name = itemName });

                    }

                    // Check to see if the max items for batch has been reached.  If so commit
                    // what as been collected so far.
                    commmitChangesIfGreater(batchDelete, batchPut, QueryBrowserModel.MAX_BATCH_SAVES_SIZE - 1);
                }

                // Commit changes to SimpleDB that hasn't already been commit from hitting the max batch size.
                commmitChangesIfGreater(batchDelete, batchPut, 0);
            }

            this._model.Results.AcceptChanges();
        }

        private void commmitChangesIfGreater(BatchDeleteAttributesRequest batchDelete, BatchPutAttributesRequest batchPut, int size)
        {
            if (batchDelete.Items.Count > size)
            {
                this._sdbClient.BatchDeleteAttributes(batchDelete);
                batchDelete.Items.Clear();
            }
            if (batchPut.Items.Count > size)
            {
                this._sdbClient.BatchPutAttributes(batchPut);
                batchPut.Items.Clear();
            }
        }

        private void checkRowDifferences(DataRow row, out ReplaceableItem ritem, out DeletableItem ditem)
        {
            ritem = new ReplaceableItem() { Name = row[ITEM_NAME_COLUMN] as string };

            ditem = new DeletableItem() { Name = row[ITEM_NAME_COLUMN] as string };

            for (int i = START_OF_ATTRIBUTE_COLUMNS; i < row.Table.Columns.Count; i++)
            {
                if (!QueryBrowserModel.HasChanged(row, i))
                    continue;

                DataColumn column = row.Table.Columns[i];
                List<string> values = row[column] as List<string>;

                if (values == null || values.Count == 0 || (values.Count == 1 && string.Empty.Equals(values[0])))
                {
                    ditem.Attributes = new List<Attribute>(){new Attribute(){Name = column.ColumnName}};
                }
                else
                {
                    foreach (string value in values)
                    {
                        ReplaceableAttribute rp = new ReplaceableAttribute()
                        {
                            Name = column.ColumnName,
                            Replace = true,
                            Value = value == null ? string.Empty : value
                        };
                        ritem.Attributes.Add(rp);
                    }
                }
            }
        }

        public void FetchMorePages(int pages)
        {
            int rowCount = 0;
            for (int i = 0; i < pages && this.Model.HasMoreRows; i++)
            {
                rowCount += fetchMoreRows();
            }

            ToolkitFactory.Instance.ShellProvider.UpdateStatus(string.Format("Fetch {0} rows of data", rowCount));
        }

        int fetchMoreRows()
        {
            if (string.IsNullOrEmpty(this.Model.LastExecutedQuery) || string.IsNullOrEmpty(this.Model.NextToken))
                return 0;

            var response = this.SimpleDBClient.Select(
                new SelectRequest()
                {
                    SelectExpression = this.Model.LastExecutedQuery,
                    NextToken = this.Model.NextToken
                });

            this.buildResultData(response);
            this.Model.NextToken = response.NextToken;

            return response.Items.Count;
        }

        public void ExecuteQuery(string query, bool useConsistentRead)
        {
            this.Model.Results = new System.Data.DataTable();
            this.Model.LastExecutedQuery = query;
            this.Model.NextToken = null;
            try
            {
                var response = this.SimpleDBClient.Select(
                    new SelectRequest()
                    {
                        SelectExpression = query,
                        ConsistentRead = useConsistentRead
                    });

                addRequiredColumns(response);
                buildAttributeColumns(response);
                buildResultData(response);
                this.Model.Results.AcceptChanges();
                
                this.Model.NextToken = response.NextToken;
                ToolkitFactory.Instance.ShellProvider.UpdateStatus(string.Format("Fetch {0} rows of data", response.Items.Count));
                this.Model.RaiseHasDataEvent();
            }
            catch
            {
                this.Model.LastExecutedQuery = string.Empty;
                throw;
            }
        }

        private void addRequiredColumns(SelectResponse selectResults)
        {
            this.Model.Results.Columns.Add(new DataColumn(ITEM_NAME_COLUMN_LABEL, typeof(string)));
        }

        private void buildResultData(SelectResponse selectResults)
        {
            foreach (var item in selectResults.Items)
            {
                var data = this.Model.Results.NewRow();
                data[ITEM_NAME_COLUMN] = item.Name;

                foreach (var attribute in item.Attributes)
                {
                    if (!data.Table.Columns.Contains(attribute.Name))
                        continue;

                    List<string> values = data[attribute.Name] as List<string>;
                    if(values == null)
                    {
                        values = new List<string>();
                        data[attribute.Name] = values;
                    }
                    values.Add(attribute.Value);
                }

                this.Model.Results.Rows.Add(data);
                data.AcceptChanges();
            }
        }

        private void buildAttributeColumns(SelectResponse selectResults)
        {
            var columnNames = new HashSet<string>();
            foreach (var item in selectResults.Items)
            {
                foreach (var attribute in item.Attributes)
                {
                    if (!columnNames.Contains(attribute.Name))
                        columnNames.Add(attribute.Name);
                }
            }

            foreach (var columnName in columnNames.OrderBy(x => x.ToLower()))
            {
                this.Model.Results.Columns.Add(columnName, typeof(List<string>));
            }
        }

        public void ExportResults(string filename)
        {
            using (var writer = new StreamWriter(filename))
            {
                writer.WriteLine(generateColumnHeader());

                foreach (DataRow item in this.Model.Results.Rows)
                {
                    var line = generatedCommaDelimitedString(item);
                    writer.WriteLine(line);
                }
            }
        }

        string generateColumnHeader()
        {
            var columnNames = new List<string>();
            foreach (DataColumn column in this.Model.Results.Columns)
                columnNames.Add(column.ColumnName);

            return AWSToolkit.Util.StringUtils.CreateCommaDelimitedList(columnNames);
        }

        string generatedCommaDelimitedString(DataRow item)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(item[0]);
            if (item.Table.Columns.Count == 1)
                return sb.ToString();

            for (int i = 1; i < item.Table.Columns.Count; i++)
            {
                sb.Append(", ");
                List<string> values = item[i] as List<string>;

                if (values == null || values.Count == 0)
                    continue;
                else if (values.Count == 1)
                {
                    if (values[0].Contains(','))
                        sb.AppendFormat("\"{0}\"", values[0].Replace("\"", "\"\""));
                    else
                        sb.Append(values[0]);
                }
                else
                {
                    var comma = AWSToolkit.Util.StringUtils.CreateCommaDelimitedList(values);
                    sb.AppendFormat("\"{0}\"", comma.Replace("\"", "\"\""));
                }
            }

            return sb.ToString();
        }
    }
}
