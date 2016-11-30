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
using Amazon.AWSToolkit.CloudFront.Model;
using Amazon.AWSToolkit.CloudFront.Controller;

using log4net;

namespace Amazon.AWSToolkit.CloudFront.View
{
    /// <summary>
    /// Interaction logic for EditDistributionConfigControl.xaml
    /// </summary>
    public partial class EditDistributionConfigControl : DistributionConfigEditor
    {
        readonly static ILog LOGGER = LogManager.GetLogger(typeof(EditDistributionConfigControl));
        EditDistributionConfigController _controller;

        public EditDistributionConfigControl(EditDistributionConfigController controller)
        {
            this._controller = controller;
            InitializeComponent();
            this._ctlConfigEditor.Initialize(this._controller);
        }

        public override string Title
        {
            get
            {
                return this._controller.Title;
            }
        }

        public override bool SupportsBackGroundDataLoad
        {
            get { return true; }
        }

        public override string UniqueId
        {
            get
            {
                return this._controller.UniqueId;
            }
        }

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }

        private void onRefreshClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.Refresh();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing distribution: " + e.Message);
            }
        }

        private void onApplyChangesClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.Persist();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error applying changes", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error applying changes to distribution: " + e.Message);
            }
        }

        private void onDomainNameClick(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                BaseDistributionConfigEditorController.OpenDistributionUrl(this._controller.Model.DomainName, "", "");
            }
            catch (Exception ex)
            {
                LOGGER.Error("Failed to launch process to go to endpoint", ex);
            }
            finally
            {
                e.Handled = true;
            }
        }

        private void Status_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            _domainLink.IsEnabled = "Deployed".Equals(this._controller.Model.Status, StringComparison.Ordinal)
                && this._controller.Model.Enabled;
        }
    }
}
