using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.SimpleDB.Controller;

namespace Amazon.AWSToolkit.SimpleDB.View
{
    /// <summary>
    /// Interaction logic for QueryBrowserControl.xaml
    /// </summary>
    public partial class QueryBrowserControl : BaseAWSControl
    {
        const int FETCH_FEW_PAGES_SIZE = 10;
        bool _turnedOffAutoScroll;
        QueryBrowserController _controller;

        public QueryBrowserControl(QueryBrowserController controller)
        {
            this._controller = controller;
            this.DataContext = this._controller.Model;
            InitializeComponent();

            this._ctlFetchFewPages.ToolTip = string.Format("Fetch an additional {0} pages of data.", FETCH_FEW_PAGES_SIZE);

            ItemNameValidationRule rule = this._ctlDataGrid.RowValidationRules[0] as ItemNameValidationRule;
            rule.OnValidationRule += new EventHandler<ItemNameValidationRule.ValidationEventArgs>(onItemNameValidation);
        }

        public QueryBrowserControl(QueryBrowserController controller, string query)
            : this(controller)
        {
            this._ctlQueryEditor.Text = query;
            onExecuteClick(this, new RoutedEventArgs());
        }


        public override string Title => "Domain: " + this._controller.Model.Domain;

        public override string UniqueId => string.Format("SDB:Domain:{0}", this._controller.Model.Domain);

