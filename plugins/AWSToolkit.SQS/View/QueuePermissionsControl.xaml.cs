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
using Amazon.AWSToolkit.SQS.Model;

using Amazon.SQS;
using Amazon.SQS.Model;

namespace Amazon.AWSToolkit.SQS.View
{
    /// <summary>
    /// Interaction logic for QueuePermissionsControl.xaml
    /// </summary>
    public partial class QueuePermissionsControl : BaseAWSControl
    {
        public QueuePermissionsControl()
            : this(new QueuePermissionsModel())
        {
        }

        public QueuePermissionsControl(QueuePermissionsModel model)
        {
            this.Model = model;
            this.DataContext = model;
            InitializeComponent();
        }

        public QueuePermissionsModel Model
        {
            get;
            set;
        }

        public override string Title
        {
            get { return "Queue Permissions"; }
        }

        private void OnAddPermission(object sender, RoutedEventArgs e)
        {
            this.Model.Permissions.Add(new QueuePermissionsModel.PermissionRecord());
            this._ctlDataGrid.SelectedIndex = this.Model.Permissions.Count - 1;

            DataGridHelper.PutCellInEditMode(this._ctlDataGrid, this._ctlDataGrid.SelectedIndex, 0);
        }

        private void OnRemovePermission(object sender, RoutedEventArgs e)
        {            
            int index = this._ctlDataGrid.SelectedIndex;
            if (index < this.Model.Permissions.Count && index >= 0)
            {
                this.Model.Permissions.RemoveAt(index);
            }
        }
    }
}
