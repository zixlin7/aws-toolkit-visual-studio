using System;
using System.Windows;
using System.Windows.Media;
using Amazon.DynamoDBv2.DocumentModel;
using System.Windows.Controls;

namespace Amazon.AWSToolkit.DynamoDB.View.Columns
{
    public abstract class BaseDynamoDBColumn : DataGridTextColumn
    {
        public event EventHandler<RoutedEventArgs> OnCommitCellEdit;

        protected static readonly Thickness TEXT_MARGIN = new Thickness(5, 0, 5, 0);

        protected DynamoDBColumnDefinition _definition;
        protected DataGridCell _currentCellBeingEdited;

        public BaseDynamoDBColumn(DynamoDBColumnDefinition definition)
        {
            this._definition = definition;
        }

        public DynamoDBColumnDefinition Definition
        {
            get { return this._definition; }
        }

        protected Brush GetForegroundBrush(object dataItem)
        {
            Document document = dataItem as Document;
            if (document == null)
            {
                return this.DataGridOwner.FindResource("awsGridForegroundBrushKey") as SolidColorBrush; //new SolidColorBrush(Colors.Black);
            }

            if (document.IsAttributeChanged(this._definition.AttributeName))
            {
                return this.DataGridOwner.FindResource("awsGridAttributeChangedForegroundBrushKey") as SolidColorBrush; //new SolidColorBrush(Colors.Red);
            }
            else
            {
                return null;
            }
        }

        protected void RaiseOnCommitCellEdit()
        {
            if (this.OnCommitCellEdit != null)
            {
                this.OnCommitCellEdit(this._currentCellBeingEdited, new RoutedEventArgs());
            }
        }
    }
}
