using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.DynamoDB.Model;
using Amazon.DynamoDBv2.DocumentModel;

namespace Amazon.AWSToolkit.DynamoDB.View.Columns
{
    public class HashAndRangeKeyColumn : BaseDynamoDBColumn
    {
        KeySchemaExtendedElement _schemaElement;

        string _currentEditedValue;
        TextBox _currentEditedControl;

        public HashAndRangeKeyColumn(KeySchemaExtendedElement schemaElement)
            : base(new DynamoDBColumnDefinition(schemaElement.AttributeName, schemaElement.AttributeType))
        {
            this._schemaElement = schemaElement;
            Header = this._schemaElement.AttributeName;
        }

        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            var document = dataItem as Document;
            if (document == null)
                return null;

            var tb = new TextBlock();
            tb.Margin = TEXT_MARGIN;

            if (document.Contains(this.Definition.AttributeName))
            {
                Primitive primitive = document[this.Definition.AttributeName].AsPrimitive();
                if (primitive.Type == DynamoDBEntryType.Binary)
                {
                    tb.Text = string.Format("Binary item: {0} bytes", primitive.AsByteArray().Length);
                    tb.FontStyle = FontStyles.Italic;
                }
                else
                {
                    tb.Text = primitive.AsString();
                }
            }

            var brush = GetForegroundBrush(dataItem);
            if (brush != null)
                tb.Foreground = brush;

            return tb;
        }

        protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        {
            this._currentCellBeingEdited = cell;
            this._currentEditedControl = new TextBox();

            var document = dataItem as Document;
            if (document != null && document.Contains(this.Definition.AttributeName))
            {
                this._currentEditedControl.Text = document[this.Definition.AttributeName].AsString();
                this._currentEditedValue = this._currentEditedControl.Text;
            }

            if ((!IsSecondaryKey() && !IsNew(dataItem)) || IsKeyBinary)
                this._currentEditedControl.IsReadOnly = true;
            else
                this._currentEditedControl.IsReadOnly = false;            

            return this._currentEditedControl;
        }

        public bool IsNew(object dataItem)
        {
            var document = dataItem as Document;
            if (document == null)
                return true;
            if (!document.Contains(this.Definition.AttributeName))
                return true;
            if (document.IsAttributeChanged(this.Definition.AttributeName))
                return true;

            return false;
        }

        public bool IsSecondaryKey()
        {
            return !_schemaElement.IsPrimaryKeyElement;
        }

        private bool IsKeyBinary => this._schemaElement.AttributeType == DynamoDBConstants.TYPE_BINARY;

        protected override bool CommitCellEdit(FrameworkElement editingElement)
        {
            if (this._currentEditedControl == null || string.IsNullOrEmpty(this._currentEditedControl.Text) || this._currentEditedValue == this._currentEditedControl.Text)
                return true;

            Document document = this._currentCellBeingEdited.DataContext as Document;
            var value = new Primitive(this._currentEditedControl.Text, this._schemaElement.AttributeType == DynamoDBConstants.TYPE_NUMERIC);
            document[this.Definition.AttributeName] = value;
            RaiseOnCommitCellEdit();
            return true;
        }
    }
}
