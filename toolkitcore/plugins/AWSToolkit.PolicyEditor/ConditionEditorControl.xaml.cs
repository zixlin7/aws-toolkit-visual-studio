using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.PolicyEditor.Model;
using Amazon.AWSToolkit.Util;

using Amazon.Auth.AccessControlPolicy;

namespace Amazon.AWSToolkit.PolicyEditor
{
    /// <summary>
    /// Interaction logic for ConditionEditorControl.xaml
    /// </summary>
    public partial class ConditionEditorControl
    {
        public ConditionEditorControl()
        {
            InitializeComponent();
        }

        public void OnAddCondition(object sender, RoutedEventArgs e)
        {
            StatementModel model = this.DataContext as StatementModel;
            model.AddCondition();
            this._ctlConditions.SelectedIndex = model.Conditions.Count - 1;
            DataGridHelper.PutCellInEditMode(this._ctlConditions, this._ctlConditions.SelectedIndex, 0);
        }

        public void OnRemoveCondition(object sender, RoutedEventArgs e)
        {
            StatementModel model = this.DataContext as StatementModel;

            List<ConditionModel> itemsToBeRemoved = new List<ConditionModel>();
            foreach (ConditionModel value in this._ctlConditions.SelectedItems)
            {
                itemsToBeRemoved.Add(value);
            }

            foreach (ConditionModel value in itemsToBeRemoved)
            {
                model.RemoveCondition(value);
            }
        }
    }
}
