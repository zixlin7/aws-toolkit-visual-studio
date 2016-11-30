using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Amazon.AWSToolkit.SimpleDB.View
{
    public class SimpleDBItemNameGridColumn : DataGridTextColumn
    {
        protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        {

            bool isReadOnly;
            DataRowView row = cell.DataContext as DataRowView;
            if (row == null)
            {
                isReadOnly = true;
            }
            else
            {
                isReadOnly = row.Row.RowState != DataRowState.Added && row.Row.RowState != DataRowState.Detached;
            }

            FrameworkElement element;
            if (isReadOnly)
                element = GenerateElement(cell, dataItem);
            else
                element = base.GenerateEditingElement(cell, dataItem);


            return element;
        }
    }
}
