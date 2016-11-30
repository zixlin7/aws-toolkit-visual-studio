using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

        public IPermissionContainerModel Model
        {
            get { return this.DataContext as IPermissionContainerModel; }
        }

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
