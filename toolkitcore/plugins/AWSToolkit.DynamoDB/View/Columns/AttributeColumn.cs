using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Amazon.AWSToolkit.Util;
using Amazon.DynamoDBv2.DocumentModel;
using System.Windows.Media;

namespace Amazon.AWSToolkit.DynamoDB.View.Columns
{
    public class AttributeColumn : BaseDynamoDBColumn
    {
        [Flags]
        public enum CellDataFormatType
        {
            Indeterminate = 0,
            IsString = 0x00000001,
            IsNumber = 0x00000002,
            IsBinary = 0x00000004,
            IsBool = 0x00000008,
            IsNull = 0x00000010,
            IsSet = 0x00010000
        }

        DataGrid _parentGrid;

        // overall edit control containing various sub-parts...
        ComboBox _cellEditControl;
        Grid _cellEditPanelContainer;
        Popup _cellEditPanelPopup;
        //...the subparts of interest, bound to when we enter edit mode on a cell
        TextBox _cellValueTextBox;
        StackPanel _multiValueListPanel;
        DataGrid _pendingValueListGrid;
        Button _btnAddToList;
        Button _btnRemoveFromList;
        RadioButton _btnAsBool;
        RadioButton _btnAsNull;
        RadioButton _btnAsString;
        RadioButton _btnAsStringSet;
        RadioButton _btnAsNumeric;
        RadioButton _btnAsNumericSet;
        Button _okButton;
        Button _cancelButton;
        TextBlock _validationBox;

        CellDataFormatType _originalDataFormat;
        List<string> _originalValues;

        CellDataFormatType _pendingDataFormat;
        ObservableCollection<MutableString> _pendingValues;

        public AttributeColumn(DataGrid parentGrid, DynamoDBColumnDefinition definition)
            : base(definition)
        {
            Header = this.Definition.AttributeName;
            this._parentGrid = parentGrid;
        }

        static bool IsSetFormat(CellDataFormatType cellFormat)
        {
            return ((cellFormat & CellDataFormatType.IsSet) == CellDataFormatType.IsSet);
        }

        static bool IsNumericFormat(CellDataFormatType cellFormat)
        {
            return ((cellFormat & CellDataFormatType.IsNumber) == CellDataFormatType.IsNumber);
        }

        static bool IsStringFormat(CellDataFormatType cellFormat)
        {
            return ((cellFormat & CellDataFormatType.IsString) == CellDataFormatType.IsString);
        }

        static bool IsBinaryFormat(CellDataFormatType cellFormat)
        {
            return ((cellFormat & CellDataFormatType.IsBinary) == CellDataFormatType.IsBinary);
        }

        void SetEditPanelLayoutForFormat(CellDataFormatType cellFormat)
        {
            this._pendingDataFormat = cellFormat;
            bool isNowASet = IsSetFormat(this._pendingDataFormat);

            if (_multiValueListPanel != null)
                _multiValueListPanel.Visibility = isNowASet ? Visibility.Visible : Visibility.Collapsed;
        }

        void SetEditPanelButtonsForFormat(CellDataFormatType cellFormat)
        {
            bool isSet = IsSetFormat(cellFormat);
            if (IsNumericFormat(cellFormat))
            {
                if (isSet)
                    _btnAsNumericSet.IsChecked = true;
                else
                    _btnAsNumeric.IsChecked = true;
            }
            else if (cellFormat == CellDataFormatType.IsBool)
            {
                _btnAsBool.IsChecked = true;
            }
            else if (cellFormat == CellDataFormatType.IsNull)
            {
                _btnAsNull.IsChecked = true;
            }
            else
            {
                if (isSet)
                    _btnAsStringSet.IsChecked = true;
                else
                    _btnAsString.IsChecked = true;
            }

            SetConvertToSingleValueEnablement(_pendingValues.Count);
        }

