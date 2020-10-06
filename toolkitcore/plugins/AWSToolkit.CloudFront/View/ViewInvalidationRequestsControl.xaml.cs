using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CloudFront.Model;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.CloudFront.Controller;

using log4net;

namespace Amazon.AWSToolkit.CloudFront.View
{
    /// <summary>
    /// Interaction logic for ViewInvalidationRequestsControl.xaml
    /// </summary>
    public partial class ViewInvalidationRequestsControl : BaseAWSView
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewInvalidationRequestsControl));
        ViewInvalidationRequestsController _controller;

        public ViewInvalidationRequestsControl(ViewInvalidationRequestsController controller)
        {
            InitializeComponent();
            this.DataContextChanged += onDataContextChanged;

            this._controller = controller;
        }

        void onDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ViewInvalidationRequestsModel model = this.DataContext as ViewInvalidationRequestsModel;
            if (model == null || model.Summaries.Count == 0)
                return;

            this._ctlRequestPicker.SelectedItem = model.Summaries[0];
        }

        public override string Title => string.Format("Invalidations: {0}", this._controller.DistributionId);

        public override string UniqueId => this.Title;

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }

        public override void OnEditorOpened(bool success)
        {
            ToolkitFactory.Instance.TelemetryLogger.RecordCloudfrontOpenInvalidationRequest(new CloudfrontOpenInvalidationRequest()
            {
                Result = success ? Result.Succeeded : Result.Failed,
            });
        }

        void onRefreshClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                InvalidationSummaryWrapper selected = this._ctlRequestPicker.SelectedItem as InvalidationSummaryWrapper;
                this._controller.Refresh();
                if (selected != null)
                {
                    foreach(var summary in this._controller.Model.Summaries)
                    {
                        if (selected.Id.Equals(summary.Id))
                        {
                            this._ctlRequestPicker.SelectedItem = summary;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing key pairs: " + e.Message);
            }
        }

        void onSelectionChanged(object sender, SelectionChangedEventArgs evnt)
        {
            this._ctlCreateTime.Text = "";
            try
            {
                InvalidationSummaryWrapper selected = this._ctlRequestPicker.SelectedItem as InvalidationSummaryWrapper;

                if (selected != null)
                {
                    this._ctlDetailsGrid.DataContext = selected;
                    if (selected.Paths == null)
                    {
                        selected.Paths = new ObservableCollection<InvalidationSummaryWrapper.InvalidationPath>();
                        this._controller.RefreshPaths(selected);
                    }

                    if (selected.CreateTime != null)
                        this._ctlCreateTime.Text = selected.CreateTime.Value.ToLocalTime().ToString();
                }
                else
                {
                    this._ctlDetailsGrid.DataContext = null;
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Error loading invalidation request", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error loading invalidation request: " + e.Message);
            }
        }
    }
}
