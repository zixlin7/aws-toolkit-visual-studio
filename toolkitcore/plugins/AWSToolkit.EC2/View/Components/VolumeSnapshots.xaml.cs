using System;
using System.Collections.Generic;
using System.ComponentModel;
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

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Controller;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View.DataGrid;

using log4net;

namespace Amazon.AWSToolkit.EC2.View.Components
{
    /// <summary>
    /// Interaction logic for VolumeSnapshots.xaml
    /// </summary>
    public partial class VolumeSnapshots
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(VolumeSnapshots));
        ViewVolumesController _controller;
        bool _displayProperties;

        const string COLUMN_USERSETTINGS_KEY = "SubSnapshotsGrid";
        static readonly string DEFAULT_SNAPSHOTS_COLUMN_DEFINITIONS;

        static VolumeSnapshots()
        {
            DEFAULT_SNAPSHOTS_COLUMN_DEFINITIONS =
                "[" +
                    "{\"Name\" : \"SnapshotId\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"Name\", \"Type\" : \"Tag\"}, " +
                    "{\"Name\" : \"VolumeSize\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"Description\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"Status\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"Started\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"Progress\", \"Type\" : \"Property\"} " +
                "]";
        }

        public VolumeSnapshots()
        {
            InitializeComponent();
            this.IsEnabled = false;
            this.DataContextChanged += this.onDataContextChanged;
        }

        public void Initialize(ViewVolumesController controller)
        {
            this._controller = controller;
            this._ctlDataGrid.Initialize(this._controller.EC2Client, this._controller.Model.SnapshotPropertyColumnDefinitions, DEFAULT_SNAPSHOTS_COLUMN_DEFINITIONS, COLUMN_USERSETTINGS_KEY);
        }

        void onDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.Model == null || !Model.IsSnapshotsReady)
            {
                if (this.Model != null)
                    this.Model.PropertyChanged += onPropertyChanged;

                this.IsEnabled = false;
            }
            else
            {
                this.IsEnabled = true;
            }
        }

        void onPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.IsEnabled = this.Model != null && this.Model.IsSnapshotsReady;
        }

        VolumeWrapper Model
        {
            get
            {
                return this.DataContext as VolumeWrapper;
            }
        }

        void onRefreshClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.RefreshFocusSnapshots();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing list of snapshots", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing list of snapshots: " + e.Message);
            }
        }

        public void onCreateSnapshotClick(object sender, RoutedEventArgs evnt)
        {
            this._controller.CreateSnapshot(this.Model);
        }

        public void onDeleteSnapshotClick(object sender, RoutedEventArgs evnt)
        {
            _controller.DeleteSnapshots(getSelectedItemsAsList());
        }

        void onGridContextMenu(object sender, RoutedEventArgs e)
        {
            List<SnapshotWrapper> selectedItems = getSelectedItemsAsList();
            if (selectedItems.Count == 0)
                return;

            ContextMenu menu = new ContextMenu();

            MenuItem deleteSnapshot = createMenuItem("Delete", this.onDeleteSnapshotClick);
            deleteSnapshot.Icon = IconHelper.GetIcon("delete.png");

            menu.Items.Add(deleteSnapshot);

            menu.PlacementTarget = this;
            menu.IsOpen = true;
        }

        List<SnapshotWrapper> getSelectedItemsAsList()
        {
            var items = new List<SnapshotWrapper>();
            foreach (SnapshotWrapper selectedItem in this._ctlDataGrid.SelectedItems)
            {
                items.Add(selectedItem);
            }

            return items;
        }

        MenuItem createMenuItem(string header, RoutedEventHandler handler)
        {
            MenuItem mi = new MenuItem() { Header = header };
            mi.Click += handler;
            return mi;
        }

        private void onSelectionChanged(object sender, RoutedEventArgs e)
        {
            if (this._displayProperties)
            {
                this.setSelectedProperties();
            }
        }

        private void onGotFocus(object sender, RoutedEventArgs e)
        {
            this.setSelectedProperties();
            this._displayProperties = true;
        }

        private void onLostFocus(object sender, RoutedEventArgs e)
        {
            this._displayProperties = false;
        }


        void setSelectedProperties()
        {
            var parent = this.Parent as FrameworkElement;
            while (parent != null)
            {
                if (parent is BaseAWSView)
                {
                    ((BaseAWSView)parent).UpdateProperties(this._ctlDataGrid.GetSelectedItems<PropertiesModel>());
                    break;
                }
                parent = parent.Parent as FrameworkElement;
            }
        }

        void OnColumnCustomizationApplyPressed(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Trace.WriteLine("OnColumnCustomizationApplyPressed");

            this._ctlDataGrid.ColumnDefinitions = _columnCustomizationPanel.SelectedColumns;
        }

        private void OnColumnCustomizationDropPanelOpening(object sender, RoutedEventArgs e)
        {
            EC2ColumnDefinition[] fixedAttributes = this._controller.Model.VolumePropertyColumnDefinitions;
            string[] tagColumns = this.Model.ListSnapshotsAvailableTags;
            EC2ColumnDefinition[] displayedColumns = this._ctlDataGrid.ColumnDefinitions;

            _columnCustomizationPanel.SetColumnData(fixedAttributes, tagColumns, displayedColumns);
        }
    }
}
