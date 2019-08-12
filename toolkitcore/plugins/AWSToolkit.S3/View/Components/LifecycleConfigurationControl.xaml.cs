using System.Collections.Generic;
using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.S3.Model;
using Amazon.AWSToolkit.S3.Controller;

namespace Amazon.AWSToolkit.S3.View.Components
{
    /// <summary>
    /// Interaction logic for LifecycleConfigurationControl.xaml
    /// </summary>
    public partial class LifecycleConfigurationControl
    {
        BucketPropertiesController _controller;

        public LifecycleConfigurationControl()
        {
            InitializeComponent();
        }

        public void Initialize(BucketPropertiesController controller)
        {
            this._controller = controller;
        }

        public void CommitEdit()
        {
            this._ctlDataGrid.CommitEdit();
        }

        public BucketPropertiesModel Model => this.DataContext as BucketPropertiesModel;

        private void OnAddRule(object sender, RoutedEventArgs args)
        {
            var rule = this._controller.AddLifecycleRule();

            this._ctlDataGrid.SelectedIndex = this.Model.LifecycleRules.Count - 1;
            DataGridHelper.PutCellInEditMode(this._ctlDataGrid, this._ctlDataGrid.SelectedIndex, 0);
        }

        private void OnRemoveRule(object sender, RoutedEventArgs args)
        {
            List<LifecycleRuleModel> itemsToBeRemoved = new List<LifecycleRuleModel>();
            foreach (LifecycleRuleModel entry in this._ctlDataGrid.SelectedItems)
            {
                itemsToBeRemoved.Add(entry);
            }

            foreach (LifecycleRuleModel entry in itemsToBeRemoved)
            {
                this.Model.LifecycleRules.Remove(entry);
            }
        }
    }
}
