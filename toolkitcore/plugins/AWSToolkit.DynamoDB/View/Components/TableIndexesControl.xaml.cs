using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.DynamoDB.Controller;
using Amazon.AWSToolkit.DynamoDB.Model;
using Amazon.AWSToolkit.DynamoDB.View.Columns;
using Amazon.AWSToolkit.Util;
using Amazon.DynamoDBv2;

namespace Amazon.AWSToolkit.DynamoDB.View.Components
{
    /// <summary>
    /// Interaction logic for TableIndexesControl.xaml
    /// </summary>
    public partial class TableIndexesControl
    {
        public enum EditingMode { GlobalNew, GlobalModified, LocalNew, LocalModified };

        DataGrid _valuesGrid = null;
        Button _btnAddValue = null, _btnDeleteValue = null;
        ObservableCollection<StringWrapper> _nonKeyList;

        HashSet<SecondaryIndex> _existingIndexes = new HashSet<SecondaryIndex>();

        const int INDEX_NAME_INDEX = 0;
        const int HASH_KEY_INDEX = 1;
        const int RANGE_KEY_INDEX = 2;
        const int PROJECTED_ATTRIBUTES_INDEX = 3;
        const int READ_CAPACITY_INDEX = 4;
        const int WRITE_CAPACITY_INDEX = 5;
        const int STATUS_INDEX = 6;

        public event RoutedEventHandler IndexChanged;

        public TableIndexesControl()
        {
            InitializeComponent();
            this.DataContextChanged += TableIndexesControl_DataContextChanged;
        }

        EditingMode _mode;
        public EditingMode Mode
        {
            get { return this._mode; }
            set
            {
                this._mode = value;
                if (this._mode == EditingMode.LocalNew || this._mode == EditingMode.LocalModified)
                {
                    this._ctlSecondaryIndexesGrid.Columns[HASH_KEY_INDEX].Visibility = System.Windows.Visibility.Hidden;
                    this._ctlSecondaryIndexesGrid.Columns[READ_CAPACITY_INDEX].Visibility = System.Windows.Visibility.Hidden;
                    this._ctlSecondaryIndexesGrid.Columns[WRITE_CAPACITY_INDEX].Visibility = System.Windows.Visibility.Hidden;
                    this._ctlSecondaryIndexesGrid.Columns[STATUS_INDEX].Visibility = System.Windows.Visibility.Hidden;

                    if (this._mode == EditingMode.LocalModified)
                    {
                        this._ctlBtnPanel.Visibility = Visibility.Collapsed;
                    }

                    this.MaxIndex = DynamoDBConstants.MAX_LSI_PER_TABLE;
                }
                else
                {
                    if (this._mode == EditingMode.GlobalNew)
                    {
                        this._ctlSecondaryIndexesGrid.Columns[STATUS_INDEX].Visibility = System.Windows.Visibility.Hidden;
                    }
                    this.MaxIndex = DynamoDBConstants.MAX_GSI_PER_TABLE;
                }
            }
        }

        void TableIndexesControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this._ctlSecondaryIndexesGrid.ItemsSource = this.BoundIndexes;

            this._existingIndexes.Clear();
            if (this.BoundIndexes != null)
            {
                foreach (var index in this.BoundIndexes)
                {
                    this._existingIndexes.Add(index);
                }
            }
        }

        private ObservableCollection<SecondaryIndex> BoundIndexes
        {
            get
            {
                return this.DataContext as ObservableCollection<SecondaryIndex>;
            }
        }

        private int MaxIndex
        {
            get;
            set;
        }

        private void _nonKeyAttributesControl_Loaded(object sender, RoutedEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            var button = sender as Button;
            if (dataGrid != null)
            {
                _valuesGrid = (DataGrid)sender;
                var context = dataGrid.DataContext as ProjectAttributeDefinition;
                if (context == null)
                    return;

                _nonKeyList = context.ProjectionColumnList;
                _valuesGrid.ItemsSource = _nonKeyList;
            }
            if (button != null)
            {
                if (button.Name.Equals("_btnAddValueButton", StringComparison.InvariantCulture))
                {
                    _btnAddValue = button;
                }
                else if (button.Name.Equals("_btnRemoveValueButton", StringComparison.InvariantCulture))
                {
                    _btnDeleteValue = button;
                }
            }
        }

