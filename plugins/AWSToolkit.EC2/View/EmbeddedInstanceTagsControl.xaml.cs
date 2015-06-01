using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

using log4net;

using Amazon.AWSToolkit.CommonUI;

using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Embeddable instance tag editor control
    /// </summary>
    public partial class EmbeddedInstanceTagsControl
    {
        ObservableCollection<Tag> _instanceTags = new ObservableCollection<Tag>();
        const int Max_Instance_Tags = 10;
        static ILog LOGGER = LogManager.GetLogger(typeof(EmbeddedInstanceTagsControl));

        public EmbeddedInstanceTagsControl()
        {
            InitializeComponent();
            this._instanceTagsGrid.ItemsSource = _instanceTags;
        }

        public ICollection<Tag> InstanceTags 
        {
            get 
            { 
                // filter out empty rows that might have been left by cancelled edits
                List<Tag> tags = new List<Tag>();
                foreach (Tag tag in _instanceTags)
                {
                    if (!string.IsNullOrEmpty(tag.Key))
                        tags.Add(tag);
                }

                return tags;
            }
            set
            {
                _instanceTags.Clear();
                foreach (Tag tag in value)   
                {
                    // don't expect empty key values inbound but play safe
                    if (!string.IsNullOrEmpty(tag.Key))
                        _instanceTags.Add(tag);
                }

                _addAnother.IsEnabled = _instanceTags.Count < Max_Instance_Tags;
            }
        }

        private void AddAnother_Click(object sender, RoutedEventArgs e)
        {
            _instanceTags.Add(new Tag());
            if (_instanceTags.Count == Max_Instance_Tags)
                _addAnother.IsEnabled = false;

            // todo: usability tweak here - put focus into the new key cell...
        }

        private void RemoveTag_Click(object sender, RoutedEventArgs e)
        {
            // todo: remove/hide/disable button eventually....
            Tag tagCellData = _instanceTagsGrid.CurrentCell.Item as Tag;
            if (string.Compare(tagCellData.Key, EC2Constants.TAG_NAME, true) == 0)
            {
                tagCellData.Value = string.Empty;
            }
            else
            {
                for (int i = _instanceTags.Count-1; i > 0; i--)
                {
                    if (string.Compare(_instanceTags[i].Key, tagCellData.Key, true) == 0)
                    {
                        _instanceTags.RemoveAt(i);
                        _addAnother.IsEnabled = _instanceTags.Count < Max_Instance_Tags;
                        return;
                    }
                }
            }
        }

        // used to trap and disallow edit request on special 'Name' key entry
        private void InstanceTagsGrid_BeginningEdit(object sender, System.Windows.Controls.DataGridBeginningEditEventArgs e)
        {
            if (e.Column.DisplayIndex > 0)
                return;

            // like the console, we expect that the reserved tag 'Name' is the first entry but play safe....
            Tag tagCellData = _instanceTagsGrid.CurrentCell.Item as Tag;
            if (tagCellData != null && string.Compare(tagCellData.Key, EC2Constants.TAG_NAME, true) == 0)
            {
                e.Cancel = true;
                MessageBox.Show("'Name' is a reserved tag and cannot be edited.", 
                                "Reserved Tag", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // used to trap attempts to create a duplicate key
        private void InstanceTagsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Cancel)
                return;

            TextBox editBox = e.EditingElement as TextBox;
            if (editBox == null)
            {
                LOGGER.ErrorFormat("Expected but did not receive TextBox EditingElement type for CellEditEnding event at row {0} column {1}; cannot validate for dupes.",
                                    e.Row.GetIndex(), e.Column.DisplayIndex);
                return;
            }

            string pendingEntry = editBox.Text;
            if (pendingEntry.StartsWith("aws:", StringComparison.InvariantCultureIgnoreCase))
            {
                e.Cancel = true;
                MessageBox.Show("The text 'aws:' is reserved and may not be used as a prefix for key names and values.",
                                "Reserved Text", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            Tag tagCellData = _instanceTagsGrid.CurrentCell.Item as Tag;
            if (tagCellData != null)
            {
                foreach (Tag tag in _instanceTags)
                {
                    if (tag != tagCellData && string.Compare(tag.Key, pendingEntry, true) == 0)
                    {
                        e.Cancel = true;
                        MessageBox.Show(string.Format("A value already exists for key '{0}'.", pendingEntry), 
                                        "Duplicate Tag", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
