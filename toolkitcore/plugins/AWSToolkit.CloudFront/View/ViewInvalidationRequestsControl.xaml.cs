using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public override string Title
        {
            get
            {
                return string.Format("Invalidations: {0}", this._controller.DistributionId);
            }
        }

        public override string UniqueId
        {
            get{return this.Title; }
        }

        public override bool SupportsBackGroundDataLoad
        {
            get { return true; }
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
