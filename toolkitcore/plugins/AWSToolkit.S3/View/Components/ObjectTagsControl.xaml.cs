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

        public ITagContainerModel Model
        {
            get { return this.DataContext as ITagContainerModel; }
        }

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
