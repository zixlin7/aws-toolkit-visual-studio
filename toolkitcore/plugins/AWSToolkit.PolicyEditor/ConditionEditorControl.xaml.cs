using System.Collections.Generic;
using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.PolicyEditor.Model;

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
