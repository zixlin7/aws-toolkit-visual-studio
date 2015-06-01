using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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


namespace Amazon.AWSToolkit.DynamoDB.View.Components
{
    /// <summary>
    /// Interaction logic for ScanConditionsControl.xaml
    /// </summary>
    public partial class ScanConditionsControl
    {
        TableBrowserController _controller;

        // transient control bind references, active only whilst value dropdown open
        ComboBox _valueSelector;
        Popup _popupPanel;
        DataGrid _valuesGrid;
        Button _addValueToList;
        Button _removeValueFromList;
        Button _okButton;
        Button _cancelButton;
        bool _cancellingValuesEdit;
        ObservableCollection<MutableString> _conditionValues;

        public ScanConditionsControl()
        {
            InitializeComponent();
        }

        public void Initialize(TableBrowserController controller)
        {
            this._controller = controller;
        }

        public void CommitEdit()
        {
            if (_valuesGrid != null)
                _valuesGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        }

        private void OnAddScanCondition(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.Model.ScanConditions.Add(new ScanCondition());
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error adding conditions: " + e.Message);
            }
        }

        private void OnRemoveScanCondition(object sender, RoutedEventArgs evnt)
        {
            try
            {
                List<ScanCondition> itemsToBeRemoved = new List<ScanCondition>();
                foreach (ScanCondition value in this._ctlConditionList.SelectedItems)
                {
                    itemsToBeRemoved.Add(value);
                }

                foreach (var value in itemsToBeRemoved)
                {
                    this._controller.Model.ScanConditions.Remove(value);
                }
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error removing conditions: " + e.Message);
            }
        }

        private void AttributeSelector_DropDownOpened(object sender, EventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            List<string> attribs = new List<string>();
            if (this._controller.Model.TableDescription != null && _controller.Model.TableDescription.KeySchema.Count != 0)
            {
                var model = this._controller.Model;

                attribs.Add(model.HashKeyElement.AttributeName);

                if (model.RangeKeyElement!=null)
                {
                    attribs.Add(model.RangeKeyElement.AttributeName);

                    foreach (var secondaryIndex in model.SecondaryIndexes)
                    {
                        attribs.Add(secondaryIndex.AttributeName);
                    }
                }
            }

            var cachedDefinitions = DynamoDBColumnDefinition.ReadCachedDefinitions(this._controller.Model.SettingsKey);
            foreach (var definition in cachedDefinitions.OrderBy(x => x.AttributeName))
            {
                attribs.Add(definition.AttributeName);
            }

            cb.ItemsSource = attribs;
            ScanCondition sc = cb.DataContext as ScanCondition;
            if (sc != null && !string.IsNullOrEmpty(sc.AttributeName))
                cb.SelectedItem = sc.AttributeName;
        }

