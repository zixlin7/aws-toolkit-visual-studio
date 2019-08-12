using System.Collections.Generic;
using System.Windows;
using Amazon.AWSToolkit.Util;
using Amazon.AWSToolkit.SimpleDB.Controller;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.SimpleDB.View
{
    /// <summary>
    /// Interaction logic for EditAttributeControl.xaml
    /// </summary>
    public partial class EditAttributeControl : BaseAWSControl
    {
        EditAttributeController _controller;

        public EditAttributeControl(EditAttributeController controller)
        {
            this._controller = controller;
            this.DataContext = this._controller.Model;
            InitializeComponent();
        }

        public override string Title => string.Format("Edit {0}", this._controller.Model.AttributeName);

        private void OnAdd(object sender, RoutedEventArgs args)
        {
            this._controller.Model.Values.Add(new MutableString(string.Empty));
            this._ctlValues.SelectedIndex = this._controller.Model.Values.Count - 1;

            DataGridHelper.PutCellInEditMode(this._ctlValues, this._ctlValues.SelectedIndex, 0);
        }

        private void OnRemove(object sender, RoutedEventArgs args)
        {
            List<MutableString> itemsToBeRemoved = new List<MutableString>();
            foreach (MutableString entry in this._ctlValues.SelectedItems)
            {
                itemsToBeRemoved.Add(entry);
            }

            foreach (MutableString entry in itemsToBeRemoved)
            {
                this._controller.Model.Values.Remove(entry);
            }
        }
    }
}
