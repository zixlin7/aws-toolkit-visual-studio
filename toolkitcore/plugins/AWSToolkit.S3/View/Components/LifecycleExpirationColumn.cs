using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

using Amazon.AWSToolkit.S3.Model;
using Amazon.S3.Model;
using Amazon.AWSToolkit.Util;
using Amazon.AWSToolkit.CommonUI;

using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;


namespace Amazon.AWSToolkit.S3.View.Components
{
    public class LifecycleExpirationColumn : LifecycleColumn
    {
        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            Func<LifecycleRuleModel, string> field = rule =>
                {
                    return rule.FormattedExpiration;
                };

            return base.GenerateElement(cell, dataItem, field);
        }

        protected override string GetInfoText() 
        { 
            return "Setting an expiration will cause objects to be deleted once the expiration has been passed";
        }

        protected override string GetFeatureName()
        {
            return "expiration";
        }

        protected override void SetupEditPanelControlValues()
        {
            if (this._rule.ExpirationDate != null)
            {
                this._useDates.IsChecked = true;
                this._calendar.SelectedDate = this._rule.ExpirationDate;
            }
            else if (this._rule.ExpirationDays != null)
            {
                this._useDays.IsChecked = true;
                this._days.Text = this._rule.ExpirationDays.ToString();
            }
            else
            {
                this._useNoExp.IsChecked = true;
            }

            this._cellValueTextBox.Text = this._rule.FormattedExpiration;
        }

        protected override bool CommitCellEdit(FrameworkElement editingElement)
        {
            if (this._useNoExp.IsChecked.GetValueOrDefault())
                this._rule.SetExpiration(null, null);
            else if (this._useDays.IsChecked.GetValueOrDefault())
            {
                int parse;
                if (int.TryParse(this._days.Text, out parse))
                {
                    this._rule.SetExpiration(parse, null);
                }
                else
                {
                    return false;
                }
            }
            else 
            {
                DateTime parse = this._calendar.SelectedDate.GetValueOrDefault();
                this._rule.SetExpiration(null, parse);
            }

            return true;
        }
    }
}
