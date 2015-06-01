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


using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.Controller;

using log4net;

namespace Amazon.AWSToolkit.EC2.View.Components
{
    /// <summary>
    /// Interaction logic for RoutesControl.xaml
    /// </summary>
    public partial class RoutesControl
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(RoutesControl));

        ViewRouteTablesController _controller;
        RouteTableWrapper _routeTable;
        public RoutesControl()
        {
            InitializeComponent();
            this.onDataContextChanged(null, default(DependencyPropertyChangedEventArgs));
            this._ctlDataGrid.DataContextChanged += this.onDataContextChanged;
        }

        void onDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this._ctlDelete.IsEnabled = false;
            this._ctlToolbar.IsEnabled = this.DataContext != null;
            this._routeTable = this.DataContext as RouteTableWrapper;
        }

        public void Initialize(ViewRouteTablesController controller)
        {
            this._controller = controller;

            this.DataContext = null;
        }

        void onAddRouteClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var route = this._controller.AddRoute(this._routeTable);
                if (route != null)
                    Amazon.AWSToolkit.CommonUI.DataGridHelper.SelectAndScrollIntoView(this._ctlDataGrid, route);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error adding route", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error adding route: " + e.Message);
            }
        }

        void onDeleteRouteClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                if (this._ctlDataGrid.SelectedItems.Count == 0)
                    return;

                string message = "Are you sure you want to delete this Route?";
                if (!ToolkitFactory.Instance.ShellProvider.Confirm("Delete Route", message))
                    return;

                List<RouteWrapper> toBeDeleted = new List<RouteWrapper>();
                foreach (RouteWrapper route in this._ctlDataGrid.SelectedItems)
                {
                    if(route.CanDelete)
                        toBeDeleted.Add(route);
                }

                this._controller.DeleteRoutes(this._routeTable.RouteTableId,  toBeDeleted);
                this._controller.RefreshRoutes(this._routeTable);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error deleting route(s)", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error deleting route(s): " + e.Message);
            }
        }

        void onRefreshClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.RefreshRoutes(this._routeTable);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing routes", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing routes: " + e.Message);
            }
        }

        void onSelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            try
            {
                this._ctlDelete.IsEnabled = false;
                foreach (RouteWrapper route in this._ctlDataGrid.SelectedItems)
                {
                    if (route.CanDelete)
                    {
                        this._ctlDelete.IsEnabled = true;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGER.Error("Error updating selection", ex);
            }
        }
    }
}
