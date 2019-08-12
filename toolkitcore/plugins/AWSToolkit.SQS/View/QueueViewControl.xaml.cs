using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.SQS.Model;
using Amazon.AWSToolkit.SQS.Controller;
using log4net;

namespace Amazon.AWSToolkit.SQS.View
{
    /// <summary>
    /// Interaction logic for QueueViewWindow.xaml
    /// </summary>
    public partial class QueueViewControl : BaseAWSView
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(QueueViewControl));

        private const int NUMBER_OF_FETCH_REQUESTS = 5;

        QueueViewCommand _controller;

        public QueueViewControl(QueueViewCommand controller)
        {
            this._controller = controller;
            InitializeComponent();

            this.DataContextChanged += onDataContextChanged;
        }

        void onDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this._controller.Model.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(onChange);
        }

        void onChange(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this._ctlSave.IsEnabled = this._controller.Model.IsDirty();
        }

        public override string Title => "Queue: " + this._controller.Model.Name;

        public override string UniqueId => string.Format("SQSSampleWindow.{0}", this._controller.Model.QueueURL);


        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model ;
        }

        private void save_Click(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.SaveAttributes();
                this._ctlSave.IsEnabled = false;
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error saving queue: " + e.Message);
            }
        }

        private void send_Click(object sender, RoutedEventArgs evnt)
        {
            try
            {
                if (this._controller.SendMessage())
                {
                    this._controller.Refresh();
                }
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error sending message: " + e.Message);
            }
        }

        private void purge_Click(object sender, RoutedEventArgs evnt)
        {
            try
            {
                if (this._controller.PurgeQueue())
                {
                    this._controller.Refresh();
                }
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error purging message: " + e.Message);
            }
        }

        private void refresh_Click(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.Refresh();
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing queue: " + e.Message);
            }
        }

        private void _ctlDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MessageWrapper w = this._ctlDataGrid.SelectedItem as MessageWrapper;
            if (w == null)
                return;
            ExamineMessageControl control = new ExamineMessageControl(w);
            ToolkitFactory.Instance.ShellProvider.ShowModal(control, MessageBoxButton.OK);
        }

        void onSelectionChanged(object sender, SelectionChangedEventArgs evnt)
        {
            try
            {
                this.UpdateProperties(DataGridHelper.GetSelectedItems<PropertiesModel>(this._ctlDataGrid));
            }
            catch (Exception e)
            {
                LOGGER.Error("Error displaying properties", e);
            }
        }

        void onGridContextMenu(object sender, RoutedEventArgs e)
        {
            var items = DataGridHelper.GetSelectedItems<PropertiesModel>(this._ctlDataGrid);
            if (items.Count == 0)
                return;

            ContextMenu menu = new ContextMenu();

            MenuItem properties = new MenuItem() { Header = "Properties" };
            properties.Click += this.onPropertiesClick;

            menu.Items.Add(properties);

            menu.PlacementTarget = this;
            menu.IsOpen = true;
        }

        void onPropertiesClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this.ShowProperties(DataGridHelper.GetSelectedItems<PropertiesModel>(this._ctlDataGrid));
            }
            catch (Exception e)
            {
                LOGGER.Error("Error displaying properties", e);
            }
        }
    }
}
