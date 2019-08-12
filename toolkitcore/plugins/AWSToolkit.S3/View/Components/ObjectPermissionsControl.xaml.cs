using System.Collections.Generic;
using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.S3.Model;

namespace Amazon.AWSToolkit.S3.View.Components
{
    /// <summary>
    /// Interaction logic for ObjectPermissionsControl.xaml
    /// </summary>
    public partial class ObjectPermissionsControl
    {
        public ObjectPermissionsControl()
        {
            InitializeComponent();
        }

        public IPermissionContainerModel Model => this.DataContext as IPermissionContainerModel;

        private void OnAddPermission(object sender, RoutedEventArgs args)
        {
            this.Model.PermissionEntries.Add(new Permission());
            this._ctlPermissionDataGrid.SelectedIndex = this.Model.PermissionEntries.Count - 1;

            DataGridHelper.PutCellInEditMode(this._ctlPermissionDataGrid, this._ctlPermissionDataGrid.SelectedIndex, 0);
        }

        private void OnRemovePermission(object sender, RoutedEventArgs args)
        {
            List<Permission> itemsToBeRemoved = new List<Permission>();
            foreach (Permission entry in this._ctlPermissionDataGrid.SelectedItems)
            {
                itemsToBeRemoved.Add(entry);
            }

            foreach (Permission entry in itemsToBeRemoved)
            {
                this.Model.PermissionEntries.Remove(entry);
            }
        }
    }
}