        void RemoveConditionButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // New rows aren't immediately selected on addition to the grid (unless the user has
            // clicked on the fields). Makes sure all new rows are immediately removable.
            var lbi = UIUtils.FindVisualParent<ListBoxItem>(sender as DependencyObject);
            if (lbi != null)
                lbi.IsSelected = true;
        }

        // this covers both the user typing and selection from the popup
        void AttributeSelector_TextChanged(object sender, TextChangedEventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            ScanAttributeUpdated(cb.DataContext as ScanCondition, UIUtils.FindVisualParent<ListBoxItem>(cb));
        }

        void DataTypeSelector_DropDownClosed(object sender, EventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            ListBoxItem lbi = UIUtils.FindVisualParent<ListBoxItem>(cb);
            ContentPresenter lbiContentPresenter = UIUtils.FindVisualChild<ContentPresenter>(lbi);
            DataTemplate lbiDataTemplate = lbiContentPresenter.ContentTemplate;

            ScanCondition sc = cb.DataContext as ScanCondition;
            SetupOperatorField(sc, lbiDataTemplate.FindName("_operatorSelector", lbiContentPresenter) as ComboBox);
        }

        void ScanAttributeUpdated(ScanCondition scanCondition, ListBoxItem lbi)
        {
            if (!scanCondition.HasAttributeName)
                return;

            var model = this._controller.Model;
            if (scanCondition.AttributeName == model.HashKeyElement.AttributeName)
                scanCondition.SetDataType(model.HashKeyElement.AttributeType);
            else if (model.RangeKeyElement != null && scanCondition.AttributeName == model.RangeKeyElement.AttributeName)
                scanCondition.SetDataType(model.RangeKeyElement.AttributeType);            
            else
            {
                var index = model.SecondaryIndexes.FirstOrDefault(i => i.AttributeName.Equals(scanCondition.AttributeName, StringComparison.InvariantCulture));
                if (index != null)
                {
                    scanCondition.SetDataType(index.AttributeType);
                }
                
                var cachedDefinitions = DynamoDBColumnDefinition.ReadCachedDefinitions(this._controller.Model.SettingsKey);
                var def = cachedDefinitions.FirstOrDefault(x => string.Equals(x.AttributeName, scanCondition.AttributeName));
                if (def != null)
                    scanCondition.SetDataType(def.DefaultDataType);
            }

            ContentPresenter lbiContentPresenter = UIUtils.FindVisualChild<ContentPresenter>(lbi);
            DataTemplate lbiDataTemplate = lbiContentPresenter.ContentTemplate;

            SetupDataTypeField(scanCondition, lbiDataTemplate.FindName("_dataTypeSelector", lbiContentPresenter) as ComboBox);
            SetupOperatorField(scanCondition, lbiDataTemplate.FindName("_operatorSelector", lbiContentPresenter) as ComboBox);
        }

        void SetupDataTypeField(ScanCondition sc, ComboBox cb)
        {
            IEnumerable<DataTypes> options = null;
            var model = this._controller.Model;
            if (string.Equals(sc.AttributeName, model.HashKeyElement.AttributeName,StringComparison.InvariantCulture) ||
                (model.RangeKeyElement != null && (string.Equals(sc.AttributeName, model.RangeKeyElement.AttributeName,StringComparison.InvariantCulture)))
                || model.SecondaryIndexes.Any(s=>s.AttributeName.Equals(sc.AttributeName,StringComparison.InvariantCulture)))
            {
                options = DataTypes.KeyDataTypes;
            }
            else
            {
                options = DataTypes.AttributeDataTypes;
            }

            cb.ItemsSource = options;
            if (sc.DataType != null && cb.Items.Contains(sc.DataType))
                cb.SelectedItem = sc.DataType;
        }

        void SetupOperatorField(ScanCondition sc, ComboBox cb)
        {
            IEnumerable<ConditionsTypes> options = null;
            switch (sc.DataType.SystemName)
            {
                case DynamoDBConstants.TYPE_STRING:
                    options = ConditionsTypes.StringOperators;
                    break;
                case DynamoDBConstants.TYPE_STRING_SET:
                    options = ConditionsTypes.StringSetOperators;
                    break;
                case DynamoDBConstants.TYPE_NUMERIC:
                    options = ConditionsTypes.NumericOperators;
                    break;
                case DynamoDBConstants.TYPE_NUMERIC_SET:
                    options = ConditionsTypes.NumericSetOperators;
                    break;
            }

            cb.ItemsSource = options;
            if (sc.Operator != null && cb.Items.Contains(sc.Operator))
                cb.SelectedItem = sc.Operator;
        }

        // note that this fires for every char typed, plus the popup closed event
        // we handle elsewhere
        void ValueSelector_TextChanged(object sender, TextChangedEventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            ScanCondition sc = cb.DataContext as ScanCondition;
            if (sc.IsSet)
                return; // data values in this mode are set in popup closed event

            List<string> values = new List<string>();
            values.Add((e.OriginalSource as TextBox).Text);

            sc.Values = values;
        }

        void ValueSelector_DropDownClosed(object sender, EventArgs e)
        {
            if (!_cancellingValuesEdit)
            {
                List<string> values = new List<string>();
                foreach (var val in _conditionValues)
                {
                    values.Add(val.Value);
                }

                ComboBox cb = sender as ComboBox;
                ScanCondition sc = cb.DataContext as ScanCondition;
                sc.Values = values;
                ((cb.Template.FindName("PART_ValueTextBox", cb)) as TextBox).Text = sc.FormattedValues;
            }

            UnbindFromPanelControls();

            _conditionValues = null;
            _valuesGrid = null;
        }

        void ValueSelector_DropDownOpened(object sender, EventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            BindToPanelControls(cb);

            _okButton.Click += new RoutedEventHandler(ValuesPanel_OKButton_Click);
            _cancelButton.Click += new RoutedEventHandler(ValuesPanel_CancelButton_Click);
            _addValueToList.Click += new RoutedEventHandler(ValuesPanel_AddValueToList_Click);
            _removeValueFromList.Click += new RoutedEventHandler(ValuesPanel_RemoveValueFromList_Click);
            _removeValueFromList.IsEnabled = false;

            _valuesGrid.SelectionChanged += new SelectionChangedEventHandler(ValuesPanel_Grid_SelectionChanged);

            _conditionValues = new ObservableCollection<MutableString>();
            ScanCondition sc = cb.DataContext as ScanCondition;
            foreach (var v in sc.Values)
            {
                _conditionValues.Add(new MutableString(v));
            }

            _valuesGrid.ItemsSource = _conditionValues;
        }

        void ValuesPanel_AddValueToList_Click(object sender, RoutedEventArgs e)
        {
            _valuesGrid.CommitEdit(DataGridEditingUnit.Cell, true);

            var ms = new MutableString();
            _conditionValues.Add(ms);
            _valuesGrid.SelectedItem = ms;
        }

        void ValuesPanel_Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _removeValueFromList.IsEnabled = _valuesGrid.SelectedItems.Count > 0;
        }

        void ValuesPanel_RemoveValueFromList_Click(object sender, RoutedEventArgs e)
        {
            _valuesGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            
            List<MutableString> itemsToBeRemoved = new List<MutableString>();
            foreach (MutableString value in _valuesGrid.SelectedItems)
            {
                itemsToBeRemoved.Add(value);
            }

            // removing the values in the event handler seems to cause instability in the
            // UIAutomation layer, so schedule the removal instead
            _valuesGrid.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                                (Action)(() => { RemoveValues(itemsToBeRemoved); }));

        }

        void RemoveValues(List<MutableString> itemsToBeRemoved)
        {
            foreach (var value in itemsToBeRemoved)
            {
                _conditionValues.Remove(value);
            }
        }

        void ValuesPanel_OKButton_Click(object sender, RoutedEventArgs e)
        {
            _valueSelector.IsDropDownOpen = false;
        }

        void ValuesPanel_CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _cancellingValuesEdit = true;
            _valueSelector.IsDropDownOpen = false;
        }

        void BindToPanelControls(ComboBox cb)
        {
            _valueSelector = cb;
            ControlTemplate template = _valueSelector.Template;

            _popupPanel = template.FindName("PART_Popup", cb) as Popup;
            _valuesGrid = template.FindName("PART_MultiValueList", cb) as DataGrid;

            _addValueToList = template.FindName("PART_AddValueButton", cb) as Button;
            _removeValueFromList = template.FindName("PART_RemoveValueButton", cb) as Button;
            _okButton = template.FindName("PART_OKButton", cb) as Button;
            _cancelButton = template.FindName("PART_CancelButton", cb) as Button;

            _cancellingValuesEdit = false;
        }

        void UnbindFromPanelControls()
        {
            _okButton.Click -= new RoutedEventHandler(ValuesPanel_OKButton_Click);
            _okButton = null;

            _cancelButton.Click -= new RoutedEventHandler(ValuesPanel_CancelButton_Click);
            _cancelButton = null;

            _addValueToList.Click -= new RoutedEventHandler(ValuesPanel_AddValueToList_Click);
            _addValueToList = null;

            _removeValueFromList.Click -= new RoutedEventHandler(ValuesPanel_RemoveValueFromList_Click);
            _removeValueFromList = null;

            _valuesGrid.SelectionChanged -= new SelectionChangedEventHandler(ValuesPanel_Grid_SelectionChanged);
            _valuesGrid.ItemsSource = null;
            _valuesGrid = null;

            _popupPanel = null;
        }

    }
}
