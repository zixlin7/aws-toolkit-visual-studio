using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;

using Amazon.AWSToolkit.CommonUI.Components;
using Amazon.AWSToolkit.ElasticBeanstalk.Controller;

using log4net;

namespace Amazon.AWSToolkit.ElasticBeanstalk.View.Components
{
    /// <summary>
    /// Interaction logic for TailLogViewer.xaml
    /// </summary>
    public partial class TailLogViewer
    {
        // this is in seconds.
        const int MAX_WAIT_TIME_FOR_SNAPSHOTS = 5;

        static ILog LOGGER = LogManager.GetLogger(typeof(TailLogViewer));
        EnvironmentStatusController _controller;
        DateTime _lastLogTimestamp;
        LoadingMessageAdorner _loadingAdorner;
        AdornerLayer _pageRootAdornerLayer;


        public TailLogViewer()
        {
            InitializeComponent();

            this._ctlNoLogFileMessage.Visibility = System.Windows.Visibility.Visible;
        }

        public void Initialize(EnvironmentStatusController controller)
        {
            this._controller = controller;
            
            this._loadingAdorner = new LoadingMessageAdorner(this, "Waiting for Snapshot...");
        }

        private void RefreshClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var result = this._controller.RetrieveEnvironmentLogs();
                buildLinksToLogs(result);
            }
            catch (Exception e)
            {
                setLoadingAdorner(false);
                LOGGER.Error("Error refreshing environment logs", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing environment logs: " + e.Message);
            }
        }

        private void SnapshotLogsClick(object sender, RoutedEventArgs evnt)
        {

            try
            {
                setLoadingAdorner(true);
                this._controller.RequestEnvironmentLogs();
                ThreadPool.QueueUserWorkItem(this.pollAfterSnapshot);
            }
            catch (Exception e)
            {
                setLoadingAdorner(false);
                LOGGER.Error("Error snapshoting logs", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error snapshoting logs: " + e.Message);
            }
        }

        void setLoadingAdorner(bool enable)
        {
            this._ctlRefresh.IsEnabled = !enable;
            this._ctlSnapshot.IsEnabled = !enable;

            if (this._pageRootAdornerLayer == null)
                this._pageRootAdornerLayer = AdornerLayer.GetAdornerLayer(this);


            if (enable)
                this._pageRootAdornerLayer.Add(this._loadingAdorner);
            else
            {
                var adorners = this._pageRootAdornerLayer.GetAdorners(this);
                if (adorners != null && adorners.Contains(this._loadingAdorner))
                    this._pageRootAdornerLayer.Remove(this._loadingAdorner);
            }
        }

        void pollAfterSnapshot(object sender)
        {
            RetrieveEnvironmentInfoResponse result = null;
            long start = DateTime.Now.Ticks;
            try
            {
                do
                {
                    result = this._controller.RetrieveEnvironmentLogs();
                    if (result.EnvironmentInfo.Count > 0)
                    {
                        var query = from info in result.EnvironmentInfo
                                    select info.SampleTimestamp;
                        var maxTimestamp = query.Max();
                        if (this._lastLogTimestamp < maxTimestamp)
                        {
                            break;
                        }
                        else
                        {
                            Thread.Sleep(750);
                        }
                    }

                } while (new TimeSpan(DateTime.Now.Ticks - start).TotalSeconds < MAX_WAIT_TIME_FOR_SNAPSHOTS);

                ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
                {
                    this.buildLinksToLogs(result);
                }));
            }
            catch (Exception e)
            {
                LOGGER.Error("Error polling for new environment logs", e);
                ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
                {
                    this.buildLinksToLogs(null);
                }));
            }
        }

        private void buildLinksToLogs(RetrieveEnvironmentInfoResponse result)
        {
            setLoadingAdorner(false);

            this._ctlLogFileLinks.Children.Clear();
            if (result == null || result.EnvironmentInfo.Count == 0)
            {
                this._ctlLogOuterContainer.Visibility = System.Windows.Visibility.Hidden;
                this._ctlNoLogFileMessage.Visibility = System.Windows.Visibility.Visible;
                return;
            }

            this._ctlLogOuterContainer.Visibility = System.Windows.Visibility.Visible;
            this._ctlNoLogFileMessage.Visibility = System.Windows.Visibility.Hidden;

            foreach (var item in result.EnvironmentInfo)
            {
                if (item.SampleTimestamp > this._lastLogTimestamp)
                    this._lastLogTimestamp = item.SampleTimestamp;

                var link = new Hyperlink();
                link.Inlines.Add(string.Format("{0} - {1}", item.Ec2InstanceId, item.SampleTimestamp));
                link.NavigateUri = new Uri(item.Message);
                link.RequestNavigate += new RequestNavigateEventHandler(link_RequestNavigate);

                var tb = new TextBlock();
                tb.Inlines.Add(link);
                this._ctlLogFileLinks.Children.Add(tb);
            }
        }

        void link_RequestNavigate(object sender, RequestNavigateEventArgs evnt)
        {
            try
            {
                Process.Start(new ProcessStartInfo(evnt.Uri.OriginalString));
                evnt.Handled = true;
            }
            catch (Exception e)
            {
                if (evnt.Uri != null)
                    LOGGER.Error("Error viewing log results with url of " + evnt.Uri.OriginalString, e);
                else
                    LOGGER.Error("Error viewing log results because uri is null", e);

                ToolkitFactory.Instance.ShellProvider.ShowError("Error polling for new environment logs: ", e.Message);
            }
        }
    }
}
