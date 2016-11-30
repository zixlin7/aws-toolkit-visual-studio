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

using Amazon.AWSToolkit.SimpleWorkers;

using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

namespace Amazon.AWSToolkit.CloudFormation.View.Components.ResourceCharts
{
    /// <summary>
    /// Interaction logic for LoadBalancerCharts.xaml
    /// </summary>
    public partial class LoadBalancerCharts : BaseResourceCharts
    {
        IAmazonCloudWatch _cwClient;
        string _loadBalancerName;
        public LoadBalancerCharts(IAmazonCloudWatch cwClient, string loadBalancerName)
        {
            this._cwClient = cwClient;
            this._loadBalancerName = loadBalancerName;
            InitializeComponent();

            this._ctlLabel.Text = "Metrics for Load Balancer " + this._loadBalancerName;
        }

        public override void RenderCharts(int hoursInPast)
        {
            List<Dimension> dimensions = new List<Dimension>();
            dimensions.Add(new Dimension(){Name = "LoadBalancerName", Value = this._loadBalancerName});

            this._ctlLatencyCountSeries.Render(this._cwClient, "AWS/ELB", "Latency", "Average", "Seconds", dimensions, hoursInPast);
            this._ctlRequestCountSeries.Render(this._cwClient, "AWS/ELB", "RequestCount", "Sum", "Count", dimensions, hoursInPast);
        }
    }
}
