using System.Collections.Generic;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

namespace Amazon.AWSToolkit.CloudFormation.View.Components.ResourceCharts
{
    /// <summary>
    /// Interaction logic for APIGatewayCharts.xaml
    /// </summary>
    public partial class APIGatewayCharts : BaseResourceCharts
    {
        IAmazonCloudWatch _cwClient;
        string _apiGatewayName;

        public APIGatewayCharts(IAmazonCloudWatch cwClient, string apiGatewayName)
        {
            this._cwClient = cwClient;
            this._apiGatewayName = apiGatewayName;
            InitializeComponent();

            this._ctlLabel.Text = "Metrics for API Gateway " + this._apiGatewayName;
        }

        public override void RenderCharts(int hoursInPast)
        {
            List<Dimension> dimensions = new List<Dimension>();
            dimensions.Add(new Dimension() { Name = "ApiName", Value = this._apiGatewayName });
            dimensions.Add(new Dimension() { Name = "Stage", Value = "Prod" });

            this._ctlAPICalls.Render(this._cwClient, "AWS/ApiGateway", "Count", "Sum", "Count", dimensions, hoursInPast);
            this._ctlLatency.Render(this._cwClient, "AWS/ApiGateway", "Latency", "Average", "Milliseconds", dimensions, hoursInPast);

            this._ctl4xxErrors.Render(this._cwClient, "AWS/ApiGateway", "4XXError", "Sum", "Count", dimensions, hoursInPast);
            this._ctl5xxErrors.Render(this._cwClient, "AWS/ApiGateway", "5XXError", "Sum", "Count", dimensions, hoursInPast);
        }
    }
}
