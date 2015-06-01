using System;
using System.Collections.Generic;
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

using System.Windows.Controls.DataVisualization.Charting;

using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

namespace Amazon.AWSToolkit.CommonUI.Charts
{
    /// <summary>
    /// Interaction logic for CloudWatchChart.xaml
    /// </summary>
    public partial class CloudWatchChart
    {
        public CloudWatchChart()
        {
            this.DataContext = this;
            InitializeComponent();
        }

        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register("Status", typeof(string), typeof(CloudWatchChart),
            new PropertyMetadata(string.Empty));

        public string Status
        {
            get
            {
                return GetValue(StatusProperty) as string;
            }
            set
            {
                SetValue(StatusProperty, value);
                updateFullTitlebar();
            }
        }


        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(CloudWatchChart),
            new PropertyMetadata(string.Empty));

        public string Title
        {
            get
            {
                return GetValue(TitleProperty) as string;
            }
            set
            {
                SetValue(TitleProperty, value);
                SetValue(FullTitleBarProperty, value);
            }
        }

        public static readonly DependencyProperty FullTitleBarProperty =
            DependencyProperty.Register("FullTitleBar", typeof(string), typeof(CloudWatchChart),
            new PropertyMetadata(string.Empty));

        public string FullTitleBar
        {
            get
            {
                return GetValue(FullTitleBarProperty) as string;
            }
        }

        void updateFullTitlebar()
        {
            if(string.IsNullOrEmpty(this.Status))
                this.SetValue(FullTitleBarProperty, this.Title);
            else
                this.SetValue(FullTitleBarProperty, string.Format("{0} ({1})", this.Title, this.Status));
        }

        public void Render(IAmazonCloudWatch cwClient, 
            string metricNamespace, string metricName, string stats, string units,
            List<Dimension> dimensions, int hoursToView)
        {
            CloudWatchDataFetcher fetcher;
            fetcher = new CloudWatchDataFetcher(
                cwClient, this, metricNamespace, metricName, stats, units, dimensions, hoursToView);
            ThreadPool.QueueUserWorkItem(fetcher.Execute);
        }

        internal LineSeries LineSeries
        {
            get { return this._ctlLineSeries; }
        }
    }
}