        void SetConvertToSingleValueEnablement(int valuesCount)
        {
            _btnAsString.IsEnabled = _btnAsNumeric.IsEnabled = valuesCount <= 1;
        }

        public bool InEditMode
        {
            get { return this._cellEditControl != null; }
        }

        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            ResetEditableState();
            this._originalDataFormat = this._pendingDataFormat = CellDataFormatType.IsString;

            var document = dataItem as Document;
            if (document == null)
                return null;

            DynamoDBEntry entry;
            if (!document.TryGetValue(this.Definition.AttributeName, out entry))
                return null;

            DetermineCurrentCellDataFormat(entry);

            bool isNumeric = false;
            string value = "";


            if (entry is PrimitiveList)
            {
                PrimitiveList primitiveList = entry as PrimitiveList;
                if (primitiveList.Type == DynamoDBEntryType.Binary)
                {
                    value = string.Format("Binary set: {0} items", primitiveList.Entries.Count);
                }
                else
                {
                    value = createDisplayString(primitiveList.AsListOfString());
                    isNumeric = primitiveList.Type == DynamoDBEntryType.Numeric;
                }
            }
            else if (entry is Primitive)
            {
                Primitive primitive = entry as Primitive;
                if (primitive.Type == DynamoDBEntryType.Binary)
                {
                    value = string.Format("Binary item: {0} bytes", primitive.AsByteArray().Length);
                }
                else
                {
                    value = primitive.AsString();
                    isNumeric = primitive.Type == DynamoDBEntryType.Numeric;
                }
            }
            else if (entry is DynamoDBBool)
            {
                value = entry.AsBoolean().ToString();
            }
            else if (entry is DynamoDBNull)
            {
                value = "NULL";
            }
            else if (entry is Document)
            {
                var subDocument = entry as Document;
                value = string.Format("Map item: {0} keys", subDocument.Count);
            }
            else if (entry is DynamoDBList)
            {
                var list = entry as DynamoDBList;
                value = string.Format("List item: {0} entries", list.Entries.Count);
            }
            else
            {
                value = "Unknown Datatype";
            }

            var tb = new TextBlock();
            tb.Margin = TEXT_MARGIN;
            tb.Text = value;
            if (!IsEditable(entry))
            {
                tb.FontStyle = FontStyles.Italic;
            }

            if (isNumeric)
            {
                tb.TextAlignment = TextAlignment.Right;
            }

            var brush = GetForegroundBrush(dataItem);
            if (brush != null)
                tb.Foreground = brush;

            this._originalDataFormat = this._pendingDataFormat;
            return tb;
        }

        private string createDisplayString(List<string> values)
        {
            string displayValue = null;
            if (values == null || values.Count == 0)
                displayValue = null;
            else if (values.Count == 1)
                displayValue = values[0];
            else
            {
                StringBuilder sb = new StringBuilder();
                foreach (string value in values)
                {
                    if (sb.Length > 0)
                        sb.Append(", ");
                    sb.Append(value);
                }

                displayValue = sb.ToString();
            }

            if((this._originalDataFormat & CellDataFormatType.IsSet) == CellDataFormatType.IsSet)
                return string.Format("[{0}]", displayValue);
            return displayValue;
        }

        private void ResetEditableState()
        {
            this._originalValues = null;
            this._pendingValues = null;
            this._cellEditControl = null;
            this._pendingValueListGrid = null;
        }

        private bool IsEditable(DynamoDBEntry entry)
        {
            if (entry is Primitive)
            {
                return ((Primitive)entry).Type != DynamoDBEntryType.Binary;
            }
            if (entry is PrimitiveList)
            {
                return ((PrimitiveList)entry).Type != DynamoDBEntryType.Binary;
            }
            if (entry is DynamoDBBool)
            {
                return true;
            }
            if (entry is DynamoDBNull)
            {
                return true;
            }

            return false;
        }

        protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        {
            ResetEditableState();
            var document = dataItem as Document;
            if (document == null)
                return null;

            DynamoDBEntry entry;
            if (document.TryGetValue(this.Definition.AttributeName, out entry))
            {
                if (!IsEditable(entry))
                {
                    MessageBox.Show("This data type cannot be edited in the Toolkit");
                    return GenerateElement(cell, dataItem);
                }
            }

            this._currentCellBeingEdited = cell;
            this._cellEditControl = new ComboBox();
            this._cellEditControl.Loaded += new RoutedEventHandler(CurrentEditedControl_Loaded);
            this._cellEditControl.Style = cell.FindResource("scanEditorCellValueEditPanel") as Style;
                
            return this._cellEditControl;
        }

        void CurrentEditedControl_Loaded(object sender, RoutedEventArgs e)
        {
            BindToEditControlSubControls();
            SetupEditPanelControlValues();
        }

        void BindToEditControlSubControls()
        {
            ControlTemplate template = this._cellEditControl.Template;
            _cellValueTextBox = template.FindName("PART_ValueTextBox", this._cellEditControl) as TextBox;
            
            _cellEditPanelContainer = template.FindName("MainGrid", this._cellEditControl) as Grid;
            _cellEditPanelPopup = template.FindName("PART_Popup", this._cellEditControl) as Popup;

            _multiValueListPanel = template.FindName("PART_MultiValueListPanel", this._cellEditControl) as StackPanel;
            _pendingValueListGrid = template.FindName("PART_MultiValueList", this._cellEditControl) as DataGrid;
            _pendingValueListGrid.SelectionChanged += new SelectionChangedEventHandler(PendingValueListGrid_SelectionChanged);

            foreach (DataGridColumn col in _pendingValueListGrid.Columns)
            {
                col.MinWidth = col.ActualWidth;
                col.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            }

            _btnAddToList = template.FindName("PART_AddValueButton", this._cellEditControl) as Button;
            if (_btnAddToList != null)
                _btnAddToList.Click += new RoutedEventHandler(AddToListButton_Click);
            _btnRemoveFromList = template.FindName("PART_RemoveValueButton", this._cellEditControl) as Button;
            if (_btnRemoveFromList != null)
            {
                _btnRemoveFromList.Click += new RoutedEventHandler(RemoveFromListButton_Click);
                _btnRemoveFromList.IsEnabled = false;
            }

            _btnAsBool = template.FindName("PART_AsBoolButton", this._cellEditControl) as RadioButton;
            if (_btnAsBool != null)
                _btnAsBool.Checked += new RoutedEventHandler(btnFormatType_Checked);

            _btnAsNull = template.FindName("PART_AsNullButton", this._cellEditControl) as RadioButton;
            if (_btnAsNull != null)
                _btnAsNull.Checked += new RoutedEventHandler(btnFormatType_Checked);

            _btnAsString = template.FindName("PART_AsStringButton", this._cellEditControl) as RadioButton;
            if (_btnAsString != null)
                _btnAsString.Checked += new RoutedEventHandler(btnFormatType_Checked);

            _btnAsNumeric = template.FindName("PART_AsNumberButton", this._cellEditControl) as RadioButton;
            if (_btnAsNumeric != null)
                _btnAsNumeric.Checked += new RoutedEventHandler(btnFormatType_Checked);

            _btnAsStringSet = template.FindName("PART_AsStringSetButton", this._cellEditControl) as RadioButton;
            if (_btnAsStringSet != null)
                _btnAsStringSet.Checked += new RoutedEventHandler(btnFormatType_Checked);

            _btnAsNumericSet = template.FindName("PART_AsNumberSetButton", this._cellEditControl) as RadioButton;
            if (_btnAsNumericSet != null)
                _btnAsNumericSet.Checked += new RoutedEventHandler(btnFormatType_Checked);

            _okButton = template.FindName("PART_OKButton", this._cellEditControl) as Button;
            if (_okButton != null)
                _okButton.Click += new RoutedEventHandler(OKButton_Click);
            _cancelButton = template.FindName("PART_CancelButton", this._cellEditControl) as Button;
            if (_cancelButton != null)
                _cancelButton.Click += new RoutedEventHandler(CancelButton_Click);

            _validationBox = template.FindName("PART_ValidationBox", this._cellEditControl) as TextBlock;
        }