        private void onPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers == ModifierKeys.Control) && e.Key == Key.R)
            {
                e.Handled = true;
                executeQuery();
            }
        }

        private void onExecuteClick(object sender, RoutedEventArgs evnt)
        {
            executeQuery();
        }

        private string getQueryToExecute()
        {
            var selectedText = this._ctlQueryEditor.SelectedText;
            if (!string.IsNullOrEmpty(selectedText))
                return selectedText;

            return this._ctlQueryEditor.Text;
        }

        private void executeQuery()
        {
            this._ctlDataGrid.CommitEdit(DataGridEditingUnit.Row, true);
            bool useConsistentRead = false;
            // Check if there is uncommited changes before executing a new query
            if (this._ctlCommitChangesBtn.IsEnabled)
            {
                if (ToolkitFactory.Instance.ShellProvider.Confirm("Commit Changes",
                            "There are uncommited changes.  Do you wish to commit changes before running another query?"))
                {
                    if (!commitChanges())
                    {
                        return;
                    }
                    useConsistentRead = true;
                }
            }

            this._ctlCommitChangesBtn.IsEnabled = false;
            try
            {
                this._ctlDataGrid.ItemsSource = null;
                this._controller.ExecuteQuery(this.getQueryToExecute(), useConsistentRead);
                this.buildColumns();
                this._ctlDataGrid.ItemsSource = this._controller.Model.Results.DefaultView;
                this._controller.Model.Results.RowDeleted += new DataRowChangeEventHandler(onRowDeleted);

                if (!this._controller.Model.IsLastQueryCountQuery())
                {
                    this._ctlAddAttributeBtn.IsEnabled = true;
                    this._ctlDataGrid.CanUserAddRows = true;
                    this._ctlDataGrid.IsReadOnly = false;
                }
                else
                {
                    this._ctlAddAttributeBtn.IsEnabled = false;
                    this._ctlDataGrid.CanUserAddRows = false;
                    this._ctlDataGrid.IsReadOnly = true;
                }
            }
            catch (Exception e)
            {
                this._ctlAddAttributeBtn.IsEnabled = false;
                this._ctlDataGrid.CanUserAddRows = false;
                ToolkitFactory.Instance.ShellProvider.ShowError("Error executing query: " + e.Message);
            }
        }

        private void onFetchSinglePageClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.FetchMorePages(1);
            }
            catch (Exception e)
            {
                this._ctlAddAttributeBtn.IsEnabled = false;
                this._ctlDataGrid.CanUserAddRows = false;
                ToolkitFactory.Instance.ShellProvider.ShowError("Error fetching more rows: " + e.Message);
            }
        }

        private void onFetchFewPagesClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.FetchMorePages(FETCH_FEW_PAGES_SIZE);
            }
            catch (Exception e)
            {
                this._ctlAddAttributeBtn.IsEnabled = false;
                this._ctlDataGrid.CanUserAddRows = false;
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

                this._controller.ExportResults(dlg.FileName);
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error exporting results into a csv file: " + e.Message);
            }
        }

        void onRowDeleted(object sender, DataRowChangeEventArgs e)
        {
            this._ctlCommitChangesBtn.IsEnabled = true;
        }

        void buildColumns()
        {
            while (this._ctlDataGrid.Columns.Count > 1)
            {
                this._ctlDataGrid.Columns.RemoveAt(1);
            }

            //SimpleDBItemNameGridColumn nameColumn = new SimpleDBItemNameGridColumn()
            //{
            //    Header = QueryBrowserController.ITEM_NAME_COLUMN_LABEL,
            //};
            //this._ctlDataGrid.Columns.Add(nameColumn);
            //nameColumn.OnCommitCellEdit += new EventHandler<RoutedEventArgs>(onCommitCellEdit);

            DataTable table = this._controller.Model.Results;
            for(int index = QueryBrowserController.START_OF_ATTRIBUTE_COLUMNS; index < table.Columns.Count; index++)
            {
                DataColumn column = table.Columns[index];
                addColumn(column.ColumnName);
            }
        }

        private void addColumn(string columnName)
        {
            SimpleDBMultiValueColumn col = new SimpleDBMultiValueColumn()
            {
                Header = columnName,
                Width = 100
            };
            col.OnCommitCellEdit += new EventHandler<RoutedEventArgs>(onCommitCellEdit);

            this._ctlDataGrid.Columns.Add(col);
        }

        void onCommitCellEdit(object sender, RoutedEventArgs e)
        {
            this._ctlCommitChangesBtn.IsEnabled = true;
        }

        void onCommitChangesClick(object sender, RoutedEventArgs evnt)
        {
            commitChanges();
        }

        bool commitChanges()
        {
            try
            {
                this._ctlDataGrid.CommitEdit(DataGridEditingUnit.Row, true);
                this._controller.CommitChanges();
                this._ctlCommitChangesBtn.IsEnabled = false;
            }
            catch (ApplicationException e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError(e.Message);
                return false;
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error committing changes: " + e.Message);
                return false;
            }

            return true;
        }

        void onAddAttributeClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._ctlDataGrid.CommitEdit(DataGridEditingUnit.Row, true);
                string attributeName = this._controller.AddAttribute();
                if (!string.IsNullOrEmpty(attributeName))
                {                    
                    addColumn(attributeName);
                }
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error adding attribute: " + e.Message);
            }
        }

        void onLoadingDataGridRow(object sender, DataGridRowEventArgs e)
        {
            DataRowView rowView = e.Row.DataContext as DataRowView;
            if (rowView == null || rowView.Row.RowState == DataRowState.Detached || rowView.Row.RowState == DataRowState.Added)
            {
                e.Row.Header = null;
                return;
            }

            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        void onItemNameValidation(object sender, ItemNameValidationRule.ValidationEventArgs args)
        {
            if (!args.IsValid)
            {
                this._ctlCommitChangesBtn.IsEnabled = false;
                this._ctlAddAttributeBtn.IsEnabled = false;
            }
            else
            {
                if(args.IsDirty)
                    this._ctlCommitChangesBtn.IsEnabled = true;

                this._ctlAddAttributeBtn.IsEnabled = true;
            }
        }

        void onLoaded(object sender, RoutedEventArgs e)
        {
            this._ctlQueryEditor.Focus();

            if (!this._turnedOffAutoScroll)
            {
                DataGridHelper.TurnOffAutoScroll(this._ctlDataGrid);
                this._turnedOffAutoScroll = true;
            }
        }

    }
}
