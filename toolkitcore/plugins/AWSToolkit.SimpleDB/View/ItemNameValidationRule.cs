using System;
using System.Data;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

using Amazon.AWSToolkit.SimpleDB.Controller;

namespace Amazon.AWSToolkit.SimpleDB.View
{
    public class ItemNameValidationRule : ValidationRule
    {
        public event EventHandler<ValidationEventArgs> OnValidationRule;

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            bool isDirty = false;
            ValidationResult result;
            BindingGroup group = value as BindingGroup;
            if (group.Items.Count == 0)
            {
                result = new ValidationResult(true, null);
            }
            else
            {
                DataRowView row = group.Items[0] as DataRowView;
                if (row == null || (row.Row.RowState == DataRowState.Detached && !row.IsEdit))
                {
                    result = new ValidationResult(true, null);
                }
                else
                {

                    string name = row[QueryBrowserController.ITEM_NAME_COLUMN] as string;
                    if (string.IsNullOrEmpty(name))
                    {
                        bool anyData = false;
                        for (int i = 1; i < row.Row.Table.Columns.Count; i++)
                        {
                            if (!row.Row.IsNull(i))
                            {
                                anyData = true;
                                break;
                            }
                        }
                        result = new ValidationResult(!anyData, "Name is a required column!");
                    }
                    else
                    {
                        result = new ValidationResult(true, null);
                    }

                    if (row.Row.RowState == DataRowState.Modified || row.Row.RowState == DataRowState.Added || row.Row.RowState == DataRowState.Deleted)
                        isDirty = true;
                }
            }

            if (OnValidationRule != null)
            {
                var args = new ValidationEventArgs() { IsValid = result.IsValid, IsDirty=isDirty };
                OnValidationRule(this, args);
            }
            return result;
        }

        public class ValidationEventArgs : EventArgs
        {
            public bool IsValid { get; set; }
            public bool IsDirty { get; set; }
        }
    }
}
