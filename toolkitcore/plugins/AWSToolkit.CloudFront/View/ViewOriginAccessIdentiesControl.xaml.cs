using System;
using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CloudFront.Controller;
using Amazon.AWSToolkit.CloudFront.Model;

using log4net;

namespace Amazon.AWSToolkit.CloudFront.View
{
    /// <summary>
    /// Interaction logic for ViewOriginAccessIdentiesControl.xaml
    /// </summary>
    public partial class ViewOriginAccessIdentiesControl : BaseAWSControl
    {
        ILog _logger = LogManager.GetLogger(typeof(ViewOriginAccessIdentiesControl));
        bool _turnedOffAutoScroll;
        ViewOriginAccessIdentiesController _controller;

        public ViewOriginAccessIdentiesControl(ViewOriginAccessIdentiesController controller)
        {
            this._controller = controller;
            InitializeComponent();
        }

        public override string Title => "Origin Access Identities";

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }

        void onAddIdentity(object sender, RoutedEventArgs e)
        {
            try
            {
                this._controller.CreateOriginAccessIdentity();
                this._ctlDataGrid.SelectedIndex = this._controller.Model.Identities.Count - 1;
                this._ctlDataGrid.ScrollIntoView(this._controller.Model.Identities[this._ctlDataGrid.SelectedIndex]);
            }
            catch (Exception ex)
            {
                this._logger.Error("Error creating new origin access identity", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating new origin access identity: " + ex.Message);
            }
        }

        void onRemoveIdentity(object sender, RoutedEventArgs e)
        {
            string msg = "Are you sure you want to delete the selected origin access identities?";
            if (!ToolkitFactory.Instance.ShellProvider.Confirm("Delete Origin Access Identities", msg))
                return;

            try
            {
                var identities = new ViewOriginAccessIdentiesModel.OriginAccessIdentity[this._ctlDataGrid.SelectedItems.Count];
                int i = 0;
                foreach (ViewOriginAccessIdentiesModel.OriginAccessIdentity identity in this._ctlDataGrid.SelectedItems)
                {
                    identities[i++] = identity;
                }
                this._controller.DeleteOriginAccessIdentities(identities);
            }
            catch (Exception ex)
            {
                this._logger.Error("Error deleting origin access identity", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error deleting origin access identity: " + ex.Message);
            }
        }

        void onLoad(object sender, RoutedEventArgs e)
        {
            if (!this._turnedOffAutoScroll)
            {
                DataGridHelper.TurnOffAutoScroll(this._ctlDataGrid);
                this._turnedOffAutoScroll = true;
            }
        }
    }
}
