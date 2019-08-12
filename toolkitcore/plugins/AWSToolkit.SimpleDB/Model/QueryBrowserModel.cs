using System.Collections.Generic;
using System.Data;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.SimpleDB.Controller;

namespace Amazon.AWSToolkit.SimpleDB.Model
{
    public class QueryBrowserModel : BaseModel
    {
        public const int MAX_BATCH_SAVES_SIZE = 25;

        DataTable _dataTable = new DataTable();

        string _domain;
        public string Domain
        {
            get => this._domain;
            set
            {
                this._domain = value;
                base.NotifyPropertyChanged("Domain");
            }
        }

        public DataTable Results
        {
            get => this._dataTable;
            set 
            { 
                this._dataTable = value;
                base.NotifyPropertyChanged("HasData");
            }
        }

        public bool HasChanged(int rowNumber, int columnNumber)
        {
            if (this.Results.Rows.Count <= rowNumber)
                return false;

            DataRow row = this.Results.Rows[rowNumber];
            if (row.Table.Columns.Count <= columnNumber)
                return false;

            return HasChanged(row, columnNumber);
        }

        public static bool HasChanged(DataRow row, int columnNumber)
        {
            if (row.RowState == DataRowState.Added)
            {
                return !row.IsNull(columnNumber);
            }
            else
            {
                List<string> orignal = row[columnNumber, DataRowVersion.Original] as List<string>;
                if (orignal == null)
                    orignal = new List<string>();
                List<string> current = row[columnNumber] as List<string>;
                if (current == null)
                    current = new List<string>();

                if (orignal.Count != current.Count)
                {
                    return true;
                }

                for (int i = 0; i < orignal.Count; i++)
                {
                    if (!orignal[i].Equals(current[i]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool HasChanged(DataRowView row, string columnName)
        {
            if (row.Row.RowState == DataRowState.Added || row.Row.RowState == DataRowState.Detached)
            {
                return !row.Row.IsNull(columnName);
            }
            else
            {
                List<string> orignal = row.Row[columnName, DataRowVersion.Original] as List<string>;
                if (orignal == null)
                    orignal = new List<string>();
                List<string> current = row[columnName] as List<string>;
                if (current == null)
                    current = new List<string>();

                if (orignal.Count != current.Count)
                {
                    return true;
                }

                for (int i = 0; i < orignal.Count; i++)
                {
                    if (!orignal[i].Equals(current[i]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool ValidateItemName(DataTable table, string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Name is required column!");
                return false;
            }

            foreach (DataRow row in table.Rows)
            {
                if(itemName.Equals(row[QueryBrowserController.ITEM_NAME_COLUMN]))
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError(string.Format("{0} is a duplicate name", itemName));
                    return false;
                }
            }

            return true;
        }

        string _lastExecutedQuery;
        public string LastExecutedQuery
        {
            get => this._lastExecutedQuery;
            set
            {
                this._lastExecutedQuery = value;
                base.NotifyPropertyChanged("LastExecutedQuery");
            }
        }

        public bool IsLastQueryCountQuery()
        {
            if (string.IsNullOrEmpty(this.LastExecutedQuery))
                return false;

            if (this.LastExecutedQuery.ToLower().Contains("count(*)"))
                return true;

            return false;
        }

        string _nextToken;
        public string NextToken
        {
            get => this._nextToken;
            set
            {
                this._nextToken = value;
                base.NotifyPropertyChanged("NextToken");
                base.NotifyPropertyChanged("HasMoreRows");
            }
        }

        public bool HasData => this.Results != null && this.Results.Rows.Count > 0;

        public void RaiseHasDataEvent()
        {
            base.NotifyPropertyChanged("HasData");
        }

        public bool HasMoreRows => !string.IsNullOrEmpty(this.NextToken) && !IsLastQueryCountQuery();

        public string DeterminDomainFromLastExecutedQuery => DetermineDomain(this.LastExecutedQuery);

        public static string DetermineDomain(string query)
        {
            try
            {
                string domain = string.Empty;
                string[] tokens = query.Split(' ', '\t', '\r', '\n');
                for (int i = 0; i < tokens.Length; i++)
                {
                    if (tokens[i].ToLower().Trim().Equals("from"))
                    {
                        // Find the next non white space token
                        for (int j = i + 1; j < tokens.Length; j++)
                        {
                            if (!string.Empty.Equals(tokens[j].Trim()))
                            {
                                domain = tokens[j].Trim();
                                break;
                            }
                        }
                        break;
                    }
                }

                domain = trimQuotes(domain, "`");
                domain = trimQuotes(domain, "'");
                domain = trimQuotes(domain, "\"");

                return domain;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string trimQuotes(string token, string quoteType)
        {
            if (token.StartsWith(quoteType))
            {
                token = token.Substring(quoteType.Length);
            }
            if (token.EndsWith(quoteType))
            {
                token = token.Substring(0, token.Length - quoteType.Length);
            }

            return token;
        }

    }
}
