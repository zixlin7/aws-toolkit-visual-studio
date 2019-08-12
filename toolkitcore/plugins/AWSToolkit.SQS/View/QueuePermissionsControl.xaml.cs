using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.SQS.Model;

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

        public override string Title => "Queue Permissions";

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
