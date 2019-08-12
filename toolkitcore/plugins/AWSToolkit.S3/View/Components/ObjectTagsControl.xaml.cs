using System.Collections.Generic;
using System.Windows;
using Amazon.S3.Model;

using Amazon.AWSToolkit.S3.Model;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.S3.View.Components
{
    /// <summary>
    /// Interaction logic for ObjectTagsControl.xaml
    /// </summary>
    public partial class ObjectTagsControl
    {
        public ObjectTagsControl()
        {
            InitializeComponent();
        }

        public ITagContainerModel Model => this.DataContext as ITagContainerModel;

        private void OnAddTag(object sender, RoutedEventArgs args)
        {
            this.Model.Tags.Add(new Tag());
            this._ctlTaggingGrid.SelectedIndex = this.Model.Tags.Count - 1;

            DataGridHelper.PutCellInEditMode(this._ctlTaggingGrid, this._ctlTaggingGrid.SelectedIndex, 0);
        }

        private void OnRemoveTag(object sender, RoutedEventArgs args)
        {
            List<Tag> itemsToBeRemoved = new List<Tag>();
            foreach (Tag entry in this._ctlTaggingGrid.SelectedItems)
            {
                itemsToBeRemoved.Add(entry);
            }

            foreach (Tag entry in itemsToBeRemoved)
            {
                this.Model.Tags.Remove(entry);
            }
        }
    }
}
