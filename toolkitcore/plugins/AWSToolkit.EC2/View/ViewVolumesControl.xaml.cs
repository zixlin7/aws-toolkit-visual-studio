using System;
using System.Collections.Generic;
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

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for ViewVolumesControl.xaml
    /// </summary>
    public partial class ViewVolumesControl : BaseAWSView
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewVolumesControl)); 
        
        const string COLUMN_USERSETTINGS_KEY = "ViewVolumesControlGrid";
        static readonly string DEFAULT_VOLUMES_COLUMN_DEFINITIONS;

        static ViewVolumesControl()
        {
            DEFAULT_VOLUMES_COLUMN_DEFINITIONS =
                "[" +
                    "{\"Name\" : \"Name\", \"Type\" : \"Tag\"}, " +
                    "{\"Name\" : \"VolumeId\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"Capacity\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"SnapshotId\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"Created\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"AvailabilityZone\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"Status\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"Attachments\", \"Type\" : \"Property\"} " +
                    "{\"Name\" : \"VolumeTypeDisplayName\", \"Type\" : \"Property\"} " +
                "]";
        }     


        ViewVolumesController _controller;
        public ViewVolumesControl(ViewVolumesController controller)
        {
            InitializeComponent();
            this._controller = controller;
            _ctlVolumeSnapshots.Initialize(_controller);

            this._ctlDataGrid.Initialize(this._controller.EC2Client, this._controller.Model.VolumePropertyColumnDefinitions, DEFAULT_VOLUMES_COLUMN_DEFINITIONS, COLUMN_USERSETTINGS_KEY);
        }

        public override string Title
        {
            get
            {
                return String.Format("{0} EBS Volumes", this._controller.RegionDisplayName);
            }
        }

        public override string UniqueId
        {
            get
            {
                return "Volumes_" + this._controller.EndPointUniqueIdentifier + "_" + this._controller.Account.SettingsUniqueKey;
            }
        }

        public override bool SupportsBackGroundDataLoad
        {
            get
            {
                return true;
            }
        }

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }

        void onRefreshClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.RefreshVolumeList();
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Data Error", "Error Loading EBS Volume Data: " + e.Message);
            }
        }

        void onCreateVolumeClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var volume = this._controller.CreateVolume();
                if (volume != null)
                {
                    this._ctlDataGrid.SelectAndScrollIntoView(volume);
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Error creating volume", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Create Error", "Error creating volume: " + e.Message);
            }
        }

        void onDeleteVolumeClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                _controller.DeleteVolume(this._ctlDataGrid.GetSelectedItems<VolumeWrapper>());
            }
            catch (Exception e)
            {
                LOGGER.Error("Error deleting volumes", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Delete Error", "Error deleting volume(s): " + e.Message);
            }
        }

        void onAttachVolumeClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                IList<VolumeWrapper> selected = this._ctlDataGrid.GetSelectedItems<VolumeWrapper>();
                if (selected.Count < 1)
                    return;

                _controller.AttachVolume(selected[0]);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error attaching volume", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Attach Error", "Error attaching volume(s): " + e.Message);
            }
        }

        void onDetachVolumeClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                _controller.DetachVolumeFocusInstance(this._ctlDataGrid.GetSelectedItems<VolumeWrapper>(), false);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error detaching volumes", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Detach Error", "Error detaching volume(s): " + e.Message);
            }
        }

        void onForceDetachVolumeClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                _controller.DetachVolumeFocusInstance(this._ctlDataGrid.GetSelectedItems<VolumeWrapper>(), true);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error force detaching volumes", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Force Detach Error", "Error detaching volume(s) by force: " + e.Message);
            }

        }

        void onCreateSnapshotClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                IList<VolumeWrapper> selected = this._ctlDataGrid.GetSelectedItems<VolumeWrapper>();

                if (selected.Count < 1)
                    return;

                this._controller.CreateSnapshot(selected[0]);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error creating snapshot", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Create Snapshot Error", "Error creating snapshot: " + e.Message);
            }
        }

        void onGridContextMenu(object sender, RoutedEventArgs e)
        {
            IList<VolumeWrapper> selectedItems = this._ctlDataGrid.GetSelectedItems<VolumeWrapper>();
            if (selectedItems.Count == 0)
                return;

            ContextMenu menu = new ContextMenu();

            MenuItem deleteVolume = createMenuItem("Delete", this.onDeleteVolumeClick);
            deleteVolume.Icon = IconHelper.GetIcon("delete.png");

            MenuItem attachVolume = createMenuItem("Attach Volume", this.onAttachVolumeClick);
            attachVolume.Icon = IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.attach-volume.png");

            MenuItem detachVolume = createMenuItem("Detach Volume", this.onDetachVolumeClick);
            detachVolume.Icon = IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.detach-volume.png");

            MenuItem forceDetachVolume = createMenuItem("Force Detach", this.onForceDetachVolumeClick);
            forceDetachVolume.Icon = IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.detach-volume.png");

            MenuItem createSnapshot = createMenuItem("Create Snapshot", this.onCreateSnapshotClick);
            createSnapshot.Icon = IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.create-snapshot.png");

            MenuItem properties = new MenuItem() { Header = "Properties" };
            properties.Click += this.onPropertiesClick;

            if (selectedItems.Count > 1)
            {
                createSnapshot.IsEnabled = false;
                attachVolume.IsEnabled = false;
                detachVolume.IsEnabled = false;
                forceDetachVolume.IsEnabled = false;
            }
            else
            {
                string status = selectedItems[0].NativeVolume.State;
                switch (status)
                {
                    case EC2Constants.VOLUME_STATE_AVAILABLE:
                        detachVolume.IsEnabled = false;
                        forceDetachVolume.IsEnabled = false;
                        break;
                    case EC2Constants.VOLUME_STATE_IN_USE:
                        attachVolume.IsEnabled = false;
                        deleteVolume.IsEnabled = false;
                        break;
                    default:
                        createSnapshot.IsEnabled = false;
                        attachVolume.IsEnabled = false;
                        detachVolume.IsEnabled = false;
                        forceDetachVolume.IsEnabled = false;
                        break;
                }
            }

            foreach (VolumeWrapper vol in selectedItems)
            {
                if (!vol.CanDelete)
                {
                    deleteVolume.IsEnabled = false;
                    break;
                }
            }

            menu.Items.Add(attachVolume);
            menu.Items.Add(detachVolume);
            menu.Items.Add(forceDetachVolume);
            menu.Items.Add(new Separator());
            menu.Items.Add(createSnapshot);
            menu.Items.Add(new Separator());
            menu.Items.Add(deleteVolume);
            menu.Items.Add(new Separator());
            menu.Items.Add(properties);

            menu.PlacementTarget = this;
            menu.IsOpen = true;
        }

        MenuItem createMenuItem(string header, RoutedEventHandler handler)
        {
            MenuItem mi = new MenuItem() { Header = header };
            mi.Click += handler;
            return mi;
        }

        void onGotFocus(object sender, RoutedEventArgs e)
        {
            this.UpdateProperties(this._ctlDataGrid.GetSelectedItems<PropertiesModel>());
        }

        void onSelectionChanged(object sender, RoutedEventArgs e)
        {
            this.UpdateProperties(this._ctlDataGrid.GetSelectedItems<PropertiesModel>());
            List<VolumeWrapper> selectedVolumes = new List<VolumeWrapper>();
            foreach (VolumeWrapper volume in _ctlDataGrid.SelectedItems)
            {
                selectedVolumes.Add(volume);
            }

            _controller.ResetSelection(selectedVolumes);

            this._ctlDelete.IsEnabled = true;
            foreach (var volume in selectedVolumes)
            {
                if (!volume.CanDelete)
                {
                    this._ctlDelete.IsEnabled = false;
                    break;
                }
            }
        }

        void onPropertiesClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this.ShowProperties(this._ctlDataGrid.GetSelectedItems<PropertiesModel>());
            }
            catch (Exception e)
            {
                LOGGER.Error("Error displaying properties", e);
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
            string[] tagColumns = this._controller.Model.ListVolumeAvailableTags;
            EC2ColumnDefinition[] displayedColumns = this._ctlDataGrid.ColumnDefinitions;

            _columnCustomizationPanel.SetColumnData(fixedAttributes, tagColumns, displayedColumns);
        }
    }
}
