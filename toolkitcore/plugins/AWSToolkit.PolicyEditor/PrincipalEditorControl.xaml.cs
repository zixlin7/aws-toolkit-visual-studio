using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.PolicyEditor.Model;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.PolicyEditor
{
    /// <summary>
    /// Interaction logic for PrincipalEditorControl.xaml
    /// </summary>
    public partial class PrincipalEditorControl
    {
        public PrincipalEditorControl()
        {
            InitializeComponent();
        }

        public void OnAddPrincipal(object sender, RoutedEventArgs e)
        {
            StatementModel model = this.DataContext as StatementModel;

            var wrapped = new MutableString();
            wrapped.PropertyChanged += new PropertyChangedEventHandler(model.OnPrincipalChange);
            model.Principals.Add(wrapped);
            this._ctlPrincipals.SelectedIndex = model.Principals.Count - 1;

            DataGridHelper.PutCellInEditMode(this._ctlPrincipals, this._ctlPrincipals.SelectedIndex, 0);

            model.SyncPrincipals();
        }

        public void OnRemovePrincipal(object sender, RoutedEventArgs e)
        {
            StatementModel model = this.DataContext as StatementModel;

            List<MutableString> itemsToBeRemoved = new List<MutableString>();
            foreach (MutableString value in this._ctlPrincipals.SelectedItems)
            {
                itemsToBeRemoved.Add(value);
            }

            foreach (MutableString value in itemsToBeRemoved)
            {
                model.Principals.Remove(value);
            }

            model.SyncPrincipals();
        }
    }
}
