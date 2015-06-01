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
    /// Interaction logic for InstanceVolumes.xaml
    /// </summary>
    public partial class InstanceVolumes
    {

        static readonly ILog LOGGER = LogManager.GetLogger(typeof(InstanceVolumes));

        const string COLUMN_USERSETTINGS_KEY = "SubVolumeGrid";
        static readonly string DEFAULT_VOLUMES_COLUMN_DEFINITIONS;

        static InstanceVolumes()
        {
            DEFAULT_VOLUMES_COLUMN_DEFINITIONS =
                "[" +
                    "{\"Name\" : \"VolumeId\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"Name\", \"Type\" : \"Tag\"}, " +
                    "{\"Name\" : \"Capacity\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"SnapshotId\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"Created\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"AvailabilityZone\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"Status\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"Attachments\", \"Type\" : \"Property\"} " +
                    "{\"Name\" : \"VolumeTypeDisplayName\", \"Type\" : \"Property\"} " +
                "]";
        }

        bool _displayProperties = false;

        ViewInstancesController _controller;
        public InstanceVolumes()
        {
            InitializeComponent();
            this.IsEnabled = false;
            this.DataContextChanged += this.onDataContextChanged;
        }

        void onDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.Model == null || !Model.IsVolumesReady)
            {
                if (this.Model != null && this.Model.NativeInstance.State.Name == EC2Constants.INSTANCE_STATE_TERMINATED)
                {
                    this.IsEnabled = false;
                    return;
                }

                if(this.Model != null)
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
            if (this.Model != null)
            {
                this.IsEnabled = this.Model.IsVolumesReady;
            }
        }

        RunningInstanceWrapper Model
        {
            get
            {
                return this.DataContext as RunningInstanceWrapper;
            }
        }

        public void Initialize(ViewInstancesController controller)
        {
            this._controller = controller;
            this._ctlDataGrid.Initialize(this._controller.EC2Client, this._controller.Model.VolumePropertyColumnDefinitions, DEFAULT_VOLUMES_COLUMN_DEFINITIONS, COLUMN_USERSETTINGS_KEY);
        }

        void onCreateVolumeClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.CreateVolumeFocusInstance();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error creating volume for instance", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating volume for: " + e.Message);
            }
        }

        void onRefreshClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.RefreshFocusVolumes();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing list of volumes", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing list of volumes: " + e.Message);
            }
        }

        void onGridContextMenu(object sender, RoutedEventArgs evnt)
        {
            if(this._ctlDataGrid.SelectedItems.Count == 0)
                return;
            ContextMenu menu = new ContextMenu();

            MenuItem detachVolume = new MenuItem();
            detachVolume.Header = "Detach Volume(s)";
            detachVolume.Icon = IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.detach-volume.png");
            detachVolume.Click += this.onDetachVolumeClick;

            menu.Items.Add(detachVolume);
            menu.PlacementTarget = this;
            menu.IsOpen = true;
        }

        void onDetachVolumeClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var volumes = getSelectedItemsAsList(true);
                if (volumes.Count == 0)
                    return;

                this._controller.DetachVolumeFocusInstance(volumes);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error detaching volume(s)", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error Detaching", "Error detaching volume(s): " + e.Message);
            }
        }

        List<VolumeWrapper> getSelectedItemsAsList(bool VolumeWrapper)
        {
            var items = new List<VolumeWrapper>();
            foreach (VolumeWrapper selectedItem in this._ctlDataGrid.SelectedItems)
            {
                 items.Add(selectedItem);
            }

            return items;
        }

        private void onSelectionChanged(object sender, RoutedEventArgs evnt)
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
            string[] tagColumns = this.Model.ListVolumeAvailableTags;
            EC2ColumnDefinition[] displayedColumns = this._ctlDataGrid.ColumnDefinitions;

            _columnCustomizationPanel.SetColumnData(fixedAttributes, tagColumns, displayedColumns);
        }

    }
}
