using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.PolicyEditor.Model;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.PolicyEditor
{
    /// <summary>
    /// Interaction logic for ResourceEditorControl.xaml
    /// </summary>
    public partial class ResourceEditorControl
    {
        public ResourceEditorControl()
        {
            InitializeComponent();
        }

        public void OnAddResource(object sender, RoutedEventArgs e)
        {
            StatementModel model = this.DataContext as StatementModel;

            var wrapped = new MutableString();
            wrapped.PropertyChanged += new PropertyChangedEventHandler(model.OnResourceChange);
            model.Resources.Add(wrapped);
            this._ctlResources.SelectedIndex = model.Resources.Count - 1;

            DataGridHelper.PutCellInEditMode(this._ctlResources, this._ctlResources.SelectedIndex, 0);

            model.SyncResources();
        }

        public void OnRemoveResource(object sender, RoutedEventArgs e)
        {
            StatementModel model = this.DataContext as StatementModel;

            List<MutableString> itemsToBeRemoved = new List<MutableString>();
            foreach (MutableString value in this._ctlResources.SelectedItems)
            {
                itemsToBeRemoved.Add(value);
            }

            foreach (MutableString value in itemsToBeRemoved)
            {
                model.Resources.Remove(value);
            }

            model.SyncResources();
        }
    }
}