        void PendingValueListGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _btnRemoveFromList.IsEnabled = _pendingValueListGrid.SelectedItems.Count != 0;
        }

        void ReleaseEditPanelCapture()
        {
        }

        void SetupEditPanelControlValues()
        {
            _originalValues = new List<string>();
            _pendingValues = new ObservableCollection<MutableString>();

            DynamoDBEntry entry;
            Document document = _currentCellBeingEdited.DataContext as Document;
            if (document.TryGetValue(Definition.AttributeName, out entry))
            {
                if (entry is PrimitiveList)
                {
                    foreach (var item in ((PrimitiveList)entry).Entries)
                    {
                        string value = item.AsString();
                        _originalValues.Add(value);
                        _pendingValues.Add(new MutableString(value));
                    }
                }
                else if(entry is Primitive)
                {
                    string value = document[this.Definition.AttributeName].AsString();
                    _originalValues.Add(value);
                    _pendingValues.Add(new MutableString(value));
                }
                else if (entry is DynamoDBBool)
                {
                    string value = document[this.Definition.AttributeName].AsBoolean() ? "True" : "False";
                    _originalValues.Add(value);
                    _pendingValues.Add(new MutableString(value));
                }
                else if (entry is DynamoDBNull)
                {
                    string value = "NULL";
                    _originalValues.Add(value);
                    _pendingValues.Add(new MutableString(value));
                }

                DetermineCurrentCellDataFormat(entry);
            }

            _cellEditControl.ItemsSource = _pendingValues;
            SetEditPanelLayoutForFormat(_originalDataFormat);
            SetEditPanelButtonsForFormat(_originalDataFormat);
            SetCellValueTextBoxContent();
        }

        void btnFormatType_Checked(object sender, RoutedEventArgs e)
        {
            if (_btnAsString != null && _btnAsString.IsChecked == true)
                _pendingDataFormat = CellDataFormatType.IsString;

            if (_btnAsStringSet != null && _btnAsStringSet.IsChecked == true)
                _pendingDataFormat = CellDataFormatType.IsString | CellDataFormatType.IsSet;

            if (_btnAsNumeric != null && _btnAsNumeric.IsChecked == true)
                _pendingDataFormat = CellDataFormatType.IsNumber;

            if (_btnAsNumeric != null && _btnAsNumericSet.IsChecked == true)
                _pendingDataFormat = CellDataFormatType.IsNumber | CellDataFormatType.IsSet;

            if (_btnAsBool != null && _btnAsBool.IsChecked == true)
            {
                _pendingDataFormat = CellDataFormatType.IsBool;
                if (this._cellValueTextBox != null && string.IsNullOrWhiteSpace(this._cellValueTextBox.Text))
                {
                    this._cellValueTextBox.Text = "True";
                }
            }

            if (_btnAsNull != null && _btnAsNull.IsChecked == true)
            {
                _pendingDataFormat = CellDataFormatType.IsNull;
                if (this._cellValueTextBox != null && string.IsNullOrWhiteSpace(this._cellValueTextBox.Text))
                {
                    this._cellValueTextBox.Text = "NULL";
                }
            }

            SetEditPanelLayoutForFormat(_pendingDataFormat);
            SetCellValueTextBoxContent();
        }

        void SetCellValueTextBoxContent()
        {
            if (!IsSetFormat(_pendingDataFormat))
            {
                if (_pendingValues.Count == 1)
                    _cellValueTextBox.Text = _pendingValues[0].Value;
                _cellValueTextBox.IsReadOnly = false;
            }
            else
            {
                _cellValueTextBox.Text = string.Empty;
                _cellValueTextBox.IsReadOnly = true;
            }
        }

        void RemoveFromListButton_Click(object sender, RoutedEventArgs e)
        {
            List<MutableString> itemsToBeRemoved = new List<MutableString>();
            foreach (MutableString value in _pendingValueListGrid.SelectedItems)
            {
                itemsToBeRemoved.Add(value);
            }

            foreach (var value in itemsToBeRemoved)
            {
                _pendingValues.Remove(value);
            }

            SetConvertToSingleValueEnablement(_pendingValues.Count);
        }

        void AddToListButton_Click(object sender, RoutedEventArgs e)
        {
            var ms = new MutableString();
            _pendingValues.Add(ms);
            _pendingValueListGrid.SelectedItem = ms;
            SetConvertToSingleValueEnablement(_pendingValues.Count);
        }

        void OKButton_Click(object sender, RoutedEventArgs e)
        {
            ReleaseEditPanelCapture();
            this._parentGrid.CommitEdit(DataGridEditingUnit.Row, true);
        }

        void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ReleaseEditPanelCapture();
            this._parentGrid.CancelEdit(DataGridEditingUnit.Row);
        }

        void DetermineCurrentCellDataFormat(DynamoDBEntry entry)
        {
            Type type = entry.GetType();
            if (type == typeof(PrimitiveList) || type == typeof(Primitive))
            {
                DynamoDBEntryType entryType = DynamoDBEntryType.Binary;
                bool isSet = false;

                if (entry is PrimitiveList)
                {
                    entryType = ((PrimitiveList)entry).Type;
                    isSet = true;
                }
                else if (entry is Primitive)
                {
                    entryType = ((Primitive)entry).Type;
                    isSet = false;
                }

                switch (entryType)
                {
                    case DynamoDBEntryType.String:
                        this._pendingDataFormat = CellDataFormatType.IsString;
                        break;
                    case DynamoDBEntryType.Numeric:
                        this._pendingDataFormat = CellDataFormatType.IsNumber;
                        break;
                    case DynamoDBEntryType.Binary:
                    default:
                        this._pendingDataFormat = CellDataFormatType.IsBinary;
                        break;
                }

                if (isSet)
                    this._pendingDataFormat |= CellDataFormatType.IsSet;

                this._originalDataFormat = this._pendingDataFormat;
            }
            else if(type == typeof(DynamoDBBool))
            {
                this._originalDataFormat = this._pendingDataFormat = CellDataFormatType.IsBool;
            }
            else if (type == typeof(DynamoDBNull))
            {
                this._originalDataFormat = this._pendingDataFormat = CellDataFormatType.IsNull;
            }
            else
            {
                this._originalDataFormat = this._pendingDataFormat = CellDataFormatType.Indeterminate;
            }
            
        }

        private bool HasDataChanged(IList<string> newDataToCommit)
        {
            if (this._originalDataFormat != this._pendingDataFormat)
                return true;

            if (this._originalValues.Count != newDataToCommit.Count)
                return true;

            for (int i = 0; i < this._originalValues.Count; i++)
            {
                var org = this._originalValues[i];
                var mut = newDataToCommit[i];

                if (!string.Equals(org, mut))
                    return true;
            }

            return false;
        }

        private IList<string> NewDataToCommit()
        {
            var trimmedValues = new List<string>();

            if (IsSetFormat(this._pendingDataFormat))
            {
                if (this._pendingValues != null)
                {
                    foreach (var item in this._pendingValues)
                    {
                        if (item.Value != null && item.Value.Trim().Length > 0)
                            trimmedValues.Add(item.Value.Trim());
                    }
                }
            }
            else
            {
                if (this._cellValueTextBox != null && this._cellValueTextBox.Text != null && this._cellValueTextBox.Text.Trim().Length > 0)
                {
                    trimmedValues.Add(this._cellValueTextBox.Text.Trim());
                }
            }

            return trimmedValues;
        }

        protected override bool CommitCellEdit(FrameworkElement editingElement)
        {
            var trimmedValues = NewDataToCommit();

            if (this._cellEditControl == null || !HasDataChanged(trimmedValues))
                return true;

            Document document = this._currentCellBeingEdited.DataContext as Document;

            if (trimmedValues.Count == 0)
            {
                document[this.Definition.AttributeName] = null;
            }
            else
            {
                bool isBinary = IsBinaryFormat(_pendingDataFormat);
                if (isBinary || _pendingDataFormat == CellDataFormatType.Indeterminate)
                    return false;

                bool isNumeric = IsNumericFormat(_pendingDataFormat);
                DynamoDBEntry entry = null;
                if (IsSetFormat(_pendingDataFormat))
                {
                    entry = new PrimitiveList(isNumeric ? DynamoDBEntryType.Numeric : DynamoDBEntryType.String);

                    foreach (var token in trimmedValues)
                    {
                        if (!IsValidForDataFormat(token))
                            return false;

                        ((PrimitiveList)entry).Add(token);
                    }
                }
                else
                {
                    if (!IsValidForDataFormat(trimmedValues[0]))
                        return false;

                    if (_pendingDataFormat == CellDataFormatType.IsBool)
                        entry = new DynamoDBBool(Boolean.Parse(trimmedValues[0]));
                    else if (_pendingDataFormat == CellDataFormatType.IsNull)
                        entry = new DynamoDBNull();
                    else
                        entry = new Primitive(trimmedValues[0], isNumeric);
                }

                document[this.Definition.AttributeName] = entry;
            }

            
            RaiseOnCommitCellEdit();
            return true;
        }

        private bool IsValidForDataFormat(string text)
        {
            bool resultBool;
            double dd;
            if (IsNumericFormat(_pendingDataFormat) && !double.TryParse(text, out dd))
            {
                //SetEditPanelControlsEnablement(false);

                _validationBox.Text = string.Format("\"{0}\" can not be parsed as a number", text);
                _validationBox.Visibility = Visibility.Visible;

                _validationBox.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                                    (Action)(() => { _cellEditControl.Focus(); }));

                //ToolkitFactory.Instance.ShellProvider.ShowMessage("Invalid Format", string.Format("\"{0}\" can not be parsed as a number", text));
                //SetEditPanelControlsEnablement(true);

                return false;
            }
            else if (_pendingDataFormat == CellDataFormatType.IsBool && !Boolean.TryParse(text, out resultBool))
            {
                _validationBox.Text = string.Format("\"{0}\" can not be parsed as a boolean", text);
                _validationBox.Visibility = Visibility.Visible;

                _validationBox.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                                    (Action)(() => { _cellEditControl.Focus(); }));
                return false;
            }
            else if (_pendingDataFormat == CellDataFormatType.IsNull && !string.Equals(text, "null", StringComparison.OrdinalIgnoreCase))
            {
                _validationBox.Text = string.Format("Value must be set to the string \"NULL\" when using the null type.", text);
                _validationBox.Visibility = Visibility.Visible;

                _validationBox.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                                    (Action)(() => { _cellEditControl.Focus(); }));
                return false;
            }

            return true;
        }

        void SetEditPanelControlsEnablement(bool enabled)
        {
            _pendingValueListGrid.IsEnabled = enabled;
            _btnAddToList.IsEnabled = enabled;
            _btnRemoveFromList.IsEnabled = enabled;
            _btnAsString.IsEnabled = enabled;
            _btnAsStringSet.IsEnabled = enabled;
            _btnAsNumeric.IsEnabled = enabled;
            _btnAsNumericSet.IsEnabled = enabled;
            _okButton.IsEnabled = enabled;
            _cancelButton.IsEnabled = enabled;
        }
    }
}