        private void OnRemoveSecondaryIndex(object sender, RoutedEventArgs e)
        {
            if (this._ctlSecondaryIndexesGrid.SelectedIndex == -1)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Select a secondary index to delete.");
            }
            else
            {
                // If the service is already processing a prior change, further updates are not permitted.
                // If the index being actioned is a local edit, not submitted yet (== no status) then we can
                // delete it. Note that the user cannot add a new index while a previous index is being processed.
                var pendingIndexAction = IsCreateOrDeleteInAction();
                var selectedIndex = (SecondaryIndex)this._ctlSecondaryIndexesGrid.SelectedItem;

                if ((this.Mode == EditingMode.GlobalModified || this.Mode == EditingMode.LocalModified) && pendingIndexAction != null)
                {
                    var updateAllowed = true;

                    // is service processing an update?
                    if (!string.IsNullOrEmpty(pendingIndexAction.IndexStatus))
                        updateAllowed = false;

                    // user selected existing index on service but we have new index not yet committed
                    if (selectedIndex != pendingIndexAction)
                        updateAllowed = false;
 
                    if (!updateAllowed)
                    {
                        ToolkitFactory.Instance.ShellProvider.ShowError("Index Action in Process", "Only one index can be created or deleted at a time.");
                        return;
                    }
                }

                this.BoundIndexes.Remove(selectedIndex);

                if (this.BoundIndexes.Count == 0)
                {
                    _btnDeleteGlobalSecondaryIndex.IsEnabled = false;
                }
            }

            if (IndexChanged != null) IndexChanged(this, new RoutedEventArgs());
        }

        private void OnAddSecondaryIndex(object sender, RoutedEventArgs e)
        {
            // if the service is still processing an index update, or we have a local change pending, addition is disallowed
            var pendingIndexAction = IsCreateOrDeleteInAction();
            if ((this.Mode == EditingMode.GlobalModified || this.Mode == EditingMode.LocalModified) && pendingIndexAction != null)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Index Action in Process", "Only one index can be modified at a time.");
                return;
            }

            var secondaryIndex = new SecondaryIndex();

            this.BoundIndexes.Add(secondaryIndex);
            this._ctlSecondaryIndexesGrid.SelectedItem = secondaryIndex;
            this._btnDeleteGlobalSecondaryIndex.IsEnabled = true;

            if(this.BoundIndexes.Count == this.MaxIndex)
            {
                _btnAddGlobalSecondaryIndex.IsEnabled = false;
            }

            if (IndexChanged != null) IndexChanged(this, new RoutedEventArgs());
        }

        private void _btnAddValueButton_Click(object sender, RoutedEventArgs e)
        {
            var newValue = new StringWrapper();
            _nonKeyList.Add(newValue);
            _btnDeleteValue.IsEnabled = true;
            if (_valuesGrid != null)
            {
                _valuesGrid.SelectedItem = newValue;
                DataGridHelper.PutCellInEditMode(_valuesGrid, _valuesGrid.Items.Count - 1, 0);
            }
        }

        private void _btnRemoveValueButton_Click(object sender, RoutedEventArgs e)
        {
            if (_valuesGrid.SelectedItem != null)
            {
                _nonKeyList.Remove((StringWrapper)_valuesGrid.SelectedItem);

                if (_nonKeyList.Count == 0)
                {
                    _btnDeleteValue.IsEnabled = false;
                }
            }
        }

        private void onCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (IndexChanged != null) IndexChanged(this, new RoutedEventArgs());
        }

        private void _btnProjectedAttributesDialogOk_Click(object sender, RoutedEventArgs e)
        {
            this._ctlSecondaryIndexesGrid.CommitEdit();
        }

        private void _btnKeyDefinitionDialogOk_Click(object sender, RoutedEventArgs e)
        {
            this._ctlSecondaryIndexesGrid.CommitEdit();
        }

        private void PART_PopupKeyDefinition_Loaded(object sender, RoutedEventArgs e)
        {
            Popup popup = sender as Popup;
            if (popup == null)
                return;

            Border border = popup.Child as Border;
            if (border == null)
                return;

            Grid grid = border.Child as Grid;
            if (grid == null)
                return;

            foreach (var child in grid.Children)
            {
                if (child is TextBox)
                {
                    // Setting focus is being weird probably because it is in a popup.
                    System.Threading.ThreadPool.QueueUserWorkItem((WaitCallback)(x =>
                    {
                        System.Threading.Thread.Sleep(100);

                        Dispatcher.BeginInvoke((Action)delegate
                        {
                            ((TextBox)child).Focus();
                        });
                    }));

                    break;
                }
            }
        }

        private void _ctlSecondaryIndexesGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (this._mode == EditingMode.GlobalModified)
            {
                var secondaryIndex = e.Row.DataContext as SecondaryIndex;
                if (this._existingIndexes.Contains(secondaryIndex) &&  
                    e.Column.DisplayIndex != READ_CAPACITY_INDEX && 
                    e.Column.DisplayIndex != WRITE_CAPACITY_INDEX)
                {
                    e.Cancel = true;
                }
            }
        }

        private SecondaryIndex IsCreateOrDeleteInAction()
        {
            foreach (var index in this.BoundIndexes)
            {
                if (index.IndexStatus == IndexStatus.DELETING.Value ||
                    index.IndexStatus == IndexStatus.CREATING.Value ||
                    string.IsNullOrEmpty(index.IndexStatus))
                    return index;
            }

            if (this._existingIndexes != null)
            {
                foreach (var index in this._existingIndexes)
                {
                    if (!this.BoundIndexes.Any(x => x.Name == index.Name))
                        return index;
                }
            }

            return null;
        }
    }
}
