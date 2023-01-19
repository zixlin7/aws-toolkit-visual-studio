using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Controller;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.Navigator;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using log4net;


namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for ViewKeyPairsControl.xaml
    /// </summary>
    public partial class ViewKeyPairsControl : BaseAWSView
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(ViewInstancesControl));

        ViewKeyPairsController _controller;
        public ViewKeyPairsControl(ViewKeyPairsController controller)
        {
            InitializeComponent();
            this._controller = controller;            
        }

        public override string Title => string.Format("{0} EC2 Key Pairs", this._controller.RegionDisplayName);

        public override string UniqueId => "KeyPairs: " + this._controller.EndPointUniqueIdentifier + "_" + this._controller.Account.Identifier.Id;

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }

        public override void OnEditorOpened(bool success)
        {
            ToolkitFactory.Instance.TelemetryLogger.RecordEc2OpenKeyPairs(new Ec2OpenKeyPairs()
            {
                Result = success ? Result.Succeeded : Result.Failed,
            });
        }

        void onRefreshClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.RefreshKeyPairs();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing key pairs: " + e.Message);
            }
        }

        void onCreateClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var result = this._controller.CreateKeyPair();
                _controller.RecordCreateKeyPair(result);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error Creating Key Pair", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error Creating Key Pair: " + e.Message);
                _controller.RecordCreateKeyPair(ActionResults.CreateFailed(e));
            }
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


        void onGridContextMenu(object sender, RoutedEventArgs e)
        {
            if (this._controller.Model.SelectedKeys.Count == 0)
                return;

            ContextMenu menu = new ContextMenu();

            MenuItem clearKey = new MenuItem() { Header = "Clear Private Key" };
            clearKey.Click += this.onClearPrivateKeyClick;
            clearKey.IsEnabled = this._controller.Model.SelectedKeys.Count(x => x.IsStoredLocally) > 0;

            MenuItem importKey = new MenuItem() { Header = "Import Private Key" };
            importKey.Click += this.onImportPrivateKeyClick;
            importKey.IsEnabled = this._controller.Model.SelectedKeys.Count == 1 && 
                !this._controller.Model.SelectedKeys[0].IsStoredLocally;

            MenuItem exportKey = new MenuItem() { Header = "Export Private Key" };
            exportKey.Click += this.onExportPrivateKeyClick;
            exportKey.IsEnabled = this._controller.Model.SelectedKeys.Count == 1 && 
                this._controller.Model.SelectedKeys[0].IsStoredLocally;

            MenuItem deleteKey = new MenuItem() { Header = "Delete Key Pair" };
            deleteKey.Click += this.onDeleteKeyPairsClick;
            deleteKey.Icon = IconHelper.GetIcon("delete.png");

            MenuItem properties = new MenuItem() { Header = "Properties" };
            properties.Click += this.onPropertiesClick;


            menu.Items.Add(clearKey);
            menu.Items.Add(importKey);
            menu.Items.Add(exportKey);
            menu.Items.Add(new Separator());
            menu.Items.Add(deleteKey);
            menu.Items.Add(new Separator());
            menu.Items.Add(properties);

            menu.PlacementTarget = this;
            menu.IsOpen = true;
        }

        void onSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.UpdateProperties(DataGridHelper.GetSelectedItems<PropertiesModel>(this._ctlDataGrid));

            IList<KeyPairWrapper> selected = DataGridHelper.GetSelectedItems<KeyPairWrapper>(this._ctlDataGrid);

            this._controller.ResetSelection(selected);
            this._ctlDelete.IsEnabled = selected.Count > 0;
        }

        void onDeleteKeyPairsClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var result = this._controller.DeleteKeyPairs();
                _controller.RecordDeleteKeyPair(_controller.Model.SelectedKeys.Count, result);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error deleting key pairs", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error Deleting Key Pairs", "Error deleting key pair: " + e.Message);
                _controller.RecordDeleteKeyPair(1, ActionResults.CreateFailed(e));
            }
        }

        void onClearPrivateKeyClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var result = this._controller.ClearPrivateKeys(this._controller.Model.SelectedKeys);
                _controller.RecordClearPrivateKey(_controller.Model.SelectedKeys.Count, result);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error clearing private key", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error Clearing Private Key(s)", "Error clearing private key(s): " + e.Message);
                _controller.RecordClearPrivateKey(1, ActionResults.CreateFailed(e));
            }
        }

        void onImportPrivateKeyClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                if (this._controller.Model.SelectedKeys.Count() == 0)
                    return;
                var key = this._controller.Model.SelectedKeys[0];

                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.FileName = key.NativeKeyPair.KeyName; // Default file name
                dlg.DefaultExt = ".pem"; // Default file extension
                dlg.Filter = "PEM File (.pem)|*.pem"; // Filter files by extension

                ActionResults actionResult = ActionResults.CreateCancelled();
                Nullable<bool> result = dlg.ShowDialog();

                if (result == true)
                {
                    string filename = dlg.FileName;
                    actionResult = this._controller.ImportPrivatekey(key, filename);
                }

                _controller.RecordImportPrivateKey(actionResult);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error importing private key", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error Importing Private Key", "Error importing private key: " + e.Message);
                _controller.RecordImportPrivateKey(ActionResults.CreateFailed(e));
            }
        }

        void onExportPrivateKeyClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                if(this._controller.Model.SelectedKeys.Count() == 0)
                    return;
                var key = this._controller.Model.SelectedKeys[0];

                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.FileName = key.NativeKeyPair.KeyName; // Default file name
                dlg.DefaultExt = ".pem"; // Default file extension
                dlg.Filter = "PEM File (.pem)|*.pem"; // Filter files by extension

                ActionResults actionResult = ActionResults.CreateCancelled();
                Nullable<bool> result = dlg.ShowDialog();

                if (result == true)
                {
                    string filename = dlg.FileName;
                    actionResult = this._controller.ExportPrivatekey(key, filename);
                }

                _controller.RecordExportPrivateKey(actionResult);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error exporting private key", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error Exporting Private Key", "Error exporting private key: " + e.Message);
                _controller.RecordExportPrivateKey(ActionResults.CreateFailed(e));
            }
        }
    }
}
