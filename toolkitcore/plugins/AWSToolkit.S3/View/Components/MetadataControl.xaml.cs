using System.Collections.Generic;
using System.Windows;
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

        public IMetadataContainerModel Model => this.DataContext as IMetadataContainerModel;


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
