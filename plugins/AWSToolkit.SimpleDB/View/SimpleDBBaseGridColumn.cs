using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Amazon.AWSToolkit.SimpleDB.Controller;
using Amazon.AWSToolkit.SimpleDB.Model;

namespace Amazon.AWSToolkit.SimpleDB.View
{
    public abstract class SimpleDBBaseGridColumn : DataGridColumn
    {
        public event EventHandler<RoutedEventArgs> OnCommitCellEdit;

        protected DataGridCell _currentCellBeingEdited;

        protected Brush GetForegroundBrush(object dataItem)
        {
            DataRowView row = dataItem as DataRowView;
            if (row == null)
            {
                return this.DataGridOwner.FindResource("awsGridForegroundBrushKey") as SolidColorBrush; 
            }
            if (row.Row.RowState == DataRowState.Added)
            {
                return this.DataGridOwner.FindResource("awsGridAttributeChangedForegroundBrushKey") as SolidColorBrush; 
            }

            if (QueryBrowserModel.HasChanged(row, this.Header.ToString()))
            {
                return this.DataGridOwner.FindResource("awsGridAttributeChangedForegroundBrushKey") as SolidColorBrush;
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
