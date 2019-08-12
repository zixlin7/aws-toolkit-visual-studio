using System.Collections.Generic;
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
