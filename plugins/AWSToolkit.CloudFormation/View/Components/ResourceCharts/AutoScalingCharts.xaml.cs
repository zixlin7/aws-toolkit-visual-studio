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
    /// Interaction logic for AutoScalingCharts.xaml
    /// </summary>
    public partial class AutoScalingCharts : BaseResourceCharts
    {
        IAmazonCloudWatch _cwClient;
        string _autoScalingGroupName;
        public AutoScalingCharts(IAmazonCloudWatch cwClient, string autoScalingGroupName)
        {
            this._cwClient = cwClient;
            this._autoScalingGroupName = autoScalingGroupName;
            InitializeComponent();

            this._ctlLabel.Text = "Metrics for AutoScaling Group " + this._autoScalingGroupName;
        }

        public override void RenderCharts(int hoursInPast)
        {
            List<Dimension> dimensions = new List<Dimension>();
            dimensions.Add(new Dimension() { Name = "AutoScalingGroupName", Value = this._autoScalingGroupName });

            this._ctlCPUUtilizationSeries.Render(this._cwClient, "AWS/EC2", "CPUUtilization", "Average", "Percent", dimensions, hoursInPast);
            this._ctlNetworkInSeries.Render(this._cwClient, "AWS/EC2", "NetworkIn", "Maximum", "Bytes", dimensions, hoursInPast);
            this._ctlNetworkOutSeries.Render(this._cwClient, "AWS/EC2", "NetworkOut", "Maximum", "Bytes", dimensions, hoursInPast);
        }
    }
}
