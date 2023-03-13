﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.View.DataGrid;
using Amazon.AWSToolkit.RDS.Controller;
using Amazon.AWSToolkit.RDS.Model;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using log4net;
using Amazon.AWSToolkit.Navigator;

namespace Amazon.AWSToolkit.RDS.View
{
    /// <summary>
    /// Interaction logic for ViewDBSubnetGroupsControl.xaml
    /// </summary>
    public partial class ViewDBSubnetGroupsControl : BaseAWSView
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewDBSubnetGroupsControl));

        public ViewDBSubnetGroupsControl()
        {
            InitializeComponent();
        }

        const string COLUMN_USERSETTINGS_KEY = "ViewDBSubnetGroupsControl";
        static readonly string DEFAULT_COLUMN_DEFINITIONS;

        static ViewDBSubnetGroupsControl()
        {
            DEFAULT_COLUMN_DEFINITIONS =
                "[" +
                    "{\"Name\" : \"Name\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"Description\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"Status\", \"Type\" : \"Property\"}, " +
                    "{\"Name\" : \"VpcId\", \"Type\" : \"Property\"} " +
                "]";
        }

        readonly ViewDBSubnetGroupsController _controller;

        public ViewDBSubnetGroupsControl(ViewDBSubnetGroupsController controller)
        {
            InitializeComponent();
            this._controller = controller;

            this._ctlDataGrid.Initialize(null, this._controller.Model.PropertyColumnDefinitions, DEFAULT_COLUMN_DEFINITIONS, COLUMN_USERSETTINGS_KEY);
        }

        public override object GetInitialData()
        {
            return this._controller.InitialDBSubnetGroupIdentifier;
        }

        public override void RefreshInitialData(object initialData)
        {
            try
            {
                this._controller.RefreshSubnetGroups();
                this.UpdateSelection(initialData as string);
            }
            catch (Exception e)
            {
                var errMsg = string.Format("An error occurred attempting to load the DB subnet groups: {0}", e.Message);
                ToolkitFactory.Instance.ShellProvider.ShowError("Data Error", errMsg);
            }
        }

        protected override void PostDataContextBound()
        {
            if (!string.IsNullOrEmpty(this._controller.InitialDBSubnetGroupIdentifier))
                UpdateSelection(this._controller.InitialDBSubnetGroupIdentifier);

        }

        private void UpdateSelection(string subnetGroupIdentifier)
        {
            foreach (var db in this._controller.Model.DBSubnetGroups)
            {
                if (string.Equals(db.DBSubnetGroupIdentifier, subnetGroupIdentifier))
                {
                    this._ctlDataGrid.SelectAndScrollIntoView(db);
                    return;
                }
            }
        }

        public override string Title => string.Format("{0} DB Subnet Groups", this._controller.RegionDisplayName);

        public override string UniqueId => "RDSDBSubnetGroups_" + this._controller.EndPointUniqueIdentifier + "_" + this._controller.Account.Identifier.Id;

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }

        public override void OnEditorOpened(bool success)
        {
            ToolkitFactory.Instance.TelemetryLogger.RecordRdsOpenSubnets(new RdsOpenSubnets()
            {
                Result = success ? Result.Succeeded : Result.Failed,
            });
        }

        void onRefreshClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.RefreshSubnetGroups();
            }
            catch (Exception e)
            {
                var errMsg = string.Format("An error occurred attempting to load the DB subnet groups: {0}", e.Message);
                ToolkitFactory.Instance.ShellProvider.ShowError("Data Error", errMsg);
            }
        }

        void onGridContextMenu(object sender, RoutedEventArgs e)
        {
            if (this._ctlDataGrid.SelectedItems.Count == 0)
                return;

            var menu = new ContextMenu();

            var deleteKey = new MenuItem() { Header = "Delete Subnet Group" };
            deleteKey.Click += this.onDeleteClick;
            deleteKey.Icon = IconHelper.GetIcon("delete.png");

            var properties = new MenuItem() { Header = "Properties" };
            properties.Click += this.onPropertiesClick;

            menu.Items.Add(deleteKey);
            menu.Items.Add(new Separator());
            menu.Items.Add(properties);

            menu.PlacementTarget = this;
            menu.IsOpen = true;
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

        void onCreateClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = this._controller.CreateSubnetGroup(_ctlDataGrid);
                _controller.RecordCreateSubnetGroup(result);
            }
            catch (Exception exc)
            {
                LOGGER.Error("Error Creating DB Subnet Group", exc);
                var errMsg = string.Format("An error occurred attempting to create the DB subnet group: {0}", exc.Message);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error Creating Subnet Group", errMsg);
                _controller.RecordCreateSubnetGroup(ActionResults.CreateFailed(exc));
            }
        }

        void onDeleteClick(object sender, RoutedEventArgs e)
        {
            var count = 0;
            try
            {
                var groups = _ctlDataGrid.GetSelectedItems<DBSubnetGroupWrapper>();
                count = groups.Count;
                var result = _controller.DeleteSubnetGroups(groups);
                _controller.RecordDeleteSubnetGroup(count, result);
            }
            catch (Exception exc)
            {
                LOGGER.Error("Error Deleting DB Subnet Group(s)", exc);
                var errMsg = string.Format("An error occurred attempting to delete the DB subnet group(s): {0}", exc.Message);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error Deleting Subnet Group", errMsg);
                _controller.RecordDeleteSubnetGroup(0, ActionResults.CreateFailed(exc));
            }
        }

        void onSelectionChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                this.UpdateProperties(this._ctlDataGrid.GetSelectedItems<PropertiesModel>());
                var selected = this._ctlDataGrid.GetSelectedItems<DBSubnetGroupWrapper>();

                this._ctlDelete.IsEnabled = selected.Count > 0;
            }
            catch (Exception ex)
            {
                LOGGER.Error("Error updating selection", ex);
            }
        }

        void OnColumnCustomizationApplyPressed(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Trace.WriteLine("OnColumnCustomizationApplyPressed");

            this._ctlDataGrid.ColumnDefinitions = _columnCustomizationPanel.SelectedColumns;
        }

        private void OnColumnCustomizationDropPanelOpening(object sender, RoutedEventArgs e)
        {
            EC2ColumnDefinition[] fixedAttributes = this._controller.Model.PropertyColumnDefinitions;
            EC2ColumnDefinition[] displayedColumns = this._ctlDataGrid.ColumnDefinitions;

            _columnCustomizationPanel.SetColumnData(fixedAttributes, null, displayedColumns);
        }
    }
}
