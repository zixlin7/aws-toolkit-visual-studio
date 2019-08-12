using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.SimpleDB.Controller;
using Amazon.AWSToolkit.SimpleDB.Model;

namespace Amazon.AWSToolkit.SimpleDB.View
{
    public class SimpleDBMultiValueColumn : SimpleDBBaseGridColumn
    {
         
        List<string> _currentEditedValues;
        TextBox _currentEditedTextBox;

        protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        {
            this._currentCellBeingEdited = cell;

            this._currentEditedValues = getListValues(cell);            

            this._currentEditedTextBox = new TextBox();
            this._currentEditedTextBox.Text = createDisplayString(cell);
            this._currentEditedTextBox.VerticalAlignment = VerticalAlignment.Stretch;
            this._currentEditedTextBox.VerticalContentAlignment = VerticalAlignment.Stretch;
            this._currentEditedTextBox.IsReadOnly = this._currentEditedValues.Count > 1;

            Button btn = new Button();
            btn.Content = "...";
            btn.Width = 20;
            btn.VerticalAlignment = VerticalAlignment.Center;
            btn.Click += new RoutedEventHandler(onMultiValueEditorClick);

            DockPanel panel = new DockPanel();
            DockPanel.SetDock(btn, Dock.Right);
            panel.Children.Add(btn);

            panel.Children.Add(this._currentEditedTextBox);
            this._currentEditedTextBox.Loaded += new RoutedEventHandler(onEditCellLoaded);


            return panel;
        }

        void onEditCellLoaded(object sender, RoutedEventArgs e)
        {
            this._currentEditedTextBox.Focus();
        }

        void onMultiValueEditorClick(object sender, RoutedEventArgs e)
        {
            EditAttributeModel model;
            if(this._currentEditedValues.Count <= 1 && !string.IsNullOrEmpty(this._currentEditedTextBox.Text))
                model = new EditAttributeModel(this.Header.ToString(), new List<string>(new string[]{this._currentEditedTextBox.Text}));
            else
                model = new EditAttributeModel(this.Header.ToString(), this._currentEditedValues);

            EditAttributeController controller = new EditAttributeController(model);
            if (controller.Execute())
            {
                this._currentEditedValues = controller.Model.GetValues();
                this._currentEditedTextBox.Text = createDisplayString(this._currentEditedValues);
                this._currentEditedTextBox.IsReadOnly = this._currentEditedValues.Count > 1;
            }
        }

        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            string value;
            if (cell.Column.Header.ToString().Equals(QueryBrowserController.ITEM_NAME_COLUMN_LABEL) &&
                !(cell.DataContext is DataRowView))
            {
                value = "<new row>";
            }
            else
            {
                value = createDisplayString(cell);
            }
            TextBlock blk = new TextBlock();
            var brush = GetForegroundBrush(dataItem);
            if (brush != null)
                blk.Foreground = brush;

            blk.Text = value;
            return blk;
        }


        protected override bool CommitCellEdit(FrameworkElement editingElement)
        {
            List<string> values;
            if (this._currentEditedValues.Count <= 1)
            {
                values = new List<string>();
                values.Add(this._currentEditedTextBox.Text);
            }
            else
            {
                values = this._currentEditedValues;
            }

            DataRowView row = this._currentCellBeingEdited.DataContext as DataRowView;
            row[this.Header.ToString()] = values;

            this.RaiseOnCommitCellEdit();
            this._currentCellBeingEdited = null;
            this._currentEditedValues = null;
            this._currentEditedTextBox = null;

            return true;
        }

        protected override void CancelCellEdit(FrameworkElement editingElement, object uneditedValue)
        {
            this._currentCellBeingEdited = null;
            this._currentEditedValues = null;
            this._currentEditedTextBox = null;
        }


        private List<string> getListValues(DataGridCell cell)
        {
            DataRowView row = cell.DataContext as DataRowView;
            if (row == null)
            {
                return new List<string>();
            }

            List<string> values = row[this.Header.ToString()] as List<string>;
            if (values == null)
            {
                return new List<string>();
            }

            return values;
        }

        private string createDisplayString(DataGridCell cell)
        {
            DataRowView row = cell.DataContext as DataRowView;
            if (row == null)
            {
                return null;
            }
            else if (row[this.Header.ToString()] is string)
            {
                return row[this.Header.ToString()] as string;
            }

            List<string> values = null;
            if (row != null)
                values = row[this.Header.ToString()] as List<string>;

            return createDisplayString(values);
        }

        private string createDisplayString(List<string> values)
        {
            if (values == null || values.Count == 0)
                return null;
            if (values.Count == 1)
                return values[0];

            StringBuilder sb = new StringBuilder();
            foreach (string value in values)
            {
                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append(value);
            }
            return string.Format("[{0}]", sb.ToString());
        }
    }
}
