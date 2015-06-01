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
    /// Interaction logic for RDSInstanceCharts.xaml
    /// </summary>
    public partial class RDSInstanceCharts : BaseResourceCharts
    {
        IAmazonCloudWatch _cwClient;
        string _dbInstanceId;
        public RDSInstanceCharts(IAmazonCloudWatch cwClient, string dbInstanceId)
        {
            this._cwClient = cwClient;
            this._dbInstanceId = dbInstanceId;
            InitializeComponent();

            this._ctlLabel.Text = "Metrics for RDS Instance " + this._dbInstanceId;
        }


        public override void RenderCharts(int hoursInPast)
        {
            List<Dimension> dimensions = new List<Dimension>();
            dimensions.Add(new Dimension() { Name = "DBInstanceIdentifier", Value = this._dbInstanceId });

            this._ctlCPUUtilizationSeries.Render(this._cwClient, "AWS/RDS", "CPUUtilization", "Average", "Percent", dimensions, hoursInPast);
            this._ctlFreeStorageSpace.Render(this._cwClient, "AWS/RDS", "FreeStorageSpace", "Minimum", "Bytes", dimensions, hoursInPast);
        }
    }
}
