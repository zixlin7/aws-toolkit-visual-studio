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

using Amazon.AWSToolkit.S3.Model;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.S3.View.Components
{
    /// <summary>
    /// Interaction logic for HeadersControl.xaml
    /// </summary>
    public partial class MetadataControl
    {
        public MetadataControl()
        {
            InitializeComponent();
        }

        public IMetadataContainerModel Model
        {
            get { return this.DataContext as IMetadataContainerModel; }
        }


        private void OnAddMetadata(object sender, RoutedEventArgs args)
        {
            this.Model.MetadataEntries.Add(new Metadata());
            this._ctlMetadataDataGrid.SelectedIndex = this.Model.MetadataEntries.Count - 1;

            DataGridHelper.PutCellInEditMode(this._ctlMetadataDataGrid, this._ctlMetadataDataGrid.SelectedIndex, 0);
        }

        private void OnRemoveMetadata(object sender, RoutedEventArgs args)
        {
            List<Metadata> itemsToBeRemoved = new List<Metadata>();
            foreach (Metadata entry in this._ctlMetadataDataGrid.SelectedItems)
            {
                itemsToBeRemoved.Add(entry);
            }

            foreach (Metadata entry in itemsToBeRemoved)
            {
                this.Model.MetadataEntries.Remove(entry);
            }
        }
    }
}
