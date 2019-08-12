using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.DynamoDB.Controller;
using Amazon.AWSToolkit.DynamoDB.View.Columns;
using log4net;
using Microsoft.Win32;

namespace Amazon.AWSToolkit.DynamoDB.View
{
    /// <summary>
    /// Interaction logic for TableBrowserControl.xaml
    /// </summary>
    public partial class TableBrowserControl : BaseAWSControl
    {
        const int FETCH_FEW_PAGES_SIZE = 10;
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(TableBrowserControl));

        TableBrowserController _controller;
        HashAndRangeKeyColumn _hashColumn;

        public TableBrowserControl(TableBrowserController controller)
        {
            this._controller = controller;
            InitializeComponent();

            this._ctlFetchFewPages.ToolTip = string.Format("Continue scanning an {0} additional times with the same conditions.", FETCH_FEW_PAGES_SIZE);
            this._scanConditionsControl.Initialize(this._controller);
        }

        public override string Title => "Table: " + this._controller.Model.TableName;

        public override string UniqueId => string.Format("DDB:Table:{0}", this._controller.Model.TableName);

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            updateUi();
            this._controller.Model.Documents.CollectionChanged += onDocumentChange;

            return this._controller.Model;
        }

        protected override void PostDataContextBound()
        {
            if (string.Equals(this._controller.Model.TableDescription.TableStatus, DynamoDBConstants.TABLE_STATUS_ACTIVE, StringComparison.InvariantCultureIgnoreCase))
                this.executeQuery();
        }

        void onDocumentChange(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
                this._ctlCommitChangesBtn.IsEnabled = true;
        }

        private void updateUi()
        {
            ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() => 
            {
                this._ctlBinaryKeysToolbar.Visibility = this._controller.Model.HasBinaryKeys ? Visibility.Visible : System.Windows.Visibility.Collapsed;
                var model = this._controller.Model;

                this._hashColumn = new HashAndRangeKeyColumn(model.HashKeyElement);
                this._ctlDataGrid.Columns.Add(this._hashColumn);

                if (model.RangeKeyElement != null)
                {
                    var column = new HashAndRangeKeyColumn(model.RangeKeyElement);
                    column.OnCommitCellEdit += new EventHandler<RoutedEventArgs>(column_OnCommitCellEdit);
                    this._ctlDataGrid.Columns.Add(column);

                    foreach (var secondaryIndex in model.SecondaryIndexes)
                    {
                        var secondaryIndexColumn = new HashAndRangeKeyColumn(secondaryIndex);
                        secondaryIndexColumn.OnCommitCellEdit+= column_OnCommitCellEdit;
                        this._ctlDataGrid.Columns.Add(secondaryIndexColumn);
                    }
                }                    
            }));
        }

        void column_OnCommitCellEdit(object sender, RoutedEventArgs e)
        {
            this._ctlCommitChangesBtn.IsEnabled = true;
        }

        private void onPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers == ModifierKeys.Control) && e.Key == System.Windows.Input.Key.R)
            {
                e.Handled = true;
                executeQuery();
            }
        }

        void onRefreshStatusClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var currentStatus = this._controller.Model.TableDescription.TableStatus;
                this._controller.RefreshTableStatus();
                var newStatus = this._controller.Model.TableDescription.TableStatus;

                // If status switches from creating to active then run the execute to get the table ready to add data.
                if (string.Equals(DynamoDBConstants.TABLE_STATUS_CREATING, currentStatus, StringComparison.InvariantCultureIgnoreCase) &&
                    string.Equals(DynamoDBConstants.TABLE_STATUS_ACTIVE, newStatus, StringComparison.InvariantCultureIgnoreCase))
                {
                    this.executeQuery();
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing table status", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing table status: " + e.Message);
            }
        }

        void onAddAttributeClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._ctlDataGrid.CommitEdit(DataGridEditingUnit.Row, true);
                string attributeName = this._controller.AddAttribute();
                if (!string.IsNullOrEmpty(attributeName))
                {
                    addCustomAttributeColumns(
                        new DynamoDBColumnDefinition[]{new DynamoDBColumnDefinition(attributeName, DynamoDBConstants.TYPE_STRING)});
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Error adding attribute", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error adding attribute: " + e.Message);
            }
        }

        void onCommitChangesClick(object sender, RoutedEventArgs evnt)
        {
            this.commitChanges();
        }

        bool commitChanges()
        {
            try
            {
                this._ctlDataGrid.CommitEdit(DataGridEditingUnit.Row, true);
                this._controller.CommitChanges();
                this._ctlCommitChangesBtn.IsEnabled = false;
                this._ctlDataGrid.Items.Refresh();
            }
            catch (ApplicationException e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError(e.Message);
                return false;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error committing changes", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error committing changes: " + e.Message);
                return false;
            }

            return true;
        }

        void onExecuteClick(object sender, RoutedEventArgs evnt)
        {
            executeQuery();
        }

        void executeQuery()
        {
            this._ctlDataGrid.CommitEdit(DataGridEditingUnit.Row, true);
            // Check if there is uncommited changes before executing a new query
            if (this._ctlCommitChangesBtn.IsEnabled)
            {
                if (ToolkitFactory.Instance.ShellProvider.Confirm("Commit Changes",
                            "There are uncommited changes.  Do you wish to commit changes before scanning table?"))
                {
                    if (!commitChanges())
                    {
                        return;
                    }
                }
            }
            setEditable(false);

            try
            {
                this._scanConditionsControl.CommitEdit();
                resetColumns();
                var columnDefinitions = this._controller.Execute();
                addCustomAttributeColumns(columnDefinitions);

                setEditable(true);
            }
            catch (Exception e)
            {
                this._ctlAddAttributeBtn.IsEnabled = false;
                setEditable(false);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error executing query: " + e.Message);
            }   
        }

        void setEditable(bool isEditable)
        {
            this._ctlDataGrid.CanUserAddRows = isEditable && !this._controller.Model.HasBinaryKeys;
            this._ctlDataGrid.CanUserDeleteRows = isEditable;
            this._ctlDataGrid.IsReadOnly = !isEditable;
            this._ctlAddAttributeBtn.IsEnabled = isEditable;

            if (!isEditable)
            {
                this._ctlCommitChangesBtn.IsEnabled = false;
            }
        }

        void resetColumns()
        {
            for (int i = this._ctlDataGrid.Columns.Count - 1; i >= 0; i--)
            {
                if (!(this._ctlDataGrid.Columns[i] is AttributeColumn))
                {
                    break;
                }
                this._ctlDataGrid.Columns.RemoveAt(i);
            }
        }

        void addCustomAttributeColumns(IEnumerable<DynamoDBColumnDefinition> definitions)
        {
            var existingColumns = new HashSet<string>();
            foreach (BaseDynamoDBColumn column in this._ctlDataGrid.Columns)
            {
                existingColumns.Add(column.Definition.AttributeName);
            }

            var columnsToAdd = new List<DynamoDBColumnDefinition>();
            foreach (var def in definitions)
            {
                if (!existingColumns.Contains(def.AttributeName))
                {
                    columnsToAdd.Add(def);
                }
            }

            foreach (var def in columnsToAdd.OrderBy(x => x.AttributeName))
            {
                var column = new AttributeColumn(this._ctlDataGrid, def);
                column.OnCommitCellEdit += column_OnCommitCellEdit;
                this._ctlDataGrid.Columns.Add(column);
            }

            addAttributesToConditionCache();
        }

        void addAttributesToConditionCache()
        {

            try
            {
                var valuesToWrite = new Dictionary<string, DynamoDBColumnDefinition>();
                var existingValues = DynamoDBColumnDefinition.ReadCachedDefinitions(this._controller.Model.SettingsKey);
                foreach (var value in existingValues)
                    valuesToWrite.Add(value.AttributeName, value);

                
                foreach(var column in this._ctlDataGrid.Columns)
                {
                    var attColumn = column as AttributeColumn;
                    if(attColumn == null)
                        continue;

                    valuesToWrite[attColumn.Definition.AttributeName] = attColumn.Definition;
                }

                DynamoDBColumnDefinition.WriteCachedDefinitions(this._controller.Model.SettingsKey, valuesToWrite.Values);
            }
            catch (Exception e)
            {
                LOGGER.Info("Error persisting attributes to cache", e);
            }
        }

        void onLoadingDataGridRow(object sender, DataGridRowEventArgs e)
        {
            if (!this._hashColumn.IsNew(e.Row.DataContext))
            {
                e.Row.Header = (e.Row.GetIndex() + 1).ToString();
            }
        }
        
        private void onFetchSinglePageClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var definitions = this._controller.FetchFromLastSearch(1);
                addCustomAttributeColumns(definitions);
            }
            catch (Exception e)
            {
                this._ctlAddAttributeBtn.IsEnabled = false;
                setEditable(false);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error fetching more rows: " + e.Message);
            }
        }

        private void onFetchFewPagesClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var definitions = this._controller.FetchFromLastSearch(FETCH_FEW_PAGES_SIZE);
                addCustomAttributeColumns(definitions);
            }
            catch (Exception e)
            {
                this._ctlAddAttributeBtn.IsEnabled = false;
                setEditable(false);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error fetching more rows: " + e.Message);
            }
        }

        private void onExportClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.Title = "Export to CSV";
                dlg.Filter = "CSV (Comma Delimited)|*.csv";
                if (!dlg.ShowDialog().GetValueOrDefault())
                {
                    return;
                }

                var attributeNames = new List<string>();
                foreach (BaseDynamoDBColumn column in this._ctlDataGrid.Columns)
                {
                    attributeNames.Add(column.Definition.AttributeName);
                }

                this._controller.ExportResults(dlg.FileName, attributeNames);
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error exporting results into a csv file: " + e.Message);
            }
        }
    }
}
